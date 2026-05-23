using ELearningWebsite.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ELearningWebsite.Services
{
    /// <summary>
    /// Zoom API Service for creating/managing meetings and recordings
    /// </summary>
    public class ZoomService : IZoomService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ZoomService> _logger;
        private readonly ZoomOptions _options;

        public ZoomService(HttpClient httpClient, IOptions<ZoomOptions> options, ILogger<ZoomService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<ZoomMeetingResponse> CreateMeetingAsync(ZoomMeetingRequest request)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var meetingPayload = new
                {
                    topic = request.Topic,
                    type = 2, // Scheduled Meeting
                    start_time = request.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    duration = request.DurationMinutes,
                    timezone = request.TimeZone,
                    password = request.Password,
                    settings = new
                    {
                        host_video = true,
                        participant_video = true,
                        join_before_host = true,
                        auto_recording = request.RecordingEnabled ? "cloud" : "none",
                        allow_multiple_devices = true,
                        waiting_room = false
                    }
                };

                var json = JsonSerializer.Serialize(meetingPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Get Zoom user ID (account owner or your app's designated user)
                var userId = _options.ZoomUserId ?? "me"; // "me" represents the authenticated user

                var response = await _httpClient.PostAsync($"https://api.zoom.us/v2/users/{userId}/meetings", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<dynamic>(responseContent, options);
                    
                    _logger.LogInformation($"Zoom meeting created successfully");
                    return ParseZoomResponse(result);
                }

                _logger.LogError($"Zoom API error: {responseContent}");
                throw new Exception($"Failed to create Zoom meeting: {responseContent}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating Zoom meeting: {ex.Message}");
                throw;
            }
        }

        public async Task<ZoomMeetingResponse?> GetMeetingAsync(string meetingId)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"https://api.zoom.us/v2/meetings/{meetingId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<dynamic>(responseContent, options);
                    return ParseZoomResponse(result);
                }

                _logger.LogWarning($"Could not retrieve Zoom meeting {meetingId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting Zoom meeting: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateMeetingAsync(string meetingId, ZoomMeetingRequest request)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var meetingPayload = new
                {
                    topic = request.Topic,
                    start_time = request.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    duration = request.DurationMinutes,
                    timezone = request.TimeZone,
                    password = request.Password
                };

                var json = JsonSerializer.Serialize(meetingPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request_update = new HttpRequestMessage(HttpMethod.Put, $"https://api.zoom.us/v2/meetings/{meetingId}")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request_update);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating Zoom meeting: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteMeetingAsync(string meetingId)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync($"https://api.zoom.us/v2/meetings/{meetingId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting Zoom meeting: {ex.Message}");
                return false;
            }
        }

        public async Task<ZoomRecordingResponse?> GetRecordingAsync(string meetingId)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"https://api.zoom.us/v2/meetings/{meetingId}/recordings");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parse and return first recording file
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("recording_files", out var files) && files.GetArrayLength() > 0)
                    {
                        var firstFile = files[0];
                        return new ZoomRecordingResponse
                        {
                            RecordingId = firstFile.GetProperty("id").GetString() ?? "",
                            MeetingId = meetingId,
                            RecordingUrl = firstFile.GetProperty("play_url").GetString() ?? "",
                            DownloadUrl = firstFile.GetProperty("download_url").GetString() ?? "",
                            FileSize = firstFile.GetProperty("file_size").GetInt64(),
                            Status = firstFile.GetProperty("status").GetString() ?? "processing"
                        };
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recording: {ex.Message}");
                return null;
            }
        }

        public async Task<List<ZoomRecordingResponse>> GetUserRecordingsAsync(string userId, int pageSize = 30)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var recordings = new List<ZoomRecordingResponse>();
                var response = await _httpClient.GetAsync($"https://api.zoom.us/v2/users/{userId}/recordings?page_size={pageSize}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("meetings", out var meetings))
                    {
                        foreach (var meeting in meetings.EnumerateArray())
                        {
                            if (meeting.TryGetProperty("recording_files", out var files))
                            {
                                foreach (var file in files.EnumerateArray())
                                {
                                    recordings.Add(new ZoomRecordingResponse
                                    {
                                        RecordingId = file.GetProperty("id").GetString() ?? "",
                                        MeetingId = meeting.GetProperty("id").GetString() ?? "",
                                        RecordingUrl = file.GetProperty("play_url").GetString() ?? "",
                                        DownloadUrl = file.GetProperty("download_url").GetString() ?? "",
                                        FileSize = file.GetProperty("file_size").GetInt64(),
                                        Status = file.GetProperty("status").GetString() ?? "processing"
                                    });
                                }
                            }
                        }
                    }
                }

                return recordings;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user recordings: {ex.Message}");
                return new List<ZoomRecordingResponse>();
            }
        }

        public bool VerifyWebhookSignature(string messageId, string timestamp, string signature, string body)
        {
            try
            {
                // Zoom webhook signature verification
                var token = $"v0:{messageId}:{timestamp}:{body}";
                var hash = ComputeHmacSha256(token, _options.WebhookSecretToken);
                var computedSignature = $"v0={hash}";

                return computedSignature.Equals(signature);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error verifying webhook signature: {ex.Message}");
                return false;
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            // Implement OAuth 2.0 Server-to-Server flow
            // This is a simplified implementation - in production, cache the token and refresh before expiration

            var payload = new
            {
                iss = _options.ClientId,
                exp = DateTimeOffset.UtcNow.AddSeconds(3600).ToUnixTimeSeconds()
            };

            var token = GenerateJWT(payload);
            return token;
        }

        private string GenerateJWT(object payload)
        {
            // For production, use proper JWT library like System.IdentityModel.Tokens.Jwt
            // This is a simplified version
            var header = Base64Encode(JsonSerializer.Serialize(new { alg = "HS256", typ = "JWT" }));
            var payloadStr = Base64Encode(JsonSerializer.Serialize(payload));
            var signature = ComputeHmacSha256($"{header}.{payloadStr}", _options.ClientSecret);

            return $"{header}.{payloadStr}.{signature}";
        }

        private string ComputeHmacSha256(string message, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        private ZoomMeetingResponse ParseZoomResponse(dynamic result)
        {
            return new ZoomMeetingResponse
            {
                MeetingId = result.GetProperty("id").GetString() ?? "",
                Topic = result.GetProperty("topic").GetString() ?? "",
                JoinUrl = result.GetProperty("join_url").GetString() ?? "",
                StartUrl = result.GetProperty("start_url").GetString() ?? "",
                StartTime = DateTime.Parse(result.GetProperty("start_time").GetString() ?? DateTime.UtcNow.ToString()),
                Duration = result.GetProperty("duration").GetInt32(),
                Status = "notstarted",
                UUID = result.GetProperty("uuid").GetString() ?? ""
            };
        }
    }

    /// <summary>
    /// Zoom configuration options
    /// </summary>
    public class ZoomOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string WebhookSecretToken { get; set; } = string.Empty;
        public string? ZoomUserId { get; set; } // Optional: specific user to host meetings
    }
}
