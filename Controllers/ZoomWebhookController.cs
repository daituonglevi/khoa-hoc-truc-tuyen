using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using ELearningWebsite.Services;
using System.Text.Json;

namespace ELearningWebsite.Controllers
{
    /// <summary>
    /// Webhook handler for Zoom events (recordings, meetings, etc)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ZoomWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IZoomService _zoomService;
        private readonly IPrivateBlobStorageService _blobService;
        private readonly ILogger<ZoomWebhookController> _logger;

        public ZoomWebhookController(
            ApplicationDbContext context,
            IZoomService zoomService,
            IPrivateBlobStorageService blobService,
            ILogger<ZoomWebhookController> logger)
        {
            _context = context;
            _zoomService = zoomService;
            _blobService = blobService;
            _logger = logger;
        }

        /// <summary>
        /// Handle Zoom webhook events
        /// </summary>
        [HttpPost("events")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleZoomEvent()
        {
            try
            {
                // Read raw body for signature verification
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                Request.Body.Position = 0;

                // Verify webhook signature
                var messageId = Request.Headers["x-zm-request-id"].ToString();
                var timestamp = Request.Headers["x-zm-request-timestamp"].ToString();
                var signature = Request.Headers["x-zm-signature"].ToString();

                if (!_zoomService.VerifyWebhookSignature(messageId, timestamp, signature, body))
                {
                    _logger.LogWarning("Invalid Zoom webhook signature");
                    return Unauthorized(new { error = "Invalid signature" });
                }

                // Parse webhook payload
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var eventType = root.GetProperty("event").GetString();

                _logger.LogInformation($"Received Zoom webhook event: {eventType}");

                // Handle different event types
                switch (eventType)
                {
                    case "recording.completed":
                        await HandleRecordingCompleted(root);
                        break;
                    case "meeting.started":
                        await HandleMeetingStarted(root);
                        break;
                    case "meeting.ended":
                        await HandleMeetingEnded(root);
                        break;
                    default:
                        _logger.LogInformation($"Unhandled Zoom event type: {eventType}");
                        break;
                }

                return Ok(new { message = "Event received" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling Zoom webhook: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Handle recording.completed event from Zoom
        /// </summary>
        private async Task HandleRecordingCompleted(JsonElement payload)
        {
            try
            {
                if (!payload.TryGetProperty("payload", out var payloadElement))
                    return;

                var zoomObject = payloadElement.GetProperty("object");
                var meetingId = zoomObject.GetProperty("id").GetString();
                var recordingFiles = zoomObject.GetProperty("recording_files");

                _logger.LogInformation($"Recording completed for Zoom meeting: {meetingId}");

                // Find LiveClass by ZoomMeetingId
                var liveClass = await _context.LiveClasses
                    .FirstOrDefaultAsync(lc => lc.ZoomMeetingId == meetingId);

                if (liveClass == null)
                {
                    _logger.LogWarning($"No LiveClass found for Zoom meeting {meetingId}");
                    return;
                }

                // Process each recording file
                foreach (var file in recordingFiles.EnumerateArray())
                {
                    var recordingId = file.GetProperty("id").GetString();
                    var recordingUrl = file.GetProperty("play_url").GetString();
                    var downloadUrl = file.GetProperty("download_url").GetString();
                    var fileSize = file.GetProperty("file_size").GetInt64();
                    var recordingType = file.GetProperty("recording_type").GetString();

                    // Only process video recordings (not chat/transcript files)
                    if (recordingType != "shared_screen_with_speaker_video" && 
                        recordingType != "shared_screen_with_speaker_video_v2" &&
                        recordingType != "video")
                    {
                        _logger.LogInformation($"Skipping non-video recording file: {recordingType}");
                        continue;
                    }

                    // Save recording metadata to database
                    var recording = new LiveClassRecording
                    {
                        LiveClassId = liveClass.Id,
                        ExternalRecordingId = recordingId,
                        RecordingUrl = recordingUrl,
                        DownloadUrl = downloadUrl,
                        FileSizeBytes = fileSize,
                        Format = file.GetProperty("file_type").GetString() ?? "mp4",
                        Provider = "Zoom",
                        Status = "Ready",
                        IsPublic = liveClass.IsRecordingPublic,
                        IsDownloadable = false,
                        AvailableAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.LiveClassRecordings.Add(recording);

                    _logger.LogInformation($"Recording saved for LiveClass {liveClass.Id}: {recordingId}");
                }

                // Update LiveClass status
                liveClass.RecordingUrl = recordingFiles.EnumerateArray()
                    .FirstOrDefault().GetProperty("play_url").GetString();
                liveClass.Status = "Completed";
                liveClass.UpdatedAt = DateTime.UtcNow;

                _context.LiveClasses.Update(liveClass);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"LiveClass {liveClass.Id} status updated to Completed");

                // TODO: Send email to enrolled students notifying recording is available
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling recording.completed event: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle meeting.started event
        /// </summary>
        private async Task HandleMeetingStarted(JsonElement payload)
        {
            try
            {
                if (!payload.TryGetProperty("payload", out var payloadElement))
                    return;

                var zoomObject = payloadElement.GetProperty("object");
                var meetingId = zoomObject.GetProperty("id").GetString();

                _logger.LogInformation($"Zoom meeting started: {meetingId}");

                // Find and update LiveClass status
                var liveClass = await _context.LiveClasses
                    .FirstOrDefaultAsync(lc => lc.ZoomMeetingId == meetingId);

                if (liveClass != null)
                {
                    liveClass.Status = "Live";
                    liveClass.ActualStartTime = DateTime.UtcNow;
                    liveClass.UpdatedAt = DateTime.UtcNow;

                    _context.LiveClasses.Update(liveClass);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"LiveClass {liveClass.Id} status updated to Live");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling meeting.started event: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle meeting.ended event
        /// </summary>
        private async Task HandleMeetingEnded(JsonElement payload)
        {
            try
            {
                if (!payload.TryGetProperty("payload", out var payloadElement))
                    return;

                var zoomObject = payloadElement.GetProperty("object");
                var meetingId = zoomObject.GetProperty("id").GetString();

                _logger.LogInformation($"Zoom meeting ended: {meetingId}");

                // Find and update LiveClass
                var liveClass = await _context.LiveClasses
                    .FirstOrDefaultAsync(lc => lc.ZoomMeetingId == meetingId);

                if (liveClass != null)
                {
                    liveClass.ActualEndTime = DateTime.UtcNow;
                    
                    // Calculate actual duration if started
                    if (liveClass.ActualStartTime.HasValue)
                    {
                        var duration = (liveClass.ActualEndTime.Value - liveClass.ActualStartTime.Value).TotalMinutes;
                        liveClass.RecordingDurationSeconds = (long)(duration * 60);
                    }

                    // Don't mark as Completed yet - wait for recording to be ready
                    liveClass.UpdatedAt = DateTime.UtcNow;

                    _context.LiveClasses.Update(liveClass);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"LiveClass {liveClass.Id} ended");

                    // Mark all attendees who joined but didn't leave as "Attended"
                    var pendingAttendees = await _context.LiveClassAttendances
                        .Where(a => a.LiveClassId == liveClass.Id && a.Status == "Pending" && a.JoinedAt.HasValue)
                        .ToListAsync();

                    foreach (var attendee in pendingAttendees)
                    {
                        attendee.Status = "Attended";
                        attendee.LeftAt = liveClass.ActualEndTime;
                        attendee.DurationMinutes = (int)((liveClass.ActualEndTime.Value - attendee.JoinedAt.Value).TotalMinutes);
                        attendee.UpdatedAt = DateTime.UtcNow;
                    }

                    if (pendingAttendees.Any())
                    {
                        _context.LiveClassAttendances.UpdateRange(pendingAttendees);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling meeting.ended event: {ex.Message}");
            }
        }
    }
}
