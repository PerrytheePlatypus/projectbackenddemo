using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace EduSyncServer.Models
{
    /// <summary>
    /// Model for events sent from client to server
    /// </summary>
    public class EventModel
    {
        /// <summary>
        /// Type of event being sent
        /// </summary>
        [Required]
        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = string.Empty;
        
        /// <summary>
        /// Assessment ID associated with the event
        /// </summary>
        [JsonPropertyName("assessmentId")]
        public string AssessmentId { get; set; } = string.Empty;
        
        /// <summary>
        /// Question ID if applicable
        /// </summary>
        [JsonPropertyName("questionId")]
        public string QuestionId { get; set; } = string.Empty;
        
        /// <summary>
        /// Answer data if applicable
        /// </summary>
        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp of the event
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Additional data in JSON format
        /// </summary>
        [JsonPropertyName("additionalData")]
        public string AdditionalData { get; set; } = "{}";
        
        /// <summary>
        /// Helper method to get a Dictionary representation of the event data
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> EventData
        {
            get
            {
                var result = new Dictionary<string, object>
                {
                    { "assessmentId", AssessmentId },
                    { "questionId", QuestionId },
                    { "answer", Answer },
                    { "timestamp", Timestamp.ToString("o") }
                };
                
                // Add any additional data if present
                if (!string.IsNullOrEmpty(AdditionalData) && AdditionalData != "{}")
                {
                    try
                    {
                        var additionalDict = JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalData);
                        if (additionalDict != null)
                        {
                            foreach (var kvp in additionalDict)
                            {
                                result[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    catch
                    {
                        // Just continue if there's an error parsing the JSON
                    }
                }
                
                return result;
            }
        }
    }
}
