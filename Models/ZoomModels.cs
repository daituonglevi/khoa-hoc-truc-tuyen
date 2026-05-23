namespace ELearningWebsite.Models
{
    /// <summary>
    /// Request model for creating/updating Zoom meeting
    /// </summary>
    public class ZoomMeetingRequest
    {
        public string Topic { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; } = 60;
        public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
        public string Password { get; set; } = string.Empty;
        public bool RecordingEnabled { get; set; } = true;
        public int? MaxParticipants { get; set; }
    }

    /// <summary>
    /// Response from Zoom API when creating meeting
    /// </summary>
    public class ZoomMeetingResponse
    {
        public string MeetingId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string JoinUrl { get; set; } = string.Empty;
        public string StartUrl { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = string.Empty; // notstarted, live, ended
        public string CreatedAt { get; set; } = string.Empty;
        public string UUID { get; set; } = string.Empty;
        public int HostId { get; set; }
    }

    /// <summary>
    /// Recording information from Zoom
    /// </summary>
    public class ZoomRecordingResponse
    {
        public string RecordingId { get; set; } = string.Empty;
        public string MeetingId { get; set; } = string.Empty;
        public string RecordingUrl { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public DateTime RecordingStart { get; set; }
        public DateTime RecordingEnd { get; set; }
        public long DurationSeconds { get; set; }
        public long FileSize { get; set; }
        public string Status { get; set; } = string.Empty; // completed, processing
        public string RecordingType { get; set; } = string.Empty; // shared_screen_with_speaker_video, shared_screen_with_speaker_video_v2, etc
    }

    /// <summary>
    /// Webhook event from Zoom
    /// </summary>
    public class ZoomWebhookEvent
    {
        public string Event { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public ZoomWebhookPayload Payload { get; set; } = new();
    }

    public class ZoomWebhookPayload
    {
        public string AccountId { get; set; } = string.Empty;
        public ZoomWebhookObject Object { get; set; } = new();
    }

    public class ZoomWebhookObject
    {
        public string Id { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Uuid { get; set; } = string.Empty;
        public string HostId { get; set; } = string.Empty;
        public string HostEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public long Duration { get; set; }
        public List<ZoomRecording> RecordingFiles { get; set; } = new();
    }

    public class ZoomRecording
    {
        public string Id { get; set; } = string.Empty;
        public string MeetingId { get; set; } = string.Empty;
        public string RecordingStart { get; set; } = string.Empty;
        public string RecordingEnd { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // MP4, M3U8, etc
        public long FileSize { get; set; }
        public string PlayUrl { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string RecordingType { get; set; } = string.Empty; // shared_screen_with_speaker_video, video, etc
        public string Status { get; set; } = string.Empty; // completed, processing
    }
}
