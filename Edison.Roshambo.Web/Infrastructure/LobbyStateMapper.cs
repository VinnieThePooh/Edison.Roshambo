using System.Collections.Generic;
using Edison.Roshambo.Domain.Infrastructure;

namespace Edison.Roshambo.Web.Infrastructure
{
    public static class LobbyStateMapper
    {
        public static IDictionary<int, string> Map { get; } = new Dictionary<int, string>();

        //  ids are bounded to DbSeeding class generation logic
        static LobbyStateMapper()
        {
            Map.Add(new KeyValuePair<int, string>(1, LobbyStateNames.AwaitingToPlayers));
            Map.Add(new KeyValuePair<int, string>(2, LobbyStateNames.ReadyToStart));
            Map.Add(new KeyValuePair<int, string>(3, LobbyStateNames.Playing));
            Map.Add(new KeyValuePair<int, string>(4, LobbyStateNames.Summarizing));
            Map.Add(new KeyValuePair<int, string>(5, LobbyStateNames.Blocked));
        }
    }
}                                  