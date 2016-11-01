using System.Data.Entity.Migrations;
using Edison.Roshambo.Domain.DataAccess;
using Edison.Roshambo.Domain.Models;

namespace Edison.Roshambo.Domain.Infrastructure
{
    public static class DbSeeding
    {
        public static void SeedContext(RoshamboContext context)
        {
            SeedLobbyStates(context);
            SeedGameShapes(context);
        }


        private static void SeedLobbyStates(RoshamboContext context)
        {
            context.Set<LobbyState>().AddOrUpdate(
                new[]
                {
                   new LobbyState() { Name = LobbyStateNames.AwaitingToPlayers},
                   new LobbyState() { Name = LobbyStateNames.ReadyToStart},
                   new LobbyState() { Name = LobbyStateNames.Playing},
                   new LobbyState() { Name = LobbyStateNames.Summarizing},
                   new LobbyState() { Name = LobbyStateNames.Blocked}
                });
        }



        private static void SeedGameShapes(RoshamboContext context)
        {
            context.Set<GameShape>().AddOrUpdate(
              new[]
              {
                   new GameShape() { ShapeName = "Rock"},
                    new GameShape() { ShapeName = "Paper"},
                    new GameShape() { ShapeName = "Scissors"},
                     new GameShape() { ShapeName = "Lizard"},
                     new GameShape() { ShapeName = "Spock"}
              });

        }
    }
}