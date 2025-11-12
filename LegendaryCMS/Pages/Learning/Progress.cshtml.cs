using LegendaryCMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LegendaryCMS.Pages.Learning
{
    public class ProgressModel : PageModel
    {
        private readonly DatabaseService _db;

        public UserInfo? CurrentUser { get; set; }
        public bool IsAuthenticated { get; set; }
        public List<CourseProgress> CourseProgress { get; set; } = new();
        public LearningStatistics Statistics { get; set; } = new();

        public ProgressModel(DatabaseService db)
        {
            _db = db;
        }

        public IActionResult OnGet()
        {
            // Check if user is authenticated
            var userId = _db.GetAuthenticatedUserId(HttpContext);
            IsAuthenticated = userId.HasValue;

            if (userId.HasValue)
            {
                CurrentUser = _db.GetUserById(userId.Value);
                LoadProgress(userId.Value);
                LoadStatistics(userId.Value);
            }

            return Page();
        }

        private void LoadProgress(int userId)
        {
            // Sample course progress data
            // In a real implementation, this would be loaded from the database
            CourseProgress = new List<CourseProgress>
            {
                new CourseProgress
                {
                    CourseId = 1,
                    CourseName = "Introduction to ASHATOS",
                    TotalLessons = 10,
                    CompletedLessons = 0,
                    PercentComplete = 0,
                    LastAccessed = null
                },
                new CourseProgress
                {
                    CourseId = 2,
                    CourseName = "Advanced Features",
                    TotalLessons = 15,
                    CompletedLessons = 0,
                    PercentComplete = 0,
                    LastAccessed = null
                }
            };
        }

        private void LoadStatistics(int userId)
        {
            // Sample statistics
            // In a real implementation, this would be calculated from the database
            Statistics = new LearningStatistics
            {
                TotalCoursesEnrolled = 0,
                TotalCoursesCompleted = 0,
                TotalLessonsCompleted = 0,
                TotalStudyTime = 0,
                AverageQuizScore = 0,
                RaCoinsEarned = 0
            };
        }
    }

    public class CourseProgress
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public int PercentComplete { get; set; }
        public DateTime? LastAccessed { get; set; }
    }

    public class LearningStatistics
    {
        public int TotalCoursesEnrolled { get; set; }
        public int TotalCoursesCompleted { get; set; }
        public int TotalLessonsCompleted { get; set; }
        public int TotalStudyTime { get; set; }
        public double AverageQuizScore { get; set; }
        public int RaCoinsEarned { get; set; }
    }
}
