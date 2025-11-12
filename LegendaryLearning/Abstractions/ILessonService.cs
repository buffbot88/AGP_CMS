using Abstractions;

namespace LegendaryLearning.Abstractions
{
    /// <summary>
    /// Service interface for lesson management and retrieval.
    /// </summary>
    public interface ILessonService
    {
        /// <summary>
        /// Get lessons for a specific course.
        /// </summary>
        Task<List<Lesson>> GetLessonsAsync(string courseId);

        /// <summary>
        /// Get a specific lesson by ID.
        /// </summary>
        Task<Lesson?> GetLessonByIdAsync(string lessonId);

        /// <summary>
        /// Add or update a lesson.
        /// </summary>
        Task<(bool success, string message)> UpdateLessonAsync(Lesson lesson);
    }
}
