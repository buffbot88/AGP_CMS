using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Blogs
{
    /// <summary>
    /// Razor Page Model for Blog Post Detail - WordPress-like blog post display
    /// </summary>
    public class PostModel : PageModel
    {
        private readonly DatabaseService _db;

        public BlogPost? Post { get; set; }
        public List<BlogPost> RecentPosts { get; set; } = new();

        public PostModel(DatabaseService db)
        {
            _db = db;
        }

        public void OnGet(int id)
        {
            // Load the specific blog post by ID
            Post = LoadBlogPost(id);

            // Load recent posts for sidebar
            RecentPosts = LoadRecentBlogPosts();

            // Increment view count
            if (Post != null)
            {
                IncrementViewCount(id);
            }
        }

        private BlogPost? LoadBlogPost(int id)
        {
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
                WHERE b.Id = @id AND b.Published = 1
            ";
                command.Parameters.AddWithValue("@id", id);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var content = reader.GetString(3);
                    var excerpt = reader.IsDBNull(2)
                        ? (content.Length > 200 ? content.Substring(0, 200) + "..." : content)
                        : reader.GetString(2);

                    return new BlogPost
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Excerpt = excerpt,
                        Content = content,
                        PublishedDate = DateTime.Parse(reader.GetString(4)),
                        ViewCount = reader.GetInt32(5),
                        Author = reader.GetString(6),
                        CommentCount = reader.GetInt32(7)
                    };
                }
            }
            catch
            {
                // Return null if error
            }

            return null;
        }

        private List<BlogPost> LoadRecentBlogPosts()
        {
            var posts = new List<BlogPost>();

            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT b.Id, b.Title, b.Excerpt, b.Content, b.CreatedAt, u.Username
                FROM BlogPosts b
                JOIN Users u ON b.AuthorId = u.Id
                WHERE b.Published = 1
                ORDER BY b.CreatedAt DESC
                LIMIT 5
            ";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var content = reader.GetString(3);
                    var excerpt = reader.IsDBNull(2)
                        ? (content.Length > 100 ? content.Substring(0, 100) + "..." : content)
                        : reader.GetString(2);

                    posts.Add(new BlogPost
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Excerpt = excerpt,
                        PublishedDate = DateTime.Parse(reader.GetString(4)),
                        Author = reader.GetString(5)
                    });
                }
            }
            catch
            {
                // Return empty list if error
            }

            return posts;
        }

        private void IncrementViewCount(int id)
        {
            try
            {
                using var connection = _db.GetConnection();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE BlogPosts SET ViewCount = ViewCount + 1 WHERE Id = @id";
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
