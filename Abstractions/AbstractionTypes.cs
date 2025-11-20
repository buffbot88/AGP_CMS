// Abstractions for AGP_CMS modules
// Shared types used across AGP_CMS and LegendaryChat modules

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
