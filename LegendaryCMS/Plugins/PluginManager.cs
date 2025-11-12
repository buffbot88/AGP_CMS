using System.Reflection;

namespace LegendaryCMS.Plugins
{
    /// <summary>
    /// Plugin manager for loading, managing, and executing plugins
    /// </summary>
    public class PluginManager : IDisposable
    {
        private readonly Dictionary<Guid, ICMSPlugin> _loadedPlugins = new();
        private readonly Dictionary<string, List<Func<object, Task>>> _eventHandlers = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginLogger _logger;
        private bool _disposed;

        public IReadOnlyCollection<PluginMetadata> LoadedPlugins =>
            _loadedPlugins.Values.Select(p => new PluginMetadata
            {
                Id = p.Id,
                Name = p.Name,
                Version = p.Version,
                Author = p.Author,
                Description = p.Description,
                Dependencies = p.Dependencies,
                RequiredPermissions = p.RequiredPermissions,
                IsEnabled = true,
                LoadedAt = DateTime.UtcNow
            }).ToList();

        public PluginManager(IServiceProvider serviceProvider, IPluginLogger logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Load plugin from assembly
        /// </summary>
        public async Task<PluginLoadResult> LoadPluginAsync(string assemblyPath)
        {
            try
            {
                if (!File.Exists(assemblyPath))
                {
                    return new PluginLoadResult
                    {
                        Success = false,
                        ErrorMessage = $"Plugin assembly not found: {assemblyPath}"
                    };
                }

                var assembly = Assembly.LoadFrom(assemblyPath);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(ICMSPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var pluginType in pluginTypes)
                {
                    var plugin = Activator.CreateInstance(pluginType) as ICMSPlugin;
                    if (plugin == null) continue;

                    // Check if already loaded
                    if (_loadedPlugins.ContainsKey(plugin.Id))
                    {
                        _logger.LogWarning($"Plugin {plugin.Name} already loaded");
                        continue;
                    }

                    // Create plugin context
                    var context = new PluginContext(_serviceProvider, this, _logger);

                    // Initialize and start plugin
                    await plugin.InitializeAsync(context);
                    await plugin.StartAsync();

                    _loadedPlugins[plugin.Id] = plugin;

                    _logger.LogInfo($"Loaded plugin: {plugin.Name} v{plugin.Version} by {plugin.Author}");

                    return new PluginLoadResult
                    {
                        Success = true,
                        Metadata = new PluginMetadata
                        {
                            Id = plugin.Id,
                            Name = plugin.Name,
                            Version = plugin.Version,
                            Author = plugin.Author,
                            Description = plugin.Description,
                            Dependencies = plugin.Dependencies,
                            RequiredPermissions = plugin.RequiredPermissions,
                            IsEnabled = true,
                            LoadedAt = DateTime.UtcNow,
                            AssemblyPath = assemblyPath
                        }
                    };
                }

                return new PluginLoadResult
                {
                    Success = false,
                    ErrorMessage = "No valid plugin types found in assembly"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load plugin from {assemblyPath}", ex);
                return new PluginLoadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Unload plugin
        /// </summary>
        public async Task<bool> UnloadPluginAsync(Guid pluginId)
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                return false;
            }

            try
            {
                await plugin.StopAsync();
                plugin.Dispose();
                _loadedPlugins.Remove(pluginId);
                _logger.LogInfo($"Unloaded plugin: {plugin.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to unload plugin {plugin.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Register event handler
        /// </summary>
        public void RegisterEventHandler(string eventName, Func<object, Task> handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Func<object, Task>>();
            }
            _eventHandlers[eventName].Add(handler);
        }

        /// <summary>
        /// Unregister event handler
        /// </summary>
        public void UnregisterEventHandler(string eventName, Func<object, Task> handler)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Emit event to all registered handlers
        /// </summary>
        public async Task EmitEventAsync(string eventName, object data)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error executing event handler for {eventName}", ex);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var plugin in _loadedPlugins.Values)
            {
                try
                {
                    plugin.StopAsync().Wait();
                    plugin.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error disposing plugin {plugin.Name}", ex);
                }
            }

            _loadedPlugins.Clear();
            _eventHandlers.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Plugin context implementation
    /// </summary>
    internal class PluginContext : IPluginContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly PluginManager _pluginManager;

        public IPluginLogger Logger { get; }

        public PluginContext(IServiceProvider serviceProvider, PluginManager pluginManager, IPluginLogger logger)
        {
            _serviceProvider = serviceProvider;
            _pluginManager = pluginManager;
            Logger = logger;
        }

        public T? GetService<T>() where T : class
        {
            return _serviceProvider.GetService(typeof(T)) as T;
        }

        public void RegisterEventHandler(string eventName, Func<object, Task> handler)
        {
            _pluginManager.RegisterEventHandler(eventName, handler);
        }

        public void UnregisterEventHandler(string eventName, Func<object, Task> handler)
        {
            _pluginManager.UnregisterEventHandler(eventName, handler);
        }

        public Task EmitEventAsync(string eventName, object data)
        {
            return _pluginManager.EmitEventAsync(eventName, data);
        }

        public T? GetConfig<T>(string key, T? defaultValue = default)
        {
            // Would retrieve from Configuration service
            return defaultValue;
        }
    }

    /// <summary>
    /// Simple console logger implementation for plugins
    /// </summary>
    public class ConsolePluginLogger : IPluginLogger
    {
        private readonly string _pluginName;

        public ConsolePluginLogger(string pluginName)
        {
            _pluginName = pluginName;
        }

        public void LogInfo(string message)
        {
            Console.WriteLine($"[Plugin:{_pluginName}] INFO: {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[Plugin:{_pluginName}] WARN: {message}");
        }

        public void LogError(string message, Exception? exception = null)
        {
            Console.WriteLine($"[Plugin:{_pluginName}] ERROR: {message}");
            if (exception != null)
            {
                Console.WriteLine($"  Exception: {exception.Message}");
            }
        }

        public void LogDebug(string message)
        {
            Console.WriteLine($"[Plugin:{_pluginName}] DEBUG: {message}");
        }
    }
}
