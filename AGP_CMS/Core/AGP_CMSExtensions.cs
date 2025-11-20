using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace AGP_CMS.Core
{
    /// <summary>
    /// Extension methods for registering and configuring AGP_CMS as a modular, plug-and-play component
    /// </summary>
    public static class AGP_CMSExtensions
    {
        /// <summary>
        /// Adds AGP_CMS services to the dependency injection container
        /// </summary>
        public static IServiceCollection AddAGP_CMS(this IServiceCollection services)
        {
            // Add Razor Pages support for embedded pages
            services.AddRazorPages()
                .AddApplicationPart(Assembly.GetExecutingAssembly())
                .AddRazorRuntimeCompilation(options =>
                {
                    // Allow runtime compilation of Razor views from the AGP_CMS assembly
                    options.FileProviders.Add(new EmbeddedFileProvider(
                        Assembly.GetExecutingAssembly(),
                        "AGP_CMS"
                    ));
                });

            // Register CMS services
            services.AddSingleton<IAGP_CMSModule, AGP_CMSModule>();

            return services;
        }

        /// <summary>
        /// Maps AGP_CMS routes and endpoints
        /// </summary>
        public static IApplicationBuilder UseAGP_CMS(this IApplicationBuilder app)
        {
            // Serve static files from AGP_CMS embedded wwwroot
            var assembly = Assembly.GetExecutingAssembly();
            var embeddedProvider = new EmbeddedFileProvider(assembly, "AGP_CMS.wwwroot");

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = embeddedProvider,
                RequestPath = "" // Serve at root, so /js/session-manager.js works
            });

            return app;
        }
    }
}
