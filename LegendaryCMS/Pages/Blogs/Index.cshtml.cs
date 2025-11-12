using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;

namespace LegendaryCMS.Pages.Blogs
{
    /// <summary>
    /// Razor Page Model for Blogs Index - replaces legacy PHP blog templates
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly DatabaseService _db;

        public List<BlogPost> RecentPosts { get; set; } = new();

        public IndexModel(DatabaseService db)
        {
            _db = db;
        }

        public void OnGet()
        {
            // Load recent blog posts from database
            RecentPosts = LoadRecentBlogPosts();
        }

        private List<BlogPost> LoadRecentBlogPosts()
        {
            var posts = new List<BlogPost>();

            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT b.Id, b.Title, b.Excerpt, b.Content, b.CreatedAt, b.ViewCount, 
                       u.Username, 
                       (SELECT COUNT(*) FROM BlogComments WHERE PostId = b.Id) as CommentCount
                FROM BlogPosts b
                JOIN Users u ON b.AuthorId = u.Id
                WHERE b.Published = 1
                ORDER BY b.CreatedAt DESC
                LIMIT 10
            ";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var content = reader.GetString(3);
                    var excerpt = reader.IsDBNull(2)
                        ? (content.Length > 200 ? content.Substring(0, 200) + "..." : content)
                        : reader.GetString(2);

                    posts.Add(new BlogPost
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Excerpt = excerpt,
                        Content = content,
                        PublishedDate = DateTime.Parse(reader.GetString(4)),
                        ViewCount = reader.GetInt32(5),
                        Author = reader.GetString(6),
                        CommentCount = reader.GetInt32(7)
                    });
                }
            }
            catch
            {
                // Return empty list if there's an error
            }

            return posts;
        }
    }

    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
    }
}
