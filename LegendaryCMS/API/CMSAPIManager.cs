namespace LegendaryCMS.API
{
    /// <summary>
    /// CMS API Manager
    /// </summary>
    public class CMSAPIManager
    {
        private readonly Dictionary<string, CMSAPIEndpoint> _endpoints = new();
        private readonly CMSRateLimiter _RateLimiter;
        private readonly object _lock = new();

        public CMSAPIManager(CMSRateLimitConfig RateLimitConfig)
        {
            _RateLimiter = new CMSRateLimiter(RateLimitConfig);
            RegisterDefaultEndpoints();
        }

        private void RegisterDefaultEndpoints()
        {
            // Health check endpoint
            RegisterEndpoint(new CMSAPIEndpoint
            {
                Path = "/api/health",
                Method = "GET",
                Description = "Health check endpoint",
                RequiresAuthentication = false,
                Handler = async (request) =>
                {
                    await Task.CompletedTask;
                    return CMSAPIResponse.Success(new
                    {
                        status = "healthy",
                        timestamp = DateTime.UtcNow,
                        version = "8.0.0"
                    });
                }
            });

            // Version endpoint
            RegisterEndpoint(new CMSAPIEndpoint
            {
                Path = "/api/version",
                Method = "GET",
                Description = "Get CMS version information",
                RequiresAuthentication = false,
                Handler = async (request) =>
                {
                    await Task.CompletedTask;
                    return CMSAPIResponse.Success(new
                    {
                        version = "8.0.0",
                        name = "Legendary CMS Suite",
                        build = DateTime.UtcNow.ToString("yyyyMMdd")
                    });
                }
            });

            // List endpoints
            RegisterEndpoint(new CMSAPIEndpoint
            {
                Path = "/api/endpoints",
                Method = "GET",
                Description = "List all available API endpoints",
                RequiresAuthentication = false,
                Handler = async (request) =>
                {
                    await Task.CompletedTask;
                    var endpoints = _endpoints.Values.Select(e => new
                    {
                        e.Path,
                        e.Method,
                        e.Description,
                        e.RequiresAuthentication,
                        e.RequiredPermissions
                    });
                    return CMSAPIResponse.Success(endpoints);
                }
            });
        }

        /// <summary>
        /// Register a new API endpoint
        /// </summary>
        public void RegisterEndpoint(CMSAPIEndpoint endpoint)
        {
            lock (_lock)
            {
                var key = $"{endpoint.Method}:{endpoint.Path}";
                _endpoints[key] = endpoint;
            }
        }

        /// <summary>
        /// Process an API request
        /// </summary>
        public async Task<CMSAPIResponse> ProcessRequestAsync(CMSAPIRequest request)
        {
            // Check Rate limiting
            var clientId = request.UserId ?? request.Headers.GetValueOrDefault("X-Forwarded-For", "unknown");
            if (!_RateLimiter.IsAllowed(clientId))
            {
                return new CMSAPIResponse
                {
                    StatusCode = 429,
                    ErrorMessage = "Rate limit exceeded"
                };
            }

            // Find endpoint
            var key = $"{request.Method}:{request.Path}";
            if (!_endpoints.TryGetValue(key, out var endpoint))
            {
                return CMSAPIResponse.NotFound($"Endpoint not found: {request.Method} {request.Path}");
            }

            // Check authentication
            if (endpoint.RequiresAuthentication && string.IsNullOrEmpty(request.UserId))
            {
                return CMSAPIResponse.Unauthorized("Authentication required");
            }

            // Check permissions
            if (endpoint.RequiredPermissions.Count > 0)
            {
                var hasPermission = endpoint.RequiredPermissions.Any(p => request.UserPermissions.Contains(p));
                if (!hasPermission)
                {
                    return CMSAPIResponse.Forbidden("Insufficient permissions");
                }
            }

            // Execute handler
            try
            {
                return await endpoint.Handler(request);
            }
            catch (Exception ex)
            {
                return CMSAPIResponse.Error($"Internal server error: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get all registered endpoints
        /// </summary>
        public List<CMSAPIEndpoint> GetEndpoints()
        {
            lock (_lock)
            {
                return _endpoints.Values.ToList();
            }
        }

        /// <summary>
        /// Generate OpenAPI/Swagger documentation
        /// </summary>
        public string GenerateOpenAPISpec()
        {
            var spec = new
            {
                openapi = "3.0.0",
                info = new
                {
                    title = "Legendary CMS API",
                    version = "8.0.0",
                    description = "Production-ready CMS API for ASHAT OS"
                },
                servers = new[]
                {
                    new { url = "http://localhost:8080", description = "Development server" }
                },
                paths = _endpoints.Values.Select(e => new
                {
                    path = e.Path,
                    method = e.Method.ToLower(),
                    summary = e.Description,
                    security = e.RequiresAuthentication ? new[] { new { bearerauth = Array.Empty<string>() } } : null,
                    Parameters = e.Parameters.Select(p => new
                    {
                        name = p.Name,
                        @in = "query",
                        required = p.Required,
                        description = p.Description,
                        schema = new { type = p.Type }
                    }).ToArray()
                }).ToDictionary(x => x.path, x => x)
            };

            return System.Text.Json.JsonSerializer.Serialize(spec, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
