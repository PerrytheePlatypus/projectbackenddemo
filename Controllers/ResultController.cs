using EduSync.Data;
using EduSync.DTOs;
using EduSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using System.Text.Json;

namespace EduSync.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ResultController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ResultController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserResults()
        {
            try 
            {
                var userId = User.FindFirst("UserId")?.Value;
                var userGuid = Guid.Parse(userId);

                Console.WriteLine($"[GetUserResults] Fetching results for user {userGuid}");

                // Log all results for debugging
                var allResults = await _context.Results
                    .Include(r => r.Assessment)
                        .ThenInclude(a => a.Course)
                    .Where(r => r.UserId == userGuid)
                    .ToListAsync();

                Console.WriteLine($"[GetUserResults] Found {allResults.Count} total results for user {userGuid}");
                foreach (var r in allResults)
                {
                    Console.WriteLine($"[GetUserResults] Result: ID={r.ResultId}, AssessmentID={r.AssessmentId}, " +
                                    $"IsCompleted={r.IsCompleted}, Score={r.Score}, " +
                                    $"AttemptDate={r.AttemptDate}, CompletedDate={r.CompletedDate}");
                }

                var results = allResults
                    .OrderByDescending(r => r.IsCompleted ? r.CompletedDate : r.AttemptDate)
                    .Select(r => new ResultDto
                    {
                        ResultId = r.ResultId,
                        AssessmentId = r.AssessmentId,
                        AssessmentTitle = r.Assessment?.Title ?? "[No Title]",
                        CourseId = r.Assessment?.CourseId ?? Guid.Empty,
                        CourseTitle = r.Assessment?.Course?.Title ?? "[No Course]",
                        AttemptDate = r.AttemptDate,
                        CompletedDate = r.CompletedDate,
                        Score = r.Score,
                        IsCompleted = r.IsCompleted,
                        MaxScore = r.Assessment?.MaxScore ?? 0,
                        ScorePercentage = r.Assessment?.MaxScore > 0 
                            ? (double)r.Score / r.Assessment.MaxScore * 100 
                            : 0
                    })
                    .ToList();


                Console.WriteLine($"[GetUserResults] Returning {results.Count} results for user {userGuid}");
                foreach (var r in results)
                {
                    Console.WriteLine($"[GetUserResults] Returning - ID={r.ResultId}, " +
                                  $"AssessmentID={r.AssessmentId}, IsCompleted={r.IsCompleted}");
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetUserResults] Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while fetching results" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetResult(Guid id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);
            var isInstructor = User.IsInRole("Instructor");

            // Get the result with related data
            var result = await _context.Results
                .Include(r => r.Assessment)
                    .ThenInclude(a => a.Course)
                .Include(r => r.Assessment.Questions)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound(new { message = "Result not found" });
            }

            // Check access based on role
            if (isInstructor)
            {
                // Verify the instructor owns the course associated with this result
                if (result.Assessment.Course.InstructorId != userGuid)
                {
                    return NotFound(new { message = "You don't have access to this result" });
                }

                // Prepare question results if answers exist
                var questionResults = new List<QuestionResultDto>();
                var answers = !string.IsNullOrEmpty(result.Answers) 
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(result.Answers) 
                    : new Dictionary<string, string>();
                
                foreach (var question in result.Assessment.Questions)
                {
                    string userAnswer = answers.ContainsKey(question.QuestionId.ToString()) 
                        ? answers[question.QuestionId.ToString()] 
                        : string.Empty;
                    
                    questionResults.Add(new QuestionResultDto
                    {
                        QuestionId = question.QuestionId,
                        QuestionText = question.QuestionText,
                        Options = JsonSerializer.Deserialize<List<string>>(question.Options),
                        CorrectAnswer = question.CorrectAnswer,
                        UserAnswer = userAnswer,
                        IsCorrect = userAnswer == question.CorrectAnswer,
                        Points = question.Points,
                        EarnedPoints = userAnswer == question.CorrectAnswer ? question.Points : 0
                    });
                }
                
                var resultDetailDto = new ResultDetailDto
                {
                    ResultId = result.ResultId,
                    AssessmentId = result.AssessmentId,
                    AssessmentTitle = result.Assessment.Title,
                    CourseId = result.Assessment.CourseId,
                    CourseTitle = result.Assessment.Course.Title,
                    AttemptDate = result.AttemptDate,
                    CompletedDate = result.CompletedDate,
                    Score = result.Score,
                    MaxScore = result.Assessment.MaxScore,
                    IsCompleted = result.IsCompleted,
                    ScorePercentage = result.Assessment.MaxScore > 0 
                        ? (double)result.Score / result.Assessment.MaxScore * 100 
                        : 0,
                    QuestionResults = questionResults
                };

                return Ok(resultDetailDto);
            }
            else
            {
                // Students can only view their own results
                if (result.UserId != userGuid)
                {
                    return NotFound(new { message = "You don't have access to this result" });
                }

                // Prepare question results if answers exist
                var questionResults = new List<QuestionResultDto>();
                var answers = !string.IsNullOrEmpty(result.Answers) 
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(result.Answers) 
                    : new Dictionary<string, string>();
                
                foreach (var question in result.Assessment.Questions)
                {
                    string userAnswer = answers.ContainsKey(question.QuestionId.ToString()) 
                        ? answers[question.QuestionId.ToString()] 
                        : string.Empty;
                    
                    questionResults.Add(new QuestionResultDto
                    {
                        QuestionId = question.QuestionId,
                        QuestionText = question.QuestionText,
                        Options = JsonSerializer.Deserialize<List<string>>(question.Options),
                        CorrectAnswer = question.CorrectAnswer,
                        UserAnswer = userAnswer,
                        IsCorrect = userAnswer == question.CorrectAnswer,
                        Points = question.Points,
                        EarnedPoints = userAnswer == question.CorrectAnswer ? question.Points : 0
                    });
                }
                
                var resultDetailDto = new ResultDetailDto
                {
                    ResultId = result.ResultId,
                    AssessmentId = result.AssessmentId,
                    AssessmentTitle = result.Assessment.Title,
                    CourseId = result.Assessment.CourseId,
                    CourseTitle = result.Assessment.Course.Title,
                    AttemptDate = result.AttemptDate,
                    CompletedDate = result.CompletedDate,
                    Score = result.Score,
                    MaxScore = result.Assessment.MaxScore,
                    IsCompleted = result.IsCompleted,
                    ScorePercentage = result.Assessment.MaxScore > 0 
                        ? (double)result.Score / result.Assessment.MaxScore * 100 
                        : 0,
                    QuestionResults = questionResults
                };

                return Ok(resultDetailDto);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitResult([FromBody] ResultSubmitDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Find the assessment attempt that's in progress
            var resultAttempt = await _context.Results
                .Include(r => r.Assessment)
                    .ThenInclude(a => a.Questions)
                .FirstOrDefaultAsync(r => r.AssessmentId == model.AssessmentId && 
                                        r.UserId == userGuid && 
                                        !r.IsCompleted);

            if (resultAttempt == null)
            {
                return BadRequest(new { message = "No active assessment attempt found" });
            }

            // Prepare to calculate score
            int totalScore = 0;

            // Process answers and calculate score
            var answers = new Dictionary<string, string>();
            if (model.Answers != null)
            {
                foreach (var answer in model.Answers)
                {
                    // Find corresponding question
                    var question = resultAttempt.Assessment.Questions
                        .FirstOrDefault(q => q.QuestionId.ToString() == answer.Key);

                    if (question != null)
                    {
                        // Store the answer
                        answers[answer.Key] = answer.Value;

                        // For now, we'll consider all answers correct for simplicity
                        // In a real app, you'd validate the answer against correct ones
                        totalScore += question.Points;
                    }
                }
            }

            // Update the result
            resultAttempt.Answers = JsonSerializer.Serialize(answers);
            resultAttempt.Score = totalScore;
            resultAttempt.IsCompleted = true;
            resultAttempt.CompletedDate = DateTime.UtcNow;

            // Also update the assessment status to completed for this user
            var assessment = await _context.Assessments
                .FirstOrDefaultAsync(a => a.AssessmentId == model.AssessmentId);
                
            if (assessment != null)
            {
                // Mark the assessment as completed
                assessment.Status = AssessmentStatus.Completed;
                
                // If this was a live assessment, mark it as not live anymore
                if (assessment.IsLive)
                {
                    assessment.IsLive = false;
                }
                
                Console.WriteLine($"Marked assessment {assessment.AssessmentId} as completed. Status: {assessment.Status}");
            }
            else
            {
                Console.WriteLine($"Warning: Could not find assessment with ID {model.AssessmentId} to mark as completed");
            }

            await _context.SaveChangesAsync();

            // Return the result ID in the response
            return Ok(new { 
                message = "Assessment submitted successfully",
                resultId = resultAttempt.ResultId,
                isCompleted = resultAttempt.IsCompleted
            });
        }

        [HttpGet("assessment/{assessmentId}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> GetAssessmentResults(Guid assessmentId)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Verify the instructor owns the assessment
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);

            if (assessment == null)
            {
                return NotFound(new { message = "Assessment not found" });
            }

            if (assessment.Course.InstructorId != userGuid)
            {
                return NotFound(new { message = "You don't have access to this assessment" });
            }

            // Get all completed results for this assessment
            var results = await _context.Results
                .Include(r => r.User)
                .Where(r => r.AssessmentId == assessmentId && r.IsCompleted)
                .OrderByDescending(r => r.CompletedDate)
                .Select(r => new StudentResultDto
                {
                    UserId = r.UserId,
                    StudentName = r.User.Name,
                    AttemptDate = r.AttemptDate,
                    CompletedDate = r.CompletedDate,
                    Score = r.Score,
                    ScorePercentage = assessment.MaxScore > 0 
                        ? (double)r.Score / assessment.MaxScore * 100 
                        : 0,
                    IsCompleted = r.IsCompleted
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("course/{courseId}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> GetCourseResults(Guid courseId)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Verify the instructor owns the course
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.InstructorId == userGuid);

            if (course == null)
            {
                return NotFound(new { message = "Course not found or you don't have access to it" });
            }

            // Get all completed results for all assessments in this course
            var results = await _context.Results
                .Include(r => r.Assessment)
                .Include(r => r.User)
                .Where(r => r.Assessment.CourseId == courseId && r.IsCompleted)
                .OrderByDescending(r => r.CompletedDate)
                .Select(r => new ResultDto
                {
                    ResultId = r.ResultId,
                    AssessmentId = r.AssessmentId,
                    AssessmentTitle = r.Assessment.Title,
                    CourseId = r.Assessment.CourseId,
                    CourseTitle = r.Assessment.Course.Title,
                    AttemptDate = r.AttemptDate,
                    CompletedDate = r.CompletedDate,
                    Score = r.Score,
                    MaxScore = r.Assessment.MaxScore,
                    IsCompleted = r.IsCompleted,
                    ScorePercentage = r.Assessment.MaxScore > 0 
                        ? (double)r.Score / r.Assessment.MaxScore * 100 
                        : 0
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("student/{studentId}/course/{courseId}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> GetStudentCourseResults(Guid studentId, Guid courseId)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userGuid = Guid.Parse(userId);

            // Verify the instructor owns the course
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.InstructorId == userGuid);

            if (course == null)
            {
                return NotFound(new { message = "Course not found or you don't have access to it" });
            }

            // Verify the student exists
            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Get all completed results for this student in this course
            var results = await _context.Results
                .Include(r => r.Assessment)
                .Where(r => r.Assessment.CourseId == courseId && r.UserId == studentId && r.IsCompleted)
                .OrderByDescending(r => r.CompletedDate)
                .Select(r => new ResultDto
                {
                    ResultId = r.ResultId,
                    AssessmentId = r.AssessmentId,
                    AssessmentTitle = r.Assessment.Title,
                    CourseId = r.Assessment.CourseId,
                    CourseTitle = r.Assessment.Course.Title,
                    AttemptDate = r.AttemptDate,
                    CompletedDate = r.CompletedDate,
                    Score = r.Score,
                    MaxScore = r.Assessment.MaxScore,
                    IsCompleted = r.IsCompleted,
                    ScorePercentage = r.Assessment.MaxScore > 0 
                        ? (double)r.Score / r.Assessment.MaxScore * 100 
                        : 0
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
