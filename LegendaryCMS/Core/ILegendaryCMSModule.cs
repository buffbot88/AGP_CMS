using LegendaryCMS.Configuration;

namespace LegendaryCMS.Core
{
    /// <summary>
    /// Main interface for the Legendary CMS Suite module
    /// </summary>
    public interface ILegendaryCMSModule : IDisposable
    {
        /// <summary>
        /// Module name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Module version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Initialize the CMS module
        /// </summary>
        void Initialize(object? manager);

        /// <summary>
        /// Process CMS commands
        /// </summary>
        string Process(string input);

        /// <summary>
        /// Get CMS status
        /// </summary>
        CMSStatus GetStatus();

        /// <summary>
        /// Get CMS Configuration
        /// </summary>
        ICMSConfiguration GetConfiguration();
    }

    /// <summary>
    /// CMS status information
    /// </summary>
    public class CMSStatus
    {
        public bool IsInitialized { get; set; }
        public bool IsRunning { get; set; }
        public string Version { get; set; } = "8.0.0";
        public DateTime StartTime { get; set; }
        public Dictionary<string, bool> ComponentStatus { get; set; } = new();
        public Dictionary<string, string> HealthChecks { get; set; } = new();
    }
}
