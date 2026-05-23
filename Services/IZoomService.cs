using ELearningWebsite.Models;

namespace ELearningWebsite.Services
{
    /// <summary>
    /// Interface for Zoom API integration
    /// </summary>
    public interface IZoomService
    {
        /// <summary>
        /// Create a new meeting/webinar on Zoom
        /// </summary>
        Task<ZoomMeetingResponse> CreateMeetingAsync(ZoomMeetingRequest request);

        /// <summary>
        /// Get meeting details from Zoom
        /// </summary>
        Task<ZoomMeetingResponse?> GetMeetingAsync(string meetingId);

        /// <summary>
        /// Update an existing meeting
        /// </summary>
        Task<bool> UpdateMeetingAsync(string meetingId, ZoomMeetingRequest request);

        /// <summary>
        /// Delete a meeting
        /// </summary>
        Task<bool> DeleteMeetingAsync(string meetingId);

        /// <summary>
        /// Get recording details for a meeting
        /// </summary>
        Task<ZoomRecordingResponse?> GetRecordingAsync(string meetingId);

        /// <summary>
        /// Get list of recordings for a user
        /// </summary>
        Task<List<ZoomRecordingResponse>> GetUserRecordingsAsync(string userId, int pageSize = 30);

        /// <summary>
        /// Verify webhook signature from Zoom
        /// </summary>
        bool VerifyWebhookSignature(string messageId, string timestamp, string signature, string body);
    }
}
