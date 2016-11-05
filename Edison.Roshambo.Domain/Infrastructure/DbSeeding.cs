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


        // don't change order of shapes here!
        private static void SeedGameShapes(RoshamboContext context)
        {
            context.Set<GameShape>().AddOrUpdate(
              new[]{
                   new GameShape() { ShapeName = ShapeNames.Rock},
                    new GameShape() { ShapeName = ShapeNames.Paper},
                    new GameShape() { ShapeName = ShapeNames.Scissors},
                     new GameShape() { ShapeName = ShapeNames.Lizard},
                     new GameShape() { ShapeName = ShapeNames.Spock},
                     new GameShape() { ShapeName = ShapeNames.Undefined}
              });

        }
    }
}