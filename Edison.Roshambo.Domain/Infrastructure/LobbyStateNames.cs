namespace Edison.Roshambo.Domain.Infrastructure
{
    public static class LobbyStateNames
    {
        public const string AwaitingToPlayers = "Awaiting for players";
        public const string ReadyToStart = "Ready to start";
        public const string Playing = "Playing";
        public const string Blocked = "Blocked";
        public const string Summarizing = "Summarizing";
    }
}