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

                // Zoom docs: start_time nên là UTC ISO-8601 có 'Z'
                var startTimeUtc = request.StartTime.Kind == DateTimeKind.Utc
                    ? request.StartTime
                    : request.StartTime.ToUniversalTime();

                var meetingPayload = new
                {
                    topic = request.Topic,
                    type = 2, // Scheduled Meeting
                    start_time = startTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
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

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://api.zoom.us/v2/users/{userId}/meetings")
                {
                    Content = content
                };
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<dynamic>(responseContent, options);

                    _logger.LogInformation("Zoom meeting created successfully");
                    return ParseZoomResponse(result);
                }

                _logger.LogError("Zoom API error ({StatusCode}): {Body}", (int)response.StatusCode, responseContent);
                throw new Exception($"Zoom API error ({(int)response.StatusCode}): {responseContent}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Zoom meeting");
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

        private static readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);
        private static string? _cachedAccessToken;
        private static DateTimeOffset _cachedAccessTokenExpiresAt;

        private async Task<string> GetAccessTokenAsync()
        {
            // Zoom Server-to-Server OAuth: cần lấy access_token từ https://zoom.us/oauth/token
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken)
                && DateTimeOffset.UtcNow < _cachedAccessTokenExpiresAt.Subtract(TimeSpan.FromMinutes(2)))
            {
                return _cachedAccessToken;
            }

            await _tokenLock.WaitAsync();
            try
            {
                if (!string.IsNullOrWhiteSpace(_cachedAccessToken)
                    && DateTimeOffset.UtcNow < _cachedAccessTokenExpiresAt.Subtract(TimeSpan.FromMinutes(2)))
                {
                    return _cachedAccessToken;
                }

                if (string.IsNullOrWhiteSpace(_options.ClientId)
                    || string.IsNullOrWhiteSpace(_options.ClientSecret)
                    || string.IsNullOrWhiteSpace(_options.AccountId))
                {
                    throw new InvalidOperationException("Thiếu cấu hình Zoom (ClientId/ClientSecret/AccountId). Hãy cấu hình mục Zoom trong appsettings/Environment Variables.");
                }

                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

                using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://zoom.us/oauth/token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "account_credentials",
                        ["account_id"] = _options.AccountId
                    })
                };
                tokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);

                var tokenResponse = await _httpClient.SendAsync(tokenRequest);
                var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Zoom token error ({StatusCode}): {Body}", (int)tokenResponse.StatusCode, tokenBody);
                    throw new Exception($"Zoom token error ({(int)tokenResponse.StatusCode}): {tokenBody}");
                }

                using var doc = JsonDocument.Parse(tokenBody);
                var root = doc.RootElement;

                var accessToken = root.GetProperty("access_token").GetString();
                var expiresIn = root.TryGetProperty("expires_in", out var exp)
                    ? exp.GetInt32()
                    : 3600;

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new Exception("Zoom token response thiếu access_token.");
                }

                _cachedAccessToken = accessToken;
                _cachedAccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

                return accessToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private static string ComputeHmacSha256(string message, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message ?? string.Empty));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private ZoomMeetingResponse ParseZoomResponse(dynamic result)
        {
            return new ZoomMeetingResponse
            {
                MeetingId = GetStringValue(result, "id"),
                Topic = GetStringValue(result, "topic"),
                JoinUrl = GetStringValue(result, "join_url"),
                StartUrl = GetStringValue(result, "start_url"),
                StartTime = DateTime.Parse(GetStringValue(result, "start_time") ?? DateTime.UtcNow.ToString()),
                Duration = GetIntValue(result, "duration"),
                Status = "notstarted",
                UUID = GetStringValue(result, "uuid")
            };
        }

        private string GetStringValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return "";

            return prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString() ?? "",
                JsonValueKind.Number => prop.GetRawText(), // Convert number to string
                JsonValueKind.Null => "",
                _ => prop.GetRawText() ?? ""
            };
        }

        private int GetIntValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return 0;

            return prop.ValueKind switch
            {
                JsonValueKind.Number => prop.GetInt32(),
                JsonValueKind.String => int.TryParse(prop.GetString(), out var result) ? result : 0,
                _ => 0
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

        // Required for Zoom Server-to-Server OAuth
        public string AccountId { get; set; } = string.Empty;

        public string WebhookSecretToken { get; set; } = string.Empty;
        public string? ZoomUserId { get; set; } // Optional: specific user to host meetings
    }
}
