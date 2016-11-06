namespace Edison.Roshambo.Web.Infrastructure
{
    public static class GameWorkflowMessages
    {
        public const string NextRoundWillStartIn = "Round * will start in: = seconds";
        public const string YourBlockingExpiresIn = "Your blocking expires in * seconds";
        public const string DrawnRound = "Drawn. Nobody won";
        public const string RoundGotAWinner = "*  won the round.";
        public const string GameGotAWinner = "* won the game.";
        public const string TipUsingFailed = "Tip using failed. Your opponent has not picked shape yet.";
    }
}