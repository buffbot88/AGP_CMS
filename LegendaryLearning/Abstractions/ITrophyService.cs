using Abstractions;

namespace LegendaryLearning.Abstractions
{
    /// <summary>
    /// Service interface for trophy management.
    /// </summary>
    public interface ITrophyService
    {
        /// <summary>
        /// Get all trophies for a user.
        /// </summary>
        Task<List<LearningTrophy>> GetUserTrophiesAsync(string userId);

        /// <summary>
        /// Award a trophy to a user.
        /// </summary>
        Task AwardTrophyAsync(string userId, LearningTrophy trophy);
    }
}
