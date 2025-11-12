using Abstractions;
using LegendaryLearning.Abstractions;
using LegendaryLearning.Database;

namespace LegendaryLearning.Services
{
    /// <summary>
    /// Service for managing assessments and user assessment results.
    /// </summary>
    public class AssessmentService
    {
        private readonly LearningDatabase _database;
        private readonly ILessonService _lessonService;
        private readonly string _moduleName;

        public AssessmentService(LearningDatabase database, ILessonService lessonService, string moduleName)
        {
            _database = database;
            _lessonService = lessonService;
            _moduleName = moduleName;
        }

        public async Task<Assessment?> GetCourseAssessmentAsync(string courseId)
        {
            await Task.CompletedTask;
            return _database.GetAssessmentByCourseId(courseId);
        }

        public async Task<UserAssessmentResult> SubmitAssessmentAsync(string userId, string assessmentId, Dictionary<string, string> userASHATAnswers)
        {
            await Task.CompletedTask;

            var assessment = _database.GetAssessment(assessmentId);
            if (assessment == null)
            {
                throw new InvalidOperationException($"Assessment {assessmentId} not found");
            }

            var questions = _database.GetQuestions(assessmentId);
            var totalPoints = questions.Sum(q => q.Points);
            var earnedPoints = 0;
            var failedLessonIds = new List<string>();

            // Group questions by lesson for adaptive retake tracking
            var questionsByLesson = questions.GroupBy(q => q.LessonId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var question in questions)
            {
                if (userASHATAnswers.TryGetValue(question.Id, out var userASHATAnswerId))
                {
                    var answers = _database.GetAnswers(question.Id);
                    var correctAnswer = answers.FirstOrDefault(a => a.IsCorrect);

                    if (correctAnswer != null && correctAnswer.Id == userASHATAnswerId)
                    {
                        earnedPoints += question.Points;
                    }
                    else
                    {
                        // User got this question wrong, mark lesson for retake
                        if (!failedLessonIds.Contains(question.LessonId))
                        {
                            failedLessonIds.Add(question.LessonId);
                        }
                    }
                }
                else
                {
                    // No answer provided, mark lesson for retake
                    if (!failedLessonIds.Contains(question.LessonId))
                    {
                        failedLessonIds.Add(question.LessonId);
                    }
                }
            }

            var score = totalPoints > 0 ? (int)((double)earnedPoints / totalPoints * 100) : 0;
            var passed = score >= assessment.PassingScore;

            var result = new UserAssessmentResult
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                AssessmentId = assessmentId,
                Score = score,
                Passed = passed,
                AttemptedAt = DateTime.UtcNow,
                FailedLessonIds = passed ? new List<string>() : failedLessonIds,
                UserASHATAnswers = userASHATAnswers
            };

            _database.SaveUserAssessmentResult(result);

            Console.WriteLine($"[{_moduleName}] User {userId} scored {score}% on assessment {assessmentId} (Passed: {passed})");
            if (!passed && failedLessonIds.Count > 0)
            {
                Console.WriteLine($"[{_moduleName}] User needs to restudy {failedLessonIds.Count} lessons");
            }

            return result;
        }

        public async Task<List<UserAssessmentResult>> GetUserAssessmentResultsAsync(string userId, string courseId)
        {
            await Task.CompletedTask;

            var assessment = _database.GetAssessmentByCourseId(courseId);
            if (assessment == null)
            {
                return new List<UserAssessmentResult>();
            }

            return _database.GetUserAssessmentResults(userId, assessment.Id);
        }

        public async Task<bool> CanTakeAssessmentAsync(string userId, string courseId)
        {
            await Task.CompletedTask;

            // Check if all lessons in the course are completed
            var progress = _database.GetCourseProgress(userId, courseId);
            if (progress == null)
            {
                return false;
            }

            var lessons = _database.GetLessons(courseId);
            var allLessonIds = lessons.Select(l => l.Id).ToList();

            // Check if all lessons are completed
            return allLessonIds.All(lessonId => progress.CompletedLessonIds.Contains(lessonId));
        }

        public void CreateAssessment(Assessment assessment, List<(Question question, List<Answer> answers)> questionsWithAnswers)
        {
            _database.SaveAssessment(assessment);

            foreach (var (question, answers) in questionsWithAnswers)
            {
                _database.SaveQuestion(question);
                foreach (var answer in answers)
                {
                    _database.SaveAnswer(answer);
                }
            }

            Console.WriteLine($"[{_moduleName}] Created assessment {assessment.Id} with {questionsWithAnswers.Count} questions");
        }
    }
}
