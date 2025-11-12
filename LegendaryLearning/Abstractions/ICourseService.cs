using Abstractions;

namespace LegendaryLearning.Abstractions
{
    /// <summary>
    /// Service interface for course management and retrieval.
    /// </summary>
    public interface ICourseService
    {
        /// <summary>
        /// Get all available courses for a specific permission level.
        /// </summary>
        Task<List<Course>> GetCoursesAsync(string permissionLevel);

        /// <summary>
        /// Get a specific course by ID.
        /// </summary>
        Task<Course?> GetCourseByIdAsync(string courseId);

        /// <summary>
        /// Add or update a course (for real-time updates when new features are added).
        /// </summary>
        Task<(bool success, string message)> UpdateCourseAsync(Course course);
    }
}
