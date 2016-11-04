using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Edison.Roshambo.Web.Infrastructure
{
    public static class GameWorkflowMessages
    {
        public const string NextRoundWillStartIn = "Round * will start in: = seconds";
        public const string YourBlockingExpiresIn = "Your blocking expires in * seconds";
        public const string DrawnRound = "Drawn. Nobody won";
        public const string RoundGotAWinner = "*  won the round.";
    }
}