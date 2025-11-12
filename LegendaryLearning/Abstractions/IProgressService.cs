using Abstractions;

namespace LegendaryLearning.Abstractions
{
    /// <summary>
    /// Service interface for user progress management.
    /// </summary>
    public interface IProgressService
    {
        /// <summary>
        /// Mark a lesson as completed for a user.
        /// </summary>
        Task<bool> CompleteLessonAsync(string userId, string lessonId);

        /// <summary>
        /// Get user progress for a course.
        /// </summary>
        Task<CourseProgress?> GetUserProgressAsync(string userId, string courseId);

        /// <summary>
        /// Check if a user has completed all required SuperAdmin courses.
        /// </summary>
        Task<bool> HasCompletedSuperAdminCoursesAsync(string userId);

        /// <summary>
        /// Mark SuperAdmin courses as completed for a user (for first-time setup).
        /// </summary>
        Task<bool> MarkSuperAdminCoursesCompletedAsync(string userId);
    }
}
