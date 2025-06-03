using EduSync.Data;
using EduSync.DTOs;
using EduSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EduSync.Configurations;
using Microsoft.Extensions.Options;

namespace EduSync.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CourseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CourseController> _logger;
        private readonly AzureBlobStorageOptions _blobStorageOptions;

        public CourseController(
            ApplicationDbContext context, 
            IMapper mapper,
            IWebHostEnvironment environment,
            ILogger<CourseController> logger,
            IOptions<AzureBlobStorageOptions> blobStorageOptions)
        {
            _context = context;
            _mapper = mapper;
            _environment = environment;
            _logger = logger;
            _blobStorageOptions = blobStorageOptions.Value;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCourses()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            var isInstructor = User.IsInRole("Instructor");

            if (isInstructor)
            {
                var courses = await _context.Courses
                    .Include(c => c.Instructor)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Assessments)
                    .Where(c => c.InstructorId == userGuid)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CourseDto
                    {
                        CourseId = c.CourseId,
                        Title = c.Title,
                        Description = c.Description,
                        InstructorId = c.InstructorId,
                        InstructorName = c.Instructor.Name,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        MediaFileName = c.MediaFileName,
                        MediaContentType = c.MediaContentType,
                        MediaFileSize = c.MediaFileSize,
                        MediaFileUrl = c.MediaFileUrl,
                        EnrolledStudentCount = c.Enrollments.Count,
                        AssessmentCount = c.Assessments.Count
                    })
                    .ToListAsync();

                return Ok(courses);
            }
            else
            {
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == userGuid)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                var courses = await _context.Courses
                    .Include(c => c.Instructor)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Assessments)
                    .Where(c => enrolledCourseIds.Contains(c.CourseId))
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CourseDto
                    {
                        CourseId = c.CourseId,
                        Title = c.Title,
                        Description = c.Description,
                        InstructorId = c.InstructorId,
                        InstructorName = c.Instructor.Name,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        MediaFileName = c.MediaFileName,
                        MediaContentType = c.MediaContentType,
                        MediaFileSize = c.MediaFileSize,
                        MediaFileUrl = c.MediaFileUrl,
                        EnrolledStudentCount = c.Enrollments.Count,
                        AssessmentCount = c.Assessments.Count
                    })
                    .ToListAsync();

                return Ok(courses);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourse(Guid id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            var isInstructor = User.IsInRole("Instructor");

            var course = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Enrollments)
                .Include(c => c.Assessments)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return NotFound(new { message = "Course not found" });

            bool hasAccess = false;
            
            if (isInstructor)
                hasAccess = course.InstructorId == userGuid;
            else
                hasAccess = course.Enrollments.Any(e => e.StudentId == userGuid);
                
            if (!hasAccess)
                return NotFound(new { message = "Course not found or you don't have access" });

            var courseDetail = new CourseDetailDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId,
                InstructorName = course.Instructor.Name,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt,
                MediaFileName = course.MediaFileName,
                MediaContentType = course.MediaContentType,
                MediaFileSize = course.MediaFileSize,
                MediaFileUrl = course.MediaFileUrl,
                EnrolledStudentCount = course.Enrollments.Count,
                IsEnrolled = course.Enrollments.Any(e => e.StudentId == userGuid)
            };
            
            return Ok(courseDetail);
        }

        [HttpPost]
        [Authorize(Roles = "Instructor")]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB max file size
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCourse([FromForm] CourseCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Invalid model state",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            
            try
            {
                var course = new Course
                {
                    CourseId = Guid.NewGuid(),
                    Title = model.Title,
                    Description = model.Description,
                    InstructorId = userGuid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Category = model.Category,
                    Level = model.Level,
                    Prerequisites = model.Prerequisites,
                    MediaFileName = string.Empty,
                    MediaContentType = string.Empty,
                    MediaFileUrl = string.Empty
                };

                if (model.MediaFile != null && model.MediaFile.Length > 0)
                {
                    var (blobUri, originalFileName) = await UploadToBlobStorage(model.MediaFile, course.CourseId.ToString());
                    
                    if (!string.IsNullOrEmpty(blobUri))
                    {
                        course.MediaFileUrl = blobUri;
                        course.MediaFileName = originalFileName;
                        course.MediaContentType = model.MediaFile.ContentType;
                        course.MediaFileSize = model.MediaFile.Length;
                    }
                }

                await _context.Courses.AddAsync(course);
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetCourse), 
                    new { id = course.CourseId }, 
                    new { 
                        success = true, 
                        message = "Course created successfully", 
                        courseId = course.CourseId,
                        title = course.Title,
                        hasMedia = !string.IsNullOrEmpty(course.MediaFileUrl)
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating course: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
                }
                
                // Check for Azure storage specific exceptions
                if (ex.Message.Contains("Azure") || ex.Message.Contains("Blob") || ex.Message.Contains("Storage"))
                {
                    _logger.LogError("Azure Blob Storage error detected");
                    
                    // Log Azure Blob Storage configuration
                    _logger.LogError("Container name: {ContainerName}", _blobStorageOptions.ContainerName);
                    _logger.LogError("Connection string starts with: {ConnectionStringStart}", 
                        _blobStorageOptions.ConnectionString.Length > 20 
                            ? _blobStorageOptions.ConnectionString.Substring(0, 20) 
                            : _blobStorageOptions.ConnectionString);
                }
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while creating the course",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50MB max file size
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateCourse(Guid id, [FromForm] CourseUpdateDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Find course and verify ownership
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return NotFound(new { message = "Course not found" });

            if (course.InstructorId != userGuid)
                return BadRequest(new { message = "You don't have permission to update this course" });

            // Update course properties
            course.Title = model.Title;
            course.Description = model.Description;
            course.UpdatedAt = DateTime.UtcNow;
            
            // Handle media file update
            if (model.MediaFile != null && model.MediaFile.Length > 0)
            {
                // If there's an existing file, delete it first
                if (!string.IsNullOrEmpty(course.MediaFileUrl))
                {
                    await DeleteFromBlobStorage(course.MediaFileUrl);
                }
                
                // Upload new file to Azure Blob Storage
                var (blobUri, originalFileName) = await UploadToBlobStorage(model.MediaFile, course.CourseId.ToString());
                
                if (!string.IsNullOrEmpty(blobUri))
                {
                    // Save file metadata to course
                    course.MediaFileUrl = blobUri;
                    course.MediaFileName = originalFileName;
                    course.MediaContentType = model.MediaFile.ContentType;
                    course.MediaFileSize = model.MediaFile.Length;
                }
            }
            else if (model.RemoveMedia && !string.IsNullOrEmpty(course.MediaFileUrl))
            {
                // Remove media file if requested
                await DeleteFromBlobStorage(course.MediaFileUrl);
                course.MediaFileUrl = null;
                course.MediaFileName = null;
                course.MediaContentType = null;
                course.MediaFileSize = null;
            }

            // Save changes
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Course updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Find course with related entities
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Assessments)
                    .ThenInclude(a => a.Questions)
                .Include(c => c.Assessments)
                    .ThenInclude(a => a.Results)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return NotFound(new { success = false, message = "Course not found" });

            // Verify ownership
            if (course.InstructorId != userGuid)
                return BadRequest(new { success = false, message = "You don't have permission to delete this course" });

            // Delete related entities first
            foreach (var assessment in course.Assessments)
            {
                // Delete assessment questions
                if (assessment.Questions.Any())
                    _context.Questions.RemoveRange(assessment.Questions);
                
                // Delete assessment results
                if (assessment.Results.Any())
                    _context.Results.RemoveRange(assessment.Results);
            }
            
            // Delete assessments
            if (course.Assessments.Any())
                _context.Assessments.RemoveRange(course.Assessments);
            
            // Delete enrollments
            if (course.Enrollments.Any())
                _context.Enrollments.RemoveRange(course.Enrollments);
            
            // Delete media files from Azure Blob Storage if they exist
            if (!string.IsNullOrEmpty(course.MediaFileUrl))
            {
                await DeleteFromBlobStorage(course.MediaFileUrl);
            }
            
            // Delete course
            _context.Courses.Remove(course);
            
            // Save changes
            await _context.SaveChangesAsync();
            
            return Ok(new { success = true, message = "Course deleted successfully" });
        }

        [HttpGet("instructor/stats")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> GetInstructorStats()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            try
            {

                var instructorId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(instructorId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }
                
                var instructorGuid = Guid.Parse(instructorId);
                
                // Get total courses count
                var totalCourses = await _context.Courses
                    .Where(c => c.InstructorId == instructorGuid)
                    .CountAsync();
                
                // Get unique students enrolled in instructor's courses
                var enrolledStudentIds = await _context.Enrollments
                    .Where(e => e.Course.InstructorId == instructorGuid)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToListAsync();
                
                var totalStudents = enrolledStudentIds.Count();
                
                // Ratings functionality removed as requested
                
                
                
                return Ok(new 
                { 
                    success = true,
                    totalCourses,
                    totalStudents
                });
            }
            catch (Exception ex)
            {
                // Log error removed during refactoring
                return StatusCode(500, new { success = false, message = "An error occurred while getting instructor stats" });
            }
        }

        [HttpGet("instructor/enrollments/recent")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> GetRecentEnrollments()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            try
            {

                var instructorId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(instructorId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }
                
                var instructorGuid = Guid.Parse(instructorId);
                
                var enrollments = await _context.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .Where(e => e.Course.InstructorId == instructorGuid)
                    .OrderByDescending(e => e.EnrollmentDate)
                    .Take(10)
                    .ToListAsync();

                var result = enrollments.Select(e => new RecentEnrollmentDto
                {
                    Id = e.EnrollmentId,
                    StudentName = e.Student.Name,
                    CourseTitle = e.Course.Title,
                    EnrollmentDate = e.EnrollmentDate
                }).ToList();
                
                return Ok(result);

            }
            catch (Exception ex)
            {
                // Log error removed during refactoring
                return StatusCode(500, new { success = false, message = "An error occurred while getting recent enrollments" });
            }
        }

        [HttpGet("instructor/top-courses")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> GetTopCourses()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            try
            {

                var instructorId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(instructorId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }
                
                var instructorGuid = Guid.Parse(instructorId);
                
                var courses = await _context.Courses
                    .Include(c => c.Enrollments)
                    // Ratings include removed as requested
                    .Where(c => c.InstructorId == instructorGuid)
                    .OrderByDescending(c => c.Enrollments.Count)
                    .Take(5)
                    .ToListAsync();

                var result = courses.Select(c => new TopCourseDto
                {
                    Id = c.CourseId,
                    Title = c.Title,
                    ThumbnailUrl = GetMediaFileUrl(c.MediaFileName, c.CourseId.ToString()),
                    EnrollmentsCount = c.Enrollments?.Count ?? 0,
                    // AverageRating removed as requested
                    Progress = CalculateAverageProgress(c)
                }).ToList();
                
                return Ok(result);

            }
            catch (Exception ex)
            {
                // Log error removed during refactoring
                return StatusCode(500, new { success = false, message = "An error occurred while getting top courses" });
            }
        }

        private string GetMediaFileUrl(string fileName, string courseId = null)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            var basePath = string.IsNullOrEmpty(courseId)
                ? $"uploads/courses/{fileName}"
                : $"uploads/courses/{courseId}/{fileName}";

            var urlPath = basePath.Replace("\\", "/").TrimStart('/');
            return $"/{urlPath}";
        }

        private async Task<(string blobUri, string fileName)> UploadToBlobStorage(IFormFile file, string courseId)
{
    if (file == null || file.Length == 0)
        return (null, null);
        
    try
    {
        // Log connection string (first 20 chars) for debugging
        _logger.LogInformation($"Using Azure connection string starting with: {_blobStorageOptions.ConnectionString.Substring(0, Math.Min(20, _blobStorageOptions.ConnectionString.Length))}...");
        _logger.LogInformation($"Container name: {_blobStorageOptions.ContainerName}");
        
        // Create a BlobServiceClient object using the connection string
        var blobServiceClient = new BlobServiceClient(_blobStorageOptions.ConnectionString);
        
        // Get a reference to the container
        var containerClient = blobServiceClient.GetBlobContainerClient(_blobStorageOptions.ContainerName);
        
        // Create the container if it doesn't exist
        var containerResponse = await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        if (containerResponse != null)
        {
            _logger.LogInformation($"Container created: {_blobStorageOptions.ContainerName}");
        }
        else
        {
            _logger.LogInformation($"Container already exists: {_blobStorageOptions.ContainerName}");
        }
        
        // Create a unique blob name using the courseId and file name
        string uniqueFileName = $"{courseId}/{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        _logger.LogInformation($"Uploading file as: {uniqueFileName}");
        
        // Get a reference to the blob
        var blobClient = containerClient.GetBlobClient(uniqueFileName);
        
        // Set the content type
        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = file.ContentType
        };
        
        // Upload the file
        using (var stream = file.OpenReadStream())
        {
            _logger.LogInformation($"Starting upload of file size: {file.Length} bytes");
            await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });
            _logger.LogInformation("Upload completed successfully");
        }
        
        var blobUri = blobClient.Uri.ToString();
        _logger.LogInformation($"Blob URI: {blobUri}");
        return (blobUri, file.FileName);
    }
    catch (Exception ex)
    {
        // Log the full exception details
        _logger.LogError(ex, "Error uploading file to Azure Blob Storage");
        Console.WriteLine($"Error uploading file to Azure Blob Storage: {ex.Message}");
        if (ex.InnerException != null)
        {
            _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        }
        return (null, null);
    }
}
        
        /// <summary>
        /// Deletes a file from Azure Blob Storage
        /// </summary>
        private async Task<bool> DeleteFromBlobStorage(string blobUri)
        {
            if (string.IsNullOrEmpty(blobUri))
                return false;
                
            try
            {
                // Create a BlobServiceClient object using the connection string
                var blobServiceClient = new BlobServiceClient(_blobStorageOptions.ConnectionString);
                
                // Get a reference to the container
                var containerClient = blobServiceClient.GetBlobContainerClient(_blobStorageOptions.ContainerName);
                
                // Parse the blob name from the URI
                Uri uri = new Uri(blobUri);
                string blobName = uri.AbsolutePath.TrimStart('/');
                
                // Remove the container name from the blob name
                if (blobName.StartsWith(_blobStorageOptions.ContainerName + "/"))
                    blobName = blobName.Substring(_blobStorageOptions.ContainerName.Length + 1);
                
                // Get a reference to the blob
                var blobClient = containerClient.GetBlobClient(blobName);
                
                // Delete the blob
                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                // In a production environment, you would log this error
                Console.WriteLine($"Error deleting file from Azure Blob Storage: {ex.Message}");
                return false;
            }
        }

        private int CalculateAverageProgress(Course course)
        {
            try
            {
                if (course.Enrollments == null || !course.Enrollments.Any())
                    return 0;

                var enrollments = course.Enrollments.ToList();
                var totalProgress = enrollments.Sum(e => e.Progress);
                return enrollments.Count > 0 ? (int)(totalProgress / enrollments.Count) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating average progress for course {course.CourseId}: {ex.Message}");
                return 0;
            }
        }

        [HttpGet("enrolled/count")]
        public async Task<IActionResult> GetEnrolledCoursesCount()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            try
            {
                var count = await _context.Enrollments
                    .Where(e => e.StudentId == userGuid)
                    .CountAsync();

                return Ok(new { success = true, count });
            }
            catch (Exception ex)
            {
                // Log error removed during refactoring
                return StatusCode(500, new { success = false, message = "An error occurred while getting enrolled courses count", error = ex.Message });
            }
        }

        [HttpPost("enroll")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EnrollInCourse([FromBody] EnrollmentDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Check if course exists
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.CourseId == model.CourseId);

            if (course == null)
                return NotFound(new { message = "Course not found" });

            // Check if student is already enrolled
            if (course.Enrollments.Any(e => e.StudentId == userGuid))
                return BadRequest(new { message = "You are already enrolled in this course" });

            // Create new enrollment
            var enrollment = new Enrollment
            {
                CourseId = course.CourseId,
                StudentId = userGuid,
                EnrollmentDate = DateTime.UtcNow
            };

            await _context.Enrollments.AddAsync(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully enrolled in the course" });
        }

        [HttpDelete("enroll/{courseId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UnenrollFromCourse(Guid courseId)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Find enrollment record
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userGuid);

            if (enrollment == null)
                return NotFound(new { message = "You are not enrolled in this course" });

            // Remove enrollment
            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully unenrolled from the course" });
        }

        [HttpGet("explore")]
        public async Task<IActionResult> ExploreCourses()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            var isInstructor = User.IsInRole("Instructor");

            // If instructor, show all courses except their own
            // If student, show all courses they're not enrolled in
            if (isInstructor)
            {
                var courses = await _context.Courses
                    .Include(c => c.Instructor)
                    .Include(c => c.Assessments)
                    .Include(c => c.Enrollments)
                    .Where(c => c.InstructorId != userGuid)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
                    
                var courseDtos = _mapper.Map<List<CourseDto>>(courses);
                return Ok(courseDtos);
            }
            else
            {
                // Get course IDs where the student is already enrolled
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == userGuid)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                // Get courses where the student is not enrolled
                var courses = await _context.Courses
                    .Include(c => c.Instructor)
                    .Include(c => c.Assessments)
                    .Include(c => c.Enrollments)
                    .Where(c => !enrolledCourseIds.Contains(c.CourseId))
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
                    
                var courseDtos = _mapper.Map<List<CourseDto>>(courses);
                return Ok(courseDtos);
            }
        }

        [HttpGet("enrolled")]
        public async Task<IActionResult> GetEnrolledCourses()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            
            try
            {
                // Get course IDs where student is enrolled
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == userGuid)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                if (!enrolledCourseIds.Any())
                    return Ok(new { success = true, data = new List<CourseDto>() });

                // Get the courses
                var courses = await _context.Courses
                    .Include(c => c.Instructor)
                    .Include(c => c.Assessments)
                    .Include(c => c.Enrollments)
                    .Where(c => enrolledCourseIds.Contains(c.CourseId))
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
                    
                var courseDtos = _mapper.Map<List<CourseDto>>(courses);
                // Return the array directly to match frontend expectations
                _logger.LogInformation($"Returning {courseDtos.Count} enrolled courses for user {userGuid}");
                return Ok(courseDtos);
            }
            catch (Exception ex)
            {
                // Log error removed during refactoring
                return StatusCode(500, new { success = false, message = "An error occurred while getting enrolled courses" });
            }
        }
    }
}
