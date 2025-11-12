using Microsoft.AspNetCore.Mvc;

namespace LegendaryCMS.API
{
    /// <summary>
    /// Robots.txt handler for SEO and privacy control
    /// </summary>
    public class RobotsTxtHandler
    {
        private readonly List<string> _disallowedPaths = new();
        private readonly bool _allowIndexing;

        public RobotsTxtHandler(bool allowIndexing = true)
        {
            _allowIndexing = allowIndexing;
            InitializeDefaultPaths();
        }

        private void InitializeDefaultPaths()
        {
            // Always disallow admin areas
            _disallowedPaths.Add("/cms/admin");
            _disallowedPaths.Add("/api/admin");

            // Disallow sensitive API endpoints
            _disallowedPaths.Add("/api/auth");
            _disallowedPaths.Add("/api/users");
        }

        /// <summary>
        /// Add a path to be disallowed in robots.txt
        /// </summary>
        public void DisallowPath(string path)
        {
            if (!_disallowedPaths.Contains(path))
            {
                _disallowedPaths.Add(path);
            }
        }

        /// <summary>
        /// Mark private categories as disallowed
        /// </summary>
        public void DisallowPrivateCategory(string categoryType, string categorySlug)
        {
            _disallowedPaths.Add($"/cms/{categoryType.ToLower()}/{categorySlug}");
        }

        /// <summary>
        /// Generate robots.txt content
        /// </summary>
        public string GenerateRobotsTxt()
        {
            var lines = new List<string>
            {
                "# Robots.txt for LegendaryCMS",
                "# Generated automatically based on privacy settings",
                "",
                "User-agent: *"
            };

            if (!_allowIndexing)
            {
                lines.Add("Disallow: /");
                lines.Add("");
                lines.Add("# Site indexing is disabled in settings");
            }
            else
            {
                foreach (var path in _disallowedPaths.OrderBy(p => p))
                {
                    lines.Add($"Disallow: {path}");
                }

                lines.Add("");
                lines.Add("# Public content is allowed");
                lines.Add("Allow: /cms/blogs");
                lines.Add("Allow: /cms/forums");
                lines.Add("Allow: /cms/downloads");
                lines.Add("Allow: /cms/profiles");
            }

            lines.Add("");
            lines.Add("# Sitemap");
            lines.Add("Sitemap: /sitemap.xml");

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Check if a path should be indexed by search engines
        /// </summary>
        public bool IsPathIndexable(string path)
        {
            if (!_allowIndexing)
            {
                return false;
            }

            return !_disallowedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// SEO Meta Tag generator
    /// </summary>
    public class SEOMetaGenerator
    {
        /// <summary>
        /// Generate meta tags for a page
        /// </summary>
        public static string GenerateMetaTags(
            string title,
            string description,
            string keywords,
            bool isPrivate = false,
            string? imageUrl = null,
            string? url = null)
        {
            var tags = new List<string>();

            // Basic meta tags
            tags.Add($"<title>{EscapeHtml(title)}</title>");
            tags.Add($"<meta name=\"description\" content=\"{EscapeHtml(description)}\" />");

            if (!string.IsNullOrWhiteSpace(keywords))
            {
                tags.Add($"<meta name=\"keywords\" content=\"{EscapeHtml(keywords)}\" />");
            }

            // Privacy control
            if (isPrivate)
            {
                tags.Add("<meta name=\"robots\" content=\"noindex, nofollow\" />");
            }
            else
            {
                tags.Add("<meta name=\"robots\" content=\"index, follow\" />");
            }

            // Open Graph tags
            tags.Add($"<meta property=\"og:title\" content=\"{EscapeHtml(title)}\" />");
            tags.Add($"<meta property=\"og:description\" content=\"{EscapeHtml(description)}\" />");
            tags.Add("<meta property=\"og:type\" content=\"website\" />");

            if (!string.IsNullOrWhiteSpace(url))
            {
                tags.Add($"<meta property=\"og:url\" content=\"{EscapeHtml(url)}\" />");
            }

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                tags.Add($"<meta property=\"og:image\" content=\"{EscapeHtml(imageUrl)}\" />");
            }

            // Twitter Card tags
            tags.Add("<meta name=\"twitter:card\" content=\"summary\" />");
            tags.Add($"<meta name=\"twitter:title\" content=\"{EscapeHtml(title)}\" />");
            tags.Add($"<meta name=\"twitter:description\" content=\"{EscapeHtml(description)}\" />");

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                tags.Add($"<meta name=\"twitter:image\" content=\"{EscapeHtml(imageUrl)}\" />");
            }

            return string.Join(Environment.NewLine + "    ", tags);
        }

        private static string EscapeHtml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}
