namespace AGP_CMS.Configuration
{
    /// <summary>
    /// CMS Configuration interface
    /// </summary>
    public interface ICMSConfiguration
    {
        /// <summary>
        /// Get Configuration value by key
        /// </summary>
        T? GetValue<T>(string key, T? defaultValue = default);

        /// <summary>
        /// Set Configuration value
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Get Configuration section
        /// </summary>
        Dictionary<string, object> GetSection(string section);

        /// <summary>
        /// Reload Configuration from source
        /// </summary>
        Task ReloadAsync();

        /// <summary>
        /// Save Configuration
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Get current environment (Development, Staging, Production)
        /// </summary>
        string Environment { get; }
    }

    /// <summary>
    /// CMS Configuration settings
    /// </summary>
    public class CMSConfiguration : ICMSConfiguration
    {
        private readonly Dictionary<string, object> _settings = new();
        private readonly string _configFilePath;
        private string _environment = "Development";

        public string Environment => _environment;

        public CMSConfiguration(string configFilePath)
        {
            _configFilePath = configFilePath;
            _environment = System.Environment.GetEnvironmentVariable("CMS_ENVIRONMENT") ?? "Development";
            LoadDefaults();
        }

        private void LoadDefaults()
        {
            // Database settings
            SetValue("Database:Type", "SQLite");
            SetValue("Database:ConnectionString", "Data Source=cms.db");
            SetValue("Database:AutoMigRate", true);

            // Site settings
            SetValue("Site:Name", "AGP CMS");
            SetValue("Site:BaseUrl", "http://localhost:8080");
            SetValue("Site:AdminEmail", "admin@legendarycms.local");

            // Security settings
            SetValue("Security:SessionTimeout", 3600);
            SetValue("Security:EnableCSRF", true);
            SetValue("Security:EnableXSSProtection", true);
            SetValue("Security:MaxLoginAttempts", 5);

            // API settings
            SetValue("API:Enabled", true);
            SetValue("API:RateLimit:RequestsPerMinute", 60);
            SetValue("API:RateLimit:RequestsPerHour", 1000);

            // Theme settings
            SetValue("Theme:Default", "classic");
            SetValue("Theme:AllowCustomThemes", true);

            // Localization settings
            SetValue("Localization:DefaultLocale", "en-US");
            SetValue("Localization:SupportedLocales", new[] { "en-US", "es-ES", "fr-FR", "de-DE" });

            // Performance settings
            SetValue("Performance:EnableCaching", true);
            SetValue("Performance:Cacheduration", 300);
            SetValue("Performance:MaxConcurrentRequests", 100);

            // Monitoring settings
            SetValue("Monitoring:EnableHealthChecks", true);
            SetValue("Monitoring:EnableMetrics", true);
            SetValue("Monitoring:LogLevel", "Information");
        }

        public T? GetValue<T>(string key, T? defaultValue = default)
        {
            if (_settings.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            if (value != null)
            {
                _settings[key] = value;
            }
        }

        public Dictionary<string, object> GetSection(string section)
        {
            var result = new Dictionary<string, object>();
            var prefix = section + ":";

            foreach (var kvp in _settings.Where(k => k.Key.StartsWith(prefix)))
            {
                var subKey = kvp.Key.Substring(prefix.Length);
                result[subKey] = kvp.Value;
            }

            return result;
        }

        public async Task ReloadAsync()
        {
            // Load defaults first
            LoadDefaults();

            // Try to load from file if it exists
            if (File.Exists(_configFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_configFilePath);
                    var doc = System.Text.Json.JsonDocument.Parse(json);

                    foreach (var property in doc.RootElement.EnumerateObject())
                    {
                        var key = property.Name;
                        var value = property.Value;

                        // Handle different value types
                        if (value.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var stringValue = value.GetString();
                            if (stringValue != null)
                            {
                                _settings[key] = stringValue;
                            }
                        }
                        else if (value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            _settings[key] = value.GetInt32();
                        }
                        else if (value.ValueKind == System.Text.Json.JsonValueKind.True ||
                                 value.ValueKind == System.Text.Json.JsonValueKind.False)
                        {
                            _settings[key] = value.GetBoolean();
                        }
                        else if (value.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var array = value.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
                            _settings[key] = array;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error - in production this should use ILogger
                    System.Diagnostics.Debug.WriteLine($"[CMSConfiguration] Error loading config from {_configFilePath}: {ex.Message}");
                }
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize settings to JSON
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null
                };

                var json = System.Text.Json.JsonSerializer.Serialize(_settings, options);
                await File.WriteAllTextAsync(_configFilePath, json);

                // Log success - in production this should use ILogger
                System.Diagnostics.Debug.WriteLine($"[CMSConfiguration] Settings saved to {_configFilePath}");
            }
            catch (Exception ex)
            {
                // Log error - in production this should use ILogger
                System.Diagnostics.Debug.WriteLine($"[CMSConfiguration] Error saving config to {_configFilePath}: {ex.Message}");
                throw;
            }
        }
    }
}
