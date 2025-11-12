using Abstractions;
using LegendaryLearning.Abstractions;
using LegendaryLearning.Database;

namespace LegendaryLearning.Services
{
    /// <summary>
    /// Service for course management and retrieval.
    /// </summary>
    public class CourseService : ICourseService
    {
        private readonly LearningDatabase _database;

        public CourseService(LearningDatabase database)
        {
            _database = database;
        }

        public async Task<List<Course>> GetCoursesAsync(string permissionLevel)
        {
            await Task.CompletedTask;
            return _database.GetCourses(permissionLevel);
        }

        public async Task<Course?> GetCourseByIdAsync(string courseId)
        {
            await Task.CompletedTask;
            return _database.GetCourse(courseId);
        }

        public async Task<(bool success, string message)> UpdateCourseAsync(Course course)
        {
            await Task.CompletedTask;

            course.UpdatedAt = DateTime.UtcNow;
            _database.SaveCourse(course);

            Console.WriteLine($"[CourseService] Updated course: {course.Title}");
            return (true, "Course updated successfully");
        }

        public void AddCourse(Course course)
        {
            _database.SaveCourse(course);
        }

        public int GetCourseCount() => _database.GetCourseCount();
    }
}
