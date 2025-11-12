namespace LegendaryCMS.Plugins
{
    /// <summary>
    /// Base interface for all CMS plugins
    /// </summary>
    public interface ICMSPlugin
    {
        /// <summary>
        /// Plugin unique identifier
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Plugin name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Plugin version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Plugin author
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Plugin description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Plugin dependencies
        /// </summary>
        List<string> Dependencies { get; }

        /// <summary>
        /// Required permissions
        /// </summary>
        List<string> RequiredPermissions { get; }

        /// <summary>
        /// Initialize the plugin
        /// </summary>
        Task InitializeAsync(IPluginContext context);

        /// <summary>
        /// Start the plugin
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stop the plugin
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Dispose the plugin
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Plugin context providing access to CMS services
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// Get CMS service
        /// </summary>
        T? GetService<T>() where T : class;

        /// <summary>
        /// Register event handler
        /// </summary>
        void RegisterEventHandler(string eventName, Func<object, Task> handler);

        /// <summary>
        /// Unregister event handler
        /// </summary>
        void UnregisterEventHandler(string eventName, Func<object, Task> handler);

        /// <summary>
        /// Emit event
        /// </summary>
        Task EmitEventAsync(string eventName, object data);

        /// <summary>
        /// Get Configuration value
        /// </summary>
        T? GetConfig<T>(string key, T? defaultValue = default);

        /// <summary>
        /// Logger for plugin
        /// </summary>
        IPluginLogger Logger { get; }
    }

    /// <summary>
    /// Plugin logger interface
    /// </summary>
    public interface IPluginLogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? exception = null);
        void LogDebug(string message);
    }

    /// <summary>
    /// Plugin metadata
    /// </summary>
    public class PluginMetadata
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
        public List<string> RequiredPermissions { get; set; } = new();
        public bool IsEnabled { get; set; }
        public DateTime LoadedAt { get; set; }
        public string AssemblyPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Plugin load result
    /// </summary>
    public class PluginLoadResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public PluginMetadata? Metadata { get; set; }
    }
}
