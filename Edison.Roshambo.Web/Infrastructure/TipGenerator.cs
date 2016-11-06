using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Edison.Roshambo.Domain.DataAccess;
using Edison.Roshambo.Web.Models;

namespace Edison.Roshambo.Web.Infrastructure
{
    public static class TipGenerator
    {

        //todo: refactor
        public static async Task<TipModel> GenerateTip(RoshamboContext context, int gameId, int roundNumber,string userName)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(userName)) throw new ArgumentNullException(nameof(userName));
            
            var game = context.Games.Find(gameId);
            if (game == null)
                throw new InvalidOperationException("There is no game with such id.");


            var opName = game.OpponentName;
            var ownerName = game.LobbyOwner.UserName;

            var tip = new TipModel() {GameId = gameId, RoundNumber = roundNumber};

            var round = game.Rounds.Single(r => r.RoundNumber.Equals(roundNumber));

            // user is lobby owner
            if (userName.Equals(ownerName))
            {
                var opponentShape = round.OpponentShapeName;

                if (string.IsNullOrEmpty(opponentShape))
                {
                    tip.TipSucceeded = false;
                    return await Task.FromResult(tip);
                }

                tip.TipSucceeded = true;
                var trueShape = round.OpponentShapeName;
                var fakeShape = FakeShapeGenerator.GenerateFakeShape(trueShape);

                // mess data due to be unpredictable
                if (FakeShapeGenerator.GenerateRandomIndex() == 0)
                {
                    tip.ShapeOne = trueShape;
                    tip.ShapeTwo = fakeShape;
                }
                else
                {
                    tip.ShapeTwo = trueShape;
                    tip.ShapeOne = fakeShape;
                }
                return await Task.FromResult(tip);
            }
            // user is opponent
            else if (userName.Equals(opName))
            {
                // just dublicating logic here
                var trueShape = round.LobbyOwnerShapeName;
                if (string.IsNullOrEmpty(trueShape))
                {
                    tip.TipSucceeded = false;
                    return await Task.FromResult(tip);
                }

                tip.TipSucceeded = true;
                var fakeShape = FakeShapeGenerator.GenerateFakeShape(trueShape);

                if (FakeShapeGenerator.GenerateRandomIndex() == 0)
                {
                    tip.ShapeOne = trueShape;
                    tip.ShapeTwo = fakeShape;
                }
                else
                {
                    tip.ShapeTwo = trueShape;
                    tip.ShapeOne = fakeShape;
                }
                return await Task.FromResult(tip);
            }
            throw new ArgumentException(nameof(userName));
        }
    }
}
   






           