using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VoiceInputAssistant.Core.Services.Interfaces;

namespace VoiceInputAssistant.WebApi.Hubs
{
    /// <summary>
    /// SignalR hub for real-time Voice Input Assistant updates
    /// </summary>
    [Authorize]
    public class VoiceAssistantHub : Hub
    {
        private readonly ILogger<VoiceAssistantHub> _logger;
        private readonly ISpeechRecognitionService _speechService;
        private readonly IUsageAnalyticsService _analyticsService;

        public VoiceAssistantHub(
            ILogger<VoiceAssistantHub> logger,
            ISpeechRecognitionService speechService,
            IUsageAnalyticsService analyticsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        }

        /// <summary>
        /// Called when a client connects
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client {ConnectionId} connected to VoiceAssistantHub", Context.ConnectionId);
            
            // Join user to their personal group for targeted messages
            var userEmail = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userEmail))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userEmail}");
            }

            // Send current system status to the newly connected client
            await SendSystemStatusUpdate();

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client {ConnectionId} disconnected from VoiceAssistantHub", Context.ConnectionId);
            
            var userEmail = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userEmail))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userEmail}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to recognition events
        /// </summary>
        public async Task SubscribeToRecognitionEvents()
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "RecognitionEvents");
                await Clients.Caller.SendAsync("Subscribed", "RecognitionEvents");
                _logger.LogInformation("Client {ConnectionId} subscribed to recognition events", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to recognition events for client {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Failed to subscribe to recognition events");
            }
        }

        /// <summary>
        /// Unsubscribe from recognition events
        /// </summary>
        public async Task UnsubscribeFromRecognitionEvents()
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "RecognitionEvents");
                await Clients.Caller.SendAsync("Unsubscribed", "RecognitionEvents");
                _logger.LogInformation("Client {ConnectionId} unsubscribed from recognition events", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from recognition events for client {ConnectionId}", Context.ConnectionId);
            }
        }

        /// <summary>
        /// Subscribe to analytics updates
        /// </summary>
        public async Task SubscribeToAnalyticsUpdates()
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "AnalyticsUpdates");
                await Clients.Caller.SendAsync("Subscribed", "AnalyticsUpdates");
                _logger.LogInformation("Client {ConnectionId} subscribed to analytics updates", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to analytics updates for client {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Failed to subscribe to analytics updates");
            }
        }

        /// <summary>
        /// Unsubscribe from analytics updates
        /// </summary>
        public async Task UnsubscribeFromAnalyticsUpdates()
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AnalyticsUpdates");
                await Clients.Caller.SendAsync("Unsubscribed", "AnalyticsUpdates");
                _logger.LogInformation("Client {ConnectionId} unsubscribed from analytics updates", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from analytics updates for client {ConnectionId}", Context.ConnectionId);
            }
        }

        /// <summary>
        /// Request current system status
        /// </summary>
        public async Task RequestSystemStatus()
        {
            try
            {
                await SendSystemStatusUpdate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending system status to client {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Failed to get system status");
            }
        }

        /// <summary>
        /// Start real-time recognition monitoring
        /// </summary>
        public async Task StartRecognitionMonitoring()
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "RecognitionMonitoring");
                await Clients.Caller.SendAsync("RecognitionMonitoringStarted");
                _logger.LogInformation("Client {ConnectionId} started recognition monitoring", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting recognition monitoring for client {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Failed to start recognition monitoring");
            }
        }

        /// <summary>
        /// Stop real-time recognition monitoring
        /// </summary>
        public async Task StopRecognitionMonitoring()
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "RecognitionMonitoring");
                await Clients.Caller.SendAsync("RecognitionMonitoringStopped");
                _logger.LogInformation("Client {ConnectionId} stopped recognition monitoring", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping recognition monitoring for client {ConnectionId}", Context.ConnectionId);
            }
        }

        /// <summary>
        /// Send a ping message (for connection testing)
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }

        /// <summary>
        /// Send current system status to the calling client
        /// </summary>
        private async Task SendSystemStatusUpdate()
        {
            try
            {
                var statusData = new
                {
                    IsListening = _speechService.IsListening,
                    IsInitialized = _speechService.IsInitialized,
                    CurrentEngine = _speechService.CurrentEngine?.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                await Clients.Caller.SendAsync("SystemStatusUpdate", statusData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending system status update");
            }
        }
    }

    /// <summary>
    /// Service for sending real-time updates through SignalR
    /// </summary>
    public interface IRealtimeNotificationService
    {
        Task SendRecognitionStartedAsync();
        Task SendRecognitionStoppedAsync();
        Task SendRecognitionResultAsync(string text, double confidence, string engine);
        Task SendRecognitionErrorAsync(string error);
        Task SendSystemStatusUpdateAsync();
        Task SendAnalyticsUpdateAsync();
        Task SendUserNotificationAsync(string userEmail, string message, string type = "info");
        Task SendGlobalNotificationAsync(string message, string type = "info");
    }

    /// <summary>
    /// Implementation of real-time notification service using SignalR
    /// </summary>
    public class RealtimeNotificationService : IRealtimeNotificationService
    {
        private readonly IHubContext<VoiceAssistantHub> _hubContext;
        private readonly ILogger<RealtimeNotificationService> _logger;
        private readonly ISpeechRecognitionService _speechService;
        private readonly IUsageAnalyticsService _analyticsService;

        public RealtimeNotificationService(
            IHubContext<VoiceAssistantHub> hubContext,
            ILogger<RealtimeNotificationService> logger,
            ISpeechRecognitionService speechService,
            IUsageAnalyticsService analyticsService)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        }

        public async Task SendRecognitionStartedAsync()
        {
            try
            {
                var data = new
                {
                    Event = "RecognitionStarted",
                    Timestamp = DateTime.UtcNow,
                    Engine = _speechService.CurrentEngine?.ToString()
                };

                await _hubContext.Clients.Groups("RecognitionEvents", "RecognitionMonitoring")
                    .SendAsync("RecognitionEvent", data);

                _logger.LogDebug("Sent recognition started event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending recognition started event");
            }
        }

        public async Task SendRecognitionStoppedAsync()
        {
            try
            {
                var data = new
                {
                    Event = "RecognitionStopped",
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Groups("RecognitionEvents", "RecognitionMonitoring")
                    .SendAsync("RecognitionEvent", data);

                _logger.LogDebug("Sent recognition stopped event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending recognition stopped event");
            }
        }

        public async Task SendRecognitionResultAsync(string text, double confidence, string engine)
        {
            try
            {
                var data = new
                {
                    Event = "RecognitionResult",
                    Text = text,
                    Confidence = confidence,
                    Engine = engine,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Groups("RecognitionEvents", "RecognitionMonitoring")
                    .SendAsync("RecognitionResult", data);

                _logger.LogDebug("Sent recognition result: {Text} (confidence: {Confidence})", text, confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending recognition result");
            }
        }

        public async Task SendRecognitionErrorAsync(string error)
        {
            try
            {
                var data = new
                {
                    Event = "RecognitionError",
                    Error = error,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Groups("RecognitionEvents", "RecognitionMonitoring")
                    .SendAsync("RecognitionError", data);

                _logger.LogDebug("Sent recognition error: {Error}", error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending recognition error");
            }
        }

        public async Task SendSystemStatusUpdateAsync()
        {
            try
            {
                var statusData = new
                {
                    IsListening = _speechService.IsListening,
                    IsInitialized = _speechService.IsInitialized,
                    CurrentEngine = _speechService.CurrentEngine?.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("SystemStatusUpdate", statusData);
                _logger.LogDebug("Sent system status update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending system status update");
            }
        }

        public async Task SendAnalyticsUpdateAsync()
        {
            try
            {
                // Get recent analytics summary
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddHours(-1); // Last hour summary

                var stats = await _analyticsService.GetUsageStatisticsAsync(startDate, endDate);

                var analyticsData = new
                {
                    Period = new { StartDate = startDate, EndDate = endDate },
                    TotalRecognitions = stats.TotalRecognitions,
                    AverageAccuracy = stats.AverageAccuracy,
                    AverageProcessingTime = stats.AverageProcessingTime.TotalMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Group("AnalyticsUpdates")
                    .SendAsync("AnalyticsUpdate", analyticsData);

                _logger.LogDebug("Sent analytics update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending analytics update");
            }
        }

        public async Task SendUserNotificationAsync(string userEmail, string message, string type = "info")
        {
            try
            {
                var notification = new
                {
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.Group($"User_{userEmail}")
                    .SendAsync("UserNotification", notification);

                _logger.LogDebug("Sent notification to user {UserEmail}: {Message}", userEmail, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending user notification to {UserEmail}", userEmail);
            }
        }

        public async Task SendGlobalNotificationAsync(string message, string type = "info")
        {
            try
            {
                var notification = new
                {
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("GlobalNotification", notification);
                _logger.LogDebug("Sent global notification: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending global notification");
            }
        }
    }
}