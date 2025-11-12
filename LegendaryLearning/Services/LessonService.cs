using Abstractions;
using LegendaryLearning.Abstractions;
using LegendaryLearning.Database;

namespace LegendaryLearning.Services
{
    /// <summary>
    /// Service for lesson management and retrieval.
    /// </summary>
    public class LessonService : ILessonService
    {
        private readonly LearningDatabase _database;

        public LessonService(LearningDatabase database)
        {
            _database = database;
        }

        public async Task<List<Lesson>> GetLessonsAsync(string courseId)
        {
            await Task.CompletedTask;
            return _database.GetLessons(courseId);
        }

        public async Task<Lesson?> GetLessonByIdAsync(string lessonId)
        {
            await Task.CompletedTask;
            return _database.GetLesson(lessonId);
        }

        public async Task<(bool success, string message)> UpdateLessonAsync(Lesson lesson)
        {
            await Task.CompletedTask;

            lesson.UpdatedAt = DateTime.UtcNow;
            _database.SaveLesson(lesson);

            Console.WriteLine($"[LessonService] Updated lesson: {lesson.Title}");
            return (true, "Lesson updated successfully");
        }

        public void AddLesson(Lesson lesson)
        {
            _database.SaveLesson(lesson);
        }

        public int GetLessonCount() => _database.GetLessonCount();

        public int GetTotalLessonCount(string courseId)
        {
            return _database.GetLessons(courseId).Count;
        }
    }
}
