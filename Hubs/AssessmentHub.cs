using EduSync.Data;
using EduSync.EventHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EduSync.Hubs
{
    // Temporarily removing [Authorize] to test connectivity
    // [Authorize]
    public class AssessmentHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<AssessmentHub> _logger;

        public AssessmentHub(
            ApplicationDbContext context,
            IEventPublisher eventPublisher,
            ILogger<AssessmentHub> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                // Look for UserId claim in multiple formats based on JWT configuration
                var userId = Context.User?.FindFirst("UserId")?.Value ?? 
                             Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                             "anonymous";
                             
                var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "anonymous";
                
                _logger.LogInformation($"User connected: {userId}, Role: {userRole}, ConnectionId: {Context.ConnectionId}");
                
                // Add connection to a general group for all users
                await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
                
                // Notify the client that they are connected successfully
                // Use lowercase method name to match client expectations
                await Clients.Caller.SendAsync("connectionestablished", new { connectionId = Context.ConnectionId });
                
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in OnConnectedAsync: {ex.Message}");
                // Continue without throwing to prevent connection termination
                await base.OnConnectedAsync();
            }
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                _logger.LogInformation($"User disconnected: {userId}, ConnectionId: {Context.ConnectionId}");
                
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync");
                throw;
            }
        }

        public async Task JoinAssessment(string assessmentId)
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning($"Unauthorized user attempted to join assessment: {assessmentId}");
                    return;
                }

                // Add connection to the assessment group
                await Groups.AddToGroupAsync(Context.ConnectionId, assessmentId);
                _logger.LogInformation($"User {userId} joined assessment: {assessmentId}");
                
                // Publish event to Event Hub
                await _eventPublisher.PublishEventAsync("StudentJoinedAssessment", new
                {
                    AssessmentId = assessmentId,
                    UserId = userId,
                    ConnectionId = Context.ConnectionId,
                    Timestamp = DateTime.UtcNow
                });
                
                // Notify other clients in the group
                await Clients.OthersInGroup(assessmentId).SendAsync(
                    "StudentJoined", 
                    new { UserId = userId, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joining assessment {assessmentId}");
            }
        }
        
        public async Task SubmitAnswer(string assessmentId, string questionId, string answer)
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning($"Unauthorized user attempted to submit answer");
                    return;
                }
                
                _logger.LogInformation($"User {userId} submitted answer for question {questionId} in assessment {assessmentId}");
                
                // Publish event to Event Hub
                await _eventPublisher.PublishEventAsync("AnswerSubmitted", new
                {
                    AssessmentId = assessmentId,
                    QuestionId = questionId,
                    UserId = userId,
                    Answer = answer,
                    Timestamp = DateTime.UtcNow
                });
                
                // If instructor is connected, notify them
                await Clients.Group(assessmentId).SendAsync(
                    "AnswerSubmitted", 
                    new { UserId = userId, QuestionId = questionId, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting answer for assessment {assessmentId}, question {questionId}");
            }
        }
        
        public async Task LeaveAssessment(string assessmentId)
        {
            try
            {
                var userId = Context.User?.FindFirst("UserId")?.Value;
                
                // Remove connection from the assessment group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, assessmentId);
                _logger.LogInformation($"User {userId} left assessment: {assessmentId}");
                
                // Publish event to Event Hub
                await _eventPublisher.PublishEventAsync("StudentLeftAssessment", new
                {
                    AssessmentId = assessmentId,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });
                
                // Notify other clients in the group
                await Clients.OthersInGroup(assessmentId).SendAsync(
                    "StudentLeft", 
                    new { UserId = userId, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error leaving assessment {assessmentId}");
            }
        }
    }
}
