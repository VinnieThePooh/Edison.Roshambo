using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Edison.Roshambo.Web.Infrastructure
{
    public static class LobbyStateMapper
    {
        public static IDictionary<int, string> Map { get; } = new Dictionary<int, string>();

        static LobbyStateMapper()
        {
            Map.Add(new KeyValuePair<int, string>(1, "Awaiting for players"));
            Map.Add(new KeyValuePair<int, string>(2, "Ready to start"));
            Map.Add(new KeyValuePair<int, string>(3, "Playing"));
            Map.Add(new KeyValuePair<int, string>(4, "Summarizing"));
            Map.Add(new KeyValuePair<int, string>(5, "Blocked"));
        }
    }
}                                  