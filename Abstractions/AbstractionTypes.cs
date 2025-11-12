// Abstractions for AGP_CMS modules
// Shared types used across LegendaryCMS, LegendaryChat, and LegendaryLearning modules

namespace Abstractions
{
    /// <summary>
    /// Base class for CMS modules
    /// </summary>
    public abstract class ModuleBase : IDisposable
    {
        public abstract string Name { get; }

        protected object? Manager { get; private set; }

        public virtual void Initialize(object? manager)
        {
            Manager = manager;
        }

        public abstract string Process(string input);

        protected void LogInfo(string message)
        {
            Console.WriteLine($"[{Name}] {message}");
        }

        protected void LogError(string message)
        {
            Console.Error.WriteLine($"[{Name}] ERROR: {message}");
        }

        protected void LogWarning(string message)
        {
            Console.WriteLine($"[{Name}] WARNING: {message}");
        }

        public virtual void Dispose()
        {
            // Override in derived classes if needed
        }
    }

    /// <summary>
    /// Attribute to mark CMS modules
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RaModuleAttribute : Attribute
    {
        public string Category { get; set; } = string.Empty;
    }

    #region Chat Module Types

    /// <summary>
    /// Chat Module interface
    /// </summary>
    public interface IChatModule
    {
        Task<List<ChatRoom>> GetRoomsAsync();
        Task<ChatRoom?> GetRoomByIdAsync(string roomId);
        Task<(bool success, string message, string? roomId)> CreateRoomAsync(string name, string createdBy, bool isPrivate = false);
        Task<List<ChatMessage>> GetMessagesAsync(string roomId, int limit = 50);
        Task<(bool success, string message, string? messageId)> SendMessageAsync(string roomId, string userId, string username, string content);
        Task<bool> DeleteMessageAsync(string messageId, string userId);
        Task<List<ChatUser>> GetActiveUsersAsync(string roomId);
        Task<bool> JoinRoomAsync(string roomId, string userId, string username);
        Task<bool> LeaveRoomAsync(string roomId, string userId);
    }

    /// <summary>
    /// Chat room model
    /// </summary>
    public class ChatRoom
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPrivate { get; set; }
        public int MessageCount { get; set; }
        public int ActiveUserCount { get; set; }
    }

    /// <summary>
    /// Chat message model
    /// </summary>
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RoomId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Chat user model
    /// </summary>
    public class ChatUser
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    }

    #endregion

    #region Learning Module Types

    /// <summary>
    /// Learning Module interface
    /// </summary>
    public interface ILearningModule
    {
        Task<List<Course>> GetCoursesAsync(string permissionLevel);
        Task<Course?> GetCourseByIdAsync(string courseId);
        Task<List<Lesson>> GetLessonsAsync(string courseId);
        Task<Lesson?> GetLessonByIdAsync(string lessonId);
        Task<bool> CompleteLessonAsync(string userId, string lessonId);
        Task<CourseProgress?> GetUserProgressAsync(string userId, string courseId);
        Task<List<LearningAchievement>> GetUserAchievementsAsync(string userId);
        Task<List<LearningTrophy>> GetUserTrophiesAsync(string userId);
        Task<(bool success, string message)> UpdateCourseAsync(Course course);
        Task<(bool success, string message)> UpdateLessonAsync(Lesson lesson);
        Task<bool> HasCompletedSuperAdminCoursesAsync(string userId);
        Task<bool> MarkSuperAdminCoursesCompletedAsync(string userId);
        Task<Assessment?> GetCourseAssessmentAsync(string courseId);
        Task<UserAssessmentResult> SubmitAssessmentAsync(string userId, string assessmentId, Dictionary<string, string> answers);
        Task<List<UserAssessmentResult>> GetUserAssessmentResultsAsync(string userId, string courseId);
        Task<bool> CanTakeAssessmentAsync(string userId, string courseId);
    }

    /// <summary>
    /// Course model
    /// </summary>
    public class Course
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PermissionLevel { get; set; } = "User";
        public string Category { get; set; } = string.Empty;
        public int LessonCount { get; set; }
        public int EstimatedMinutes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? PrerequisiteCourseId { get; set; }
    }

    /// <summary>
    /// Lesson model
    /// </summary>
    public class Lesson
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CourseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int EstimatedMinutes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public LessonType Type { get; set; } = LessonType.Reading;
        public string? VideoUrl { get; set; }
        public string? CodeExample { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Lesson type enum
    /// </summary>
    public enum LessonType
    {
        Reading,
        Video,
        Interactive,
        Quiz,
        Exercise,
        CodeExample
    }

    /// <summary>
    /// Course progress model
    /// </summary>
    public class CourseProgress
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public List<string> CompletedLessonIds { get; set; } = new();
        public int ProgressPercentage { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Learning achievement model
    /// </summary>
    public class LearningAchievement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public LearningAchievementType Type { get; set; } = LearningAchievementType.CourseCompletion;
        public int Points { get; set; }
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Learning achievement type enum
    /// </summary>
    public enum LearningAchievementType
    {
        CourseCompletion,
        LessonStreak,
        PerfectScore,
        FastLearner,
        Milestone,
        FirstLesson
    }

    /// <summary>
    /// Learning trophy model
    /// </summary>
    public class LearningTrophy
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public LearningTrophyTier Tier { get; set; } = LearningTrophyTier.Bronze;
        public string? RelatedCourseId { get; set; }
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Learning trophy tier enum
    /// </summary>
    public enum LearningTrophyTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond
    }

    /// <summary>
    /// Assessment model
    /// </summary>
    public class Assessment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CourseId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PassingScore { get; set; } = 70;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public List<Question> Questions { get; set; } = new();
    }

    /// <summary>
    /// Question model
    /// </summary>
    public class Question
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AssessmentId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
        public int OrderIndex { get; set; }
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public int Points { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Answer model
    /// </summary>
    public class Answer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string QuestionId { get; set; } = string.Empty;
        public string AnswerText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int OrderIndex { get; set; }
    }

    /// <summary>
    /// Question type enum
    /// </summary>
    public enum QuestionType
    {
        MultipleChoice,
        TrueFalse,
        ShortAnswer,
        Essay
    }

    /// <summary>
    /// User assessment result model
    /// </summary>
    public class UserAssessmentResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string AssessmentId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool Passed { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
        public List<string> FailedLessonIds { get; set; } = new();
        public Dictionary<string, string> Answers { get; set; } = new();
        public Dictionary<string, string> UserASHATAnswers { get; set; } = new();
    }

    #endregion

    #region AI Language Module Types

    /// <summary>
    /// AI Language Module interface
    /// </summary>
    public interface IAILanguageModule
    {
        Task<AIResponse> GenerateAsync(string intent, string context, string language, Dictionary<string, object> metadata);
    }

    /// <summary>
    /// AI Response model
    /// </summary>
    public class AIResponse
    {
        public string Text { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion

    #region RaCoin Module Types

    /// <summary>
    /// RaCoin Module interface
    /// </summary>
    public interface IRaCoinModule
    {
        Task<bool> AwardCoinsAsync(string userId, int amount, string reason);
        Task<RaCoinResult> TopUpAsync(Guid userId, decimal amount, string reason);
        Task<int> GetBalanceAsync(string userId);
    }

    /// <summary>
    /// RaCoin operation result
    /// </summary>
    public class RaCoinResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion

    #region Content Moderation Types

    /// <summary>
    /// Content moderation module interface
    /// </summary>
    public interface IContentmoderationModule
    {
        Task<ModerationResult> ScanTextAsync(string content, string userId, string moduleName, string contextId);
    }

    /// <summary>
    /// Moderation result model
    /// </summary>
    public class ModerationResult
    {
        public moderationAction Action { get; set; } = moderationAction.Allowed;
        public List<ModerationViolation> Violations { get; set; } = new();
    }

    /// <summary>
    /// Moderation violation model
    /// </summary>
    public class ModerationViolation
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Severity { get; set; }
    }

    /// <summary>
    /// Moderation action enum
    /// </summary>
    public enum moderationAction
    {
        Allowed,
        Blocked,
        RequiresReview
    }

    #endregion

    #region Common Types

    /// <summary>
    /// User profile model
    /// </summary>
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActiveAt { get; set; }
        public string Role { get; set; } = "User";
        public int PostCount { get; set; }
        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
    }

    /// <summary>
    /// User activity model
    /// </summary>
    public class Activity
    {
        public string ActivityId { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Social post model for user profiles
    /// </summary>
    public class SocialPost
    {
        public string PostId { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public List<string> Likes { get; set; } = new();
        public List<string> Comments { get; set; } = new();
    }

    #endregion
}
