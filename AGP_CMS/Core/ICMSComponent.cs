namespace AGP_CMS.Core
{
    /// <summary>
    /// Base interface for all CMS components (Forums, Blogs, Profiles, Chat, etc.)
    /// </summary>
    public interface ICMSComponent
    {
        /// <summary>
        /// Component name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initialize the component
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Start the component
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stop the component
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Get component health status
        /// </summary>
        Task<ComponentHealth> GetHealthAsync();

        /// <summary>
        /// Handle component-specific Operations
        /// </summary>
        Task<string> ProcessAsync(string Operation, Dictionary<string, object>? Parameters = null);
    }

    /// <summary>
    /// Component health status
    /// </summary>
    public class ComponentHealth
    {
        public string ComponentName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastCheck { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public List<string> Issues { get; set; } = new();
    }
}
