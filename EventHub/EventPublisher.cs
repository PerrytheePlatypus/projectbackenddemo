using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using EduSync.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduSync.EventHub
{
    public interface IEventPublisher
    {
        Task PublishEventAsync(string eventType, object data);
    }

    public class EventHubPublisher : IEventPublisher
    {
        private readonly EventHubProducerClient _producerClient;
        private readonly ILogger<EventHubPublisher> _logger;
        private readonly bool _isEventHubEnabled;

        public EventHubPublisher(IOptions<EventHubOptions> options, ILogger<EventHubPublisher> logger)
        {
            _logger = logger;
            _isEventHubEnabled = !string.IsNullOrEmpty(options.Value?.ConnectionString) && 
                                !string.IsNullOrEmpty(options.Value?.EventHubName);

            if (_isEventHubEnabled)
            {
                try
                {
                    _producerClient = new EventHubProducerClient(
                        options.Value.ConnectionString,
                        options.Value.EventHubName);
                    
                    _logger.LogInformation($"EventHubPublisher initialized with hub name: {options.Value.EventHubName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize EventHubProducerClient. Events will be logged only.");
                    _isEventHubEnabled = false;
                }
            }
            else
            {
                _logger.LogWarning("Event Hub is not configured. Events will be logged only.");
            }
        }

        public async Task PublishEventAsync(string eventType, object data)
        {
            try
            {
                // Always log the event
                _logger.LogInformation($"Event: {eventType}, Data: {JsonSerializer.Serialize(data)}");

                if (!_isEventHubEnabled)
                {
                    _logger.LogWarning($"Event Hub is not enabled. Event {eventType} was logged but not published.");
                    return;
                }

                // Create a batch of events
                using EventDataBatch eventBatch = await _producerClient.CreateBatchAsync();

                // Create the event with serialized data
                var eventData = new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)));
                
                // Add metadata to the event
                eventData.Properties.Add("EventType", eventType);
                eventData.Properties.Add("Timestamp", DateTime.UtcNow.ToString("o"));

                // Add the event to the batch
                if (!eventBatch.TryAdd(eventData))
                {
                    _logger.LogError($"Event too large to fit in the batch: {eventType}");
                    return;
                }

                // Send the batch to the event hub
                await _producerClient.SendAsync(eventBatch);
                _logger.LogInformation($"Published event: {eventType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish event: {eventType}. Event was logged but not published to Event Hub.");
                // Don't throw the exception - we want the application to continue even if Event Hub is down
            }
        }
    }
}
