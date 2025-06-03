using EduSync.Data;
using EduSync.DTOs;
using EduSync.EventHub;
using EduSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using EduSync.Hubs;
using EduSyncServer.Models;

namespace EduSync.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AssessmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IEventPublisher _eventPublisher;
        private readonly IHubContext<AssessmentHub> _hubContext;
        private readonly ILogger<AssessmentController> _logger;

        public AssessmentController(
            ApplicationDbContext context, 
            IMapper mapper,
            IEventPublisher eventPublisher,
            IHubContext<AssessmentHub> hubContext,
            ILogger<AssessmentController> logger)
        {
            _context = context;
            _mapper = mapper;
            _eventPublisher = eventPublisher;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssessment(Guid id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            var isInstructor = User.IsInRole("Instructor");

            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .Include(a => a.Questions)
                .Include(a => a.Results)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {                
                return NotFound(new { message = "Assessment not found" });
            }

            // Check access permissions
            if (isInstructor)
            {
                // Check if instructor owns this course
                if (assessment.Course.InstructorId != userGuid)
                {
                    return NotFound(new { message = "You don't have access to this assessment" });
                }

                var assessmentDto = new AssessmentDetailDto
                {
                    AssessmentId = assessment.AssessmentId,
                    CourseId = assessment.CourseId,
                    Title = assessment.Title,
                    Description = assessment.Description,
                    MaxScore = assessment.MaxScore,
                    TimeLimit = assessment.TimeLimit,
                    CreatedAt = assessment.CreatedAt,
                    UpdatedAt = assessment.UpdatedAt,
                    CourseName = assessment.Course.Title,
                    Questions = assessment.Questions.Select(q => new QuestionDto
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        Options = q.GetOptionsAsList(),
                        Points = q.Points,
                        Type = (int)q.Type
                    }).ToList()
                };

                return Ok(assessmentDto);
            }
            else
            {
                // Check if student is enrolled in this course
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.CourseId == assessment.CourseId && e.StudentId == userGuid);

                if (!isEnrolled)
                {
                    return NotFound(new { message = "You don't have access to this assessment" });
                }

                // Check if student has started an attempt
                var attempt = assessment.Results
                    .FirstOrDefault(r => r.UserId == userGuid && !r.IsCompleted);
                var hasStartedAttempt = attempt != null;

                var studentAssessment = new StudentAssessmentDto
                {
                    AssessmentId = assessment.AssessmentId,
                    CourseId = assessment.CourseId,
                    Title = assessment.Title,
                    Description = assessment.Description,
                    MaxScore = assessment.MaxScore,
                    TimeLimit = assessment.TimeLimit,
                    CourseName = assessment.Course.Title,
                    Questions = hasStartedAttempt
                        ? assessment.Questions.Select(q => new StudentQuestionDto
                        {
                            QuestionId = q.QuestionId,
                            QuestionText = q.QuestionText,
                            Options = q.GetOptionsAsList(),
                            Points = q.Points,
                            Type = (int)q.Type
                        }).ToList()
                        : new List<StudentQuestionDto>() // Return empty list instead of null
                };

                return Ok(studentAssessment);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> CreateAssessment([FromBody] AssessmentCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Check if course exists and user is the instructor
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == model.CourseId);

            if (course == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            if (course.InstructorId != userGuid)
            {
                return BadRequest(new { message = "You don't have permission to create assessments for this course" });
            }

            // Create new assessment and make it live automatically
            var assessmentId = Guid.NewGuid();
            var sessionId = Guid.NewGuid().ToString().Substring(0, 8);
            var currentTime = DateTime.UtcNow;
            
            var assessment = new Assessment
            {
                AssessmentId = assessmentId,
                CourseId = model.CourseId,
                Title = model.Title,
                Description = model.Description,
                MaxScore = model.MaxScore,
                TimeLimit = model.TimeLimit,
                CreatedAt = currentTime,
                IsLive = true, // Set to live immediately
                StartedAt = currentTime, // Set start time to now
                EndedAt = null,
                SessionId = sessionId, // Generate a session ID
                Status = AssessmentStatus.Live // Set status to Live
            };

            // Create questions
            if (model.Questions != null && model.Questions.Count > 0)
            {
                foreach (var questionDto in model.Questions)
                {
                    var question = new Question
                    {
                        QuestionId = Guid.NewGuid(),
                        AssessmentId = assessment.AssessmentId,
                        QuestionText = questionDto.QuestionText,
                        CorrectAnswer = questionDto.CorrectAnswer,
                        Points = questionDto.Points,
                        Type = (QuestionType)questionDto.Type
                    };
                    question.SetOptionsFromList(questionDto.Options);

                    assessment.Questions.Add(question);
                }
            }

            _context.Assessments.Add(assessment);
            await _context.SaveChangesAsync();
            
            // Publish event to Event Hub that assessment is live
            await _eventPublisher.PublishEventAsync("AssessmentStarted", new
            {
                AssessmentId = assessment.AssessmentId.ToString(),
                CourseId = assessment.CourseId.ToString(),
                Title = assessment.Title,
                StartedAt = assessment.StartedAt,
                SessionId = assessment.SessionId,
                InstructorId = userId
            });

            // Notify connected clients through SignalR
            await _hubContext.Clients.All.SendAsync("AssessmentStarted", new
            {
                AssessmentId = assessment.AssessmentId.ToString(),
                Title = assessment.Title,
                StartedAt = assessment.StartedAt,
                SessionId = assessment.SessionId
            });
            
            return CreatedAtAction(nameof(GetAssessment), new { id = assessment.AssessmentId }, 
                new { 
                    assessmentId = assessment.AssessmentId,
                    isLive = assessment.IsLive,
                    status = assessment.Status,
                    sessionId = assessment.SessionId
                });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> UpdateAssessment(Guid id, [FromBody] AssessmentUpdateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .Include(a => a.Questions)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound(new { message = "Assessment not found" });
            }

            if (assessment.Course.InstructorId != userGuid)
            {
                return BadRequest(new { message = "You don't have permission to update this assessment" });
            }

            // Update assessment properties
            assessment.Title = model.Title;
            assessment.Description = model.Description;
            assessment.MaxScore = model.MaxScore;
            assessment.TimeLimit = model.TimeLimit;
            assessment.UpdatedAt = DateTime.UtcNow;

            // Update questions if provided
            if (model.Questions != null && model.Questions.Count > 0)
            {
                // Get existing question IDs
                var existingQuestionIds = assessment.Questions
                    .Select(q => q.QuestionId)
                    .ToList();

                // Process each question in the update
                foreach (var questionDto in model.Questions)
                {
                    if (questionDto.QuestionId.HasValue)
                    {
                        // Update existing question
                        var existingQuestion = assessment.Questions
                            .FirstOrDefault(q => q.QuestionId == questionDto.QuestionId.Value);

                        if (existingQuestion != null)
                        {
                            existingQuestion.QuestionText = questionDto.QuestionText;
                            existingQuestion.SetOptionsFromList(questionDto.Options);
                            existingQuestion.CorrectAnswer = questionDto.CorrectAnswer;
                            existingQuestion.Points = questionDto.Points;
                            existingQuestion.Type = (QuestionType)questionDto.Type;
                        }
                    }
                    else
                    {
                        // Add new question
                        var newQuestion = new Question
                        {
                            QuestionId = Guid.NewGuid(),
                            AssessmentId = assessment.AssessmentId,
                            QuestionText = questionDto.QuestionText,
                            CorrectAnswer = questionDto.CorrectAnswer,
                            Points = questionDto.Points,
                            Type = (QuestionType)questionDto.Type
                        };
                        newQuestion.SetOptionsFromList(questionDto.Options);

                        assessment.Questions.Add(newQuestion);
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Assessment updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteAssessment(Guid id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .Include(a => a.Questions)
                .Include(a => a.Results)
                .Include(a => a.Attempts)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound(new { message = "Assessment not found" });
            }

            if (assessment.Course.InstructorId != userGuid)
            {
                return BadRequest(new { message = "You don't have permission to delete this assessment" });
            }

            // Remove related Results
            if (assessment.Results != null && assessment.Results.Any())
            {
                _context.Results.RemoveRange(assessment.Results);
            }

            // Remove related Attempts
            var attempts = _context.AssessmentAttempts.Where(a => a.AssessmentId == assessment.AssessmentId);
            if (attempts.Any())
            {
                _context.AssessmentAttempts.RemoveRange(attempts);
            }

            // Remove related AssessmentParticipants
            var participants = _context.AssessmentParticipants.Where(p => p.AssessmentId == assessment.AssessmentId);
            if (participants.Any())
            {
                _context.AssessmentParticipants.RemoveRange(participants);
            }

            // Remove questions first
            _context.Questions.RemoveRange(assessment.Questions);
            
            // Remove assessment
            _context.Assessments.Remove(assessment);
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "Assessment deleted successfully" });
        }

        [HttpGet("instructor")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> GetAllAssessmentsForInstructor()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            var assessments = await _context.Assessments
                .Include(a => a.Course)
                .Include(a => a.Questions)
                .Where(a => a.Course.InstructorId == userGuid)
                .Select(a => new AssessmentDto
                {
                    AssessmentId = a.AssessmentId,
                    CourseId = a.CourseId,
                    Title = a.Title,
                    Description = a.Description,
                    MaxScore = a.MaxScore,
                    TimeLimit = a.TimeLimit,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    CourseName = a.Course.Title,
                    QuestionCount = a.Questions.Count
                })
                .ToListAsync();

            return Ok(assessments);
        }

        [HttpGet("student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetAllAssessmentsForStudent()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            
            _logger.LogInformation($"Fetching assessments for student {userGuid}");

            // Get assessments from enrolled courses
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == userGuid)
                .Select(e => e.CourseId)
                .ToListAsync();
                
            _logger.LogInformation($"Student is enrolled in {enrolledCourseIds.Count} courses");

            var assessments = await _context.Assessments
                .Include(a => a.Course)
                .Include(a => a.Results.Where(r => r.UserId == userGuid))
                .Where(a => enrolledCourseIds.Contains(a.CourseId))
                .Select(a => new AssessmentDto
                {
                    AssessmentId = a.AssessmentId,
                    CourseId = a.CourseId,
                    Title = a.Title,
                    Description = a.Description,
                    MaxScore = a.MaxScore,
                    TimeLimit = a.TimeLimit,
                    CreatedAt = a.CreatedAt,
                    CourseName = a.Course.Title,
                    IsLive = a.IsLive,
                    Status = a.Status,
                    IsCompleted = a.Results.Any(r => r.IsCompleted),
                    Score = a.Results
                        .Where(r => r.IsCompleted)
                        .Select(r => r.Score)
                        .FirstOrDefault()
                })
                .ToListAsync();
                
            _logger.LogInformation($"Found {assessments.Count} total assessments for student");
            _logger.LogInformation($"Live assessments: {assessments.Count(a => a.IsLive || a.Status == AssessmentStatus.Live)}");
            _logger.LogInformation($"Completed assessments: {assessments.Count(a => a.IsCompleted)}");

            return Ok(assessments);
        }

        [HttpGet("course/{courseId}")]
        public async Task<IActionResult> GetCourseAssessments(Guid courseId)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            var isInstructor = User.IsInRole("Instructor");

            // Verify the course exists
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            // For instructors, check if they own the course
            if (isInstructor && course.InstructorId != userGuid)
            {
                return NotFound(new { message = "You don't have access to this course" });
            }

            // For students, check if they are enrolled
            if (!isInstructor)
            {
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.CourseId == courseId && e.StudentId == userGuid);

                if (!isEnrolled)
                {
                    return NotFound(new { message = "You don't have access to this course" });
                }
            }

            var assessments = await _context.Assessments
                .Include(a => a.Course)
                .Include(a => a.Questions)
                .Where(a => a.CourseId == courseId)
                .Select(a => new AssessmentDto
                {
                    AssessmentId = a.AssessmentId,
                    CourseId = a.CourseId,
                    Title = a.Title,
                    Description = a.Description,
                    MaxScore = a.MaxScore,
                    TimeLimit = a.TimeLimit,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    CourseName = a.Course.Title,
                    QuestionCount = a.Questions.Count,
                    IsLive = a.IsLive,
                    Status = a.Status,
                    IsCompleted = isInstructor ? false : _context.Results
                        .Any(r => r.AssessmentId == a.AssessmentId && r.UserId == userGuid && r.IsCompleted),
                    Score = !isInstructor ? _context.Results
                        .Where(r => r.AssessmentId == a.AssessmentId && r.UserId == userGuid && r.IsCompleted)
                        .Select(r => r.Score)
                        .FirstOrDefault() : null
                })
                .ToListAsync();

            if (!isInstructor)
            {
                // For students, only return assessments that have at least one question
                // AND filter out assessments that aren't live (unless they're already completed)
                var assessmentIds = assessments.Select(a => a.AssessmentId).ToList();
                var assessmentsWithQuestions = await _context.Questions
                    .Where(q => assessmentIds.Contains(q.AssessmentId))
                    .GroupBy(q => q.AssessmentId)
                    .Select(g => g.Key)
                    .ToListAsync();

                assessments = assessments
                    .Where(a => assessmentsWithQuestions.Contains(a.AssessmentId) && 
                                (a.IsLive || a.Status == AssessmentStatus.Live || a.IsCompleted || a.Status == AssessmentStatus.Completed))
                    .ToList();
                
                _logger.LogInformation($"Filtered assessments for student {userGuid}: {assessments.Count} assessments match criteria");
            }

            return Ok(assessments);
        }

        [HttpGet("student/live")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetLiveAssessments()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            
            _logger.LogInformation($"Fetching LIVE assessments for student {userGuid}");

            // Get assessments from enrolled courses
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == userGuid)
                .Select(e => e.CourseId)
                .ToListAsync();
                
            if (!enrolledCourseIds.Any())
            {
                _logger.LogInformation("Student is not enrolled in any courses");
                return Ok(new List<AssessmentDto>());
            }

            var liveAssessments = await _context.Assessments
                .Include(a => a.Course)
                .Where(a => enrolledCourseIds.Contains(a.CourseId) && 
                       (a.IsLive == true || a.Status == AssessmentStatus.Live))
                .Select(a => new AssessmentDto
                {
                    AssessmentId = a.AssessmentId,
                    CourseId = a.CourseId,
                    Title = a.Title,
                    Description = a.Description,
                    MaxScore = a.MaxScore,
                    TimeLimit = a.TimeLimit,
                    CreatedAt = a.CreatedAt,
                    CourseName = a.Course.Title,
                    IsLive = a.IsLive,
                    Status = a.Status
                })
                .ToListAsync();
                
            _logger.LogInformation($"Found {liveAssessments.Count} live assessments for student");
            
            return Ok(liveAssessments);
        }
        

        [HttpGet("student/completed")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetCompletedAssessments()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Get completed assessments for this student
            var completedAssessments = await _context.Results
                .Include(r => r.Assessment)
                    .ThenInclude(a => a.Course)
                .Where(r => r.UserId == userGuid && r.IsCompleted)
                .Select(r => new AssessmentDto
                {
                    AssessmentId = r.AssessmentId,
                    CourseId = r.Assessment.CourseId,
                    Title = r.Assessment.Title,
                    Description = r.Assessment.Description,
                    MaxScore = r.Assessment.MaxScore,
                    TimeLimit = r.Assessment.TimeLimit,
                    CreatedAt = r.Assessment.CreatedAt,
                    CourseName = r.Assessment.Course.Title,
                    IsCompleted = true,
                    Score = r.Score,
                    CompletedDate = r.CompletedDate
                })
                .ToListAsync();

            return Ok(completedAssessments);
        }

         [HttpPost("live/join/{assessmentId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> JoinLiveAssessment(Guid assessmentId)
        {
            try
            {
                _logger.LogInformation($"Student attempting to join live assessment: {assessmentId}");
                
                var assessment = await _context.Assessments
                    .Include(a => a.Course)
                    .Include(a => a.Questions)
                    .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);

                if (assessment == null)
                {
                    _logger.LogWarning($"Assessment not found: {assessmentId}");
                    return NotFound(new { success = false, message = "Assessment not found" });
                }

                // Check if assessment is currently live
                if (!assessment.IsLive || assessment.Status != AssessmentStatus.Live)
                {
                    _logger.LogWarning($"Assessment is not currently live: {assessmentId}, IsLive={assessment.IsLive}, Status={assessment.Status}");
                    return BadRequest(new { success = false, message = "Assessment is not currently live" });
                }

                // Check if student is enrolled in the course
                var studentId = User.FindFirst("UserId")?.Value;
                var studentGuid = Guid.Parse(studentId);
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.CourseId == assessment.CourseId && e.StudentId == studentGuid);

                if (!isEnrolled)
                {
                    _logger.LogWarning($"Student {studentGuid} is not enrolled in course {assessment.CourseId}");
                    return Forbid(); // Return 403 Forbidden
                }

                // Check if the student already has a result for this assessment
                var existingResult = await _context.Results
                    .FirstOrDefaultAsync(r => r.AssessmentId == assessmentId && r.UserId == studentGuid);

                if (existingResult == null)
                {
                    // Create a new result record
                    var result = new Result
                    {
                        ResultId = Guid.NewGuid(),
                        AssessmentId = assessmentId,
                        UserId = studentGuid,
                        AttemptDate = DateTime.UtcNow,
                        Score = 0,
                        IsCompleted = false,
                        Answers = "{}"
                    };

                    await _context.Results.AddAsync(result);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Created new result for student {studentGuid} in assessment {assessmentId}");
                    
                    // Publish event to Event Hub
                    await _eventPublisher.PublishEventAsync("StudentJoinedAssessment", new
                    {
                        AssessmentId = assessment.AssessmentId.ToString(),
                        UserId = studentId,
                        Timestamp = DateTime.UtcNow
                    });

                    // If SignalR hub is available, notify other participants
                    await _hubContext.Clients.Group(assessment.AssessmentId.ToString())
                        .SendAsync("StudentJoined", new { UserId = studentId, Timestamp = DateTime.UtcNow });
                }
                else if (existingResult.IsCompleted)
                {
                    _logger.LogWarning($"Student {studentGuid} has already completed assessment {assessmentId}");
                    return BadRequest(new { success = false, message = "You have already completed this assessment" });
                }

                // Student can now proceed with the assessment
                // Return a success response with the assessment details
                // Note: We're getting questions from the already loaded assessment
                var questionsDto = assessment.Questions.Select(q => new StudentQuestionDto
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    Type = (int)q.Type,
                    Options = q.GetOptionsAsList(),
                    Points = q.Points
                }).ToList();

                _logger.LogInformation($"Student {studentGuid} successfully joined live assessment {assessmentId} with {questionsDto.Count} questions");

                return Ok(new
                {
                    success = true,
                    message = "Successfully joined live assessment",
                    assessment = new
                    {
                        assessment.AssessmentId,
                        assessment.Title,
                        assessment.Description,
                        assessment.TimeLimit,
                        assessment.MaxScore,
                        assessment.StartedAt,
                        assessment.SessionId,
                        CourseName = assessment.Course.Title,
                        Questions = questionsDto
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining live assessment");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while joining the live assessment",
                    error = ex.Message
                });
            }
        }
        

        /// <summary>
        /// Get the status of a live assessment
        /// </summary>
        /// <param name="assessmentId">ID of the assessment to check</param>
        /// <returns>Assessment status information</returns>
        [HttpGet("live/status/{assessmentId}")]
        [Authorize]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<IActionResult> GetLiveAssessmentStatus(Guid assessmentId)
        {
            try
            {
                _logger.LogInformation($"Getting status for live assessment: {assessmentId}");
                
                var assessment = await _context.Assessments
                    .Include(a => a.Course)
                    .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);

                if (assessment == null)
                {
                    _logger.LogWarning($"Assessment not found: {assessmentId}");
                    return NotFound(new { success = false, message = "Assessment not found" });
                }

                // Check if assessment is currently live
                if (!assessment.IsLive || assessment.Status != AssessmentStatus.Live)
                {
                    _logger.LogWarning($"Assessment is not currently live: {assessmentId}, IsLive={assessment.IsLive}, Status={assessment.Status}");
                    return BadRequest(new { success = false, message = "Assessment is not currently live" });
                }

                // Get the number of students who have joined
                var joinedStudents = await _context.Results
                    .Where(r => r.AssessmentId == assessmentId)
                    .CountAsync();

                return Ok(new
                {
                    success = true,
                    assessment = new
                    {
                        assessment.AssessmentId,
                        assessment.Title,
                        assessment.Description,
                        assessment.TimeLimit,
                        assessment.MaxScore,
                        assessment.StartedAt,
                        assessment.SessionId,
                        CourseName = assessment.Course.Title,
                        IsLive = assessment.IsLive,
                        Status = assessment.Status,
                        JoinedStudents = joinedStudents
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live assessment status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while getting the live assessment status",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Endpoint to receive events from the frontend
        /// </summary>
        /// <param name="model">Event data from client</param>
        /// <returns>Success response if event was processed</returns>
        /// <response code="200">Event processed successfully</response>
        /// <response code="400">Invalid event data</response>
        /// <response code="401">User is not authorized</response>
        /// <response code="500">Server error</response>
        [HttpPost("events")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReceiveEvent([FromBody] EventModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.EventType))
                {
                    return BadRequest(new { success = false, message = "Invalid event data" });
                }

                _logger.LogInformation($"Received event: {model.EventType} with assessmentId: {model.AssessmentId}, questionId: {model.QuestionId}");
                
                // Get data dictionary for the event hub
                var eventData = model.EventData;
                _logger.LogInformation($"Event data: {System.Text.Json.JsonSerializer.Serialize(eventData)}");
                
                // Forward event to the event hub
                await _eventPublisher.PublishEventAsync(model.EventType, eventData);
                
                // If SignalR hub is available and we have a valid assessment ID, notify other participants
                if (!string.IsNullOrEmpty(model.AssessmentId))
                {
                    await _hubContext.Clients.Group(model.AssessmentId)
                        .SendAsync(model.EventType, eventData);
                    
                    _logger.LogInformation($"Forwarded event {model.EventType} to SignalR group: {model.AssessmentId}");
                }
                
                return Ok(new { success = true, message = $"Event {model.EventType} processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing event: {model.EventType}");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An error occurred while processing the event",
                    error = ex.Message
                });
            }
        }
    }
}
