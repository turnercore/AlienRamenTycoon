using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server
{
    public interface IRpsMatchService
    {
        event Action<RpsMatchReadyPayload> MatchReady;
        event Action<RpsRoundResultPayload> RoundResolved;

        string LocalUserId { get; }
        Task MatchmakeAsync();
        Task SendPickAsync(string action);
        string MatchId { get; }
    }

    [Serializable]
    public class RpsMatchReadyPayload
    {
        public string matchId;
        public string localUserId;
        public string opponentUserId;
        public string opponentUsername;
        public int round;
        public Dictionary<string, int> scores;
    }

    [Serializable]
    public class RpsMatchReadyServerPayload
    {
        public string type;
        public string matchId;
        public Dictionary<string, RpsMatchReadyPlayer> players;
        public Dictionary<string, int> scores;
        public int round;
    }

    [Serializable]
    public class RpsMatchReadyPlayer
    {
        public string username;
    }

    [Serializable]
    public class RpsRoundResultPayload
    {
        public int round;
        public Dictionary<string, string> picks;
        public string winnerUserId;
        public Dictionary<string, int> scores;
        public int nextRound;
    }
}
