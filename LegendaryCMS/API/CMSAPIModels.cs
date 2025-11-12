namespace LegendaryCMS.API
{
    /// <summary>
    /// CMS API endpoint definition
    /// </summary>
    public class CMSAPIEndpoint
    {
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public string Description { get; set; } = string.Empty;
        public bool RequiresAuthentication { get; set; }
        public List<string> RequiredPermissions { get; set; } = new();
        public List<CMSAPIParameter> Parameters { get; set; } = new();
        public Func<CMSAPIRequest, Task<CMSAPIResponse>> Handler { get; set; } = null!;
    }

    /// <summary>
    /// API Parameter definition
    /// </summary>
    public class CMSAPIParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Description { get; set; } = string.Empty;
        public object? DefaultValue { get; set; }
    }

    /// <summary>
    /// API request
    /// </summary>
    public class CMSAPIRequest
    {
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        [Obsolete("Use BodyData for structured data")]
        public string? Body { get; set; }
        public Dictionary<string, string> BodyData { get; set; } = new();
        public Dictionary<string, string> QueryParameters { get; set; } = new();
        public string? UserId { get; set; }
        public List<string> UserPermissions { get; set; } = new();
    }

    /// <summary>
    /// API response
    /// </summary>
    public class CMSAPIResponse
    {
        public int StatusCode { get; set; } = 200;
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();

        public static CMSAPIResponse Success(object? data = null)
        {
            return new CMSAPIResponse { StatusCode = 200, Data = data };
        }

        public static CMSAPIResponse Error(string message, int statusCode = 400)
        {
            return new CMSAPIResponse { StatusCode = statusCode, ErrorMessage = message };
        }

        public static CMSAPIResponse Unauthorized(string message = "Unauthorized")
        {
            return new CMSAPIResponse { StatusCode = 401, ErrorMessage = message };
        }

        public static CMSAPIResponse Forbidden(string message = "Forbidden")
        {
            return new CMSAPIResponse { StatusCode = 403, ErrorMessage = message };
        }

        public static CMSAPIResponse NotFound(string message = "Not Found")
        {
            return new CMSAPIResponse { StatusCode = 404, ErrorMessage = message };
        }

        public static CMSAPIResponse BadRequest(string message = "Bad Request")
        {
            return new CMSAPIResponse { StatusCode = 400, ErrorMessage = message };
        }
    }

    /// <summary>
    /// Rate limit Configuration
    /// </summary>
    public class CMSRateLimitConfig
    {
        public int RequestsPerMinute { get; set; } = 60;
        public int RequestsPerHour { get; set; } = 1000;
        public int BurstSize { get; set; } = 10;
    }

    /// <summary>
    /// Rate limiter for API requests
    /// </summary>
    public class CMSRateLimiter
    {
        private readonly Dictionary<string, List<DateTime>> _requestHistory = new();
        private readonly CMSRateLimitConfig _config;
        private readonly object _lock = new();

        public CMSRateLimiter(CMSRateLimitConfig config)
        {
            _config = config;
        }

        public bool IsAllowed(string clientId)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;

                if (!_requestHistory.ContainsKey(clientId))
                {
                    _requestHistory[clientId] = new List<DateTime>();
                }

                var history = _requestHistory[clientId];

                // Remove old entries
                history.RemoveAll(t => (now - t).TotalHours >= 1);

                // Check hourly limit
                if (history.Count >= _config.RequestsPerHour)
                {
                    return false;
                }

                // Check per-minute limit
                var lastMinute = history.Count(t => (now - t).TotalMinutes < 1);
                if (lastMinute >= _config.RequestsPerMinute)
                {
                    return false;
                }

                history.Add(now);
                return true;
            }
        }

        public void ResetClient(string clientId)
        {
            lock (_lock)
            {
                _requestHistory.Remove(clientId);
            }
        }
    }
}
