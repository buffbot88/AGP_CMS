using System.Collections.Concurrent;
using Abstractions;
using LegendaryLearning.Abstractions;

namespace LegendaryLearning.Services
{
    /// <summary>
    /// Service for trophy management.
    /// </summary>
    public class TrophyService : ITrophyService
    {
        private readonly ConcurrentDictionary<string, List<LearningTrophy>> _userTrophies = new();
        private readonly string _moduleName;

        public TrophyService(string moduleName)
        {
            _moduleName = moduleName;
        }

        public async Task<List<LearningTrophy>> GetUserTrophiesAsync(string userId)
        {
            await Task.CompletedTask;

            return _userTrophies.TryGetValue(userId, out var trophies)
                ? new List<LearningTrophy>(trophies)
                : new List<LearningTrophy>();
        }

        public async Task AwardTrophyAsync(string userId, LearningTrophy trophy)
        {
            await Task.CompletedTask;

            if (!_userTrophies.ContainsKey(userId))
            {
                _userTrophies[userId] = new List<LearningTrophy>();
            }

            _userTrophies[userId].Add(trophy);
            Console.WriteLine($"[{_moduleName}] Awarded trophy to {userId}: {trophy.Title}");
        }
    }
}
