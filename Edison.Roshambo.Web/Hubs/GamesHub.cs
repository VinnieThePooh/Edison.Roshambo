﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Edison.Roshambo.Domain.DataAccess;
using Edison.Roshambo.Domain.Infrastructure;
using Edison.Roshambo.Domain.Models;
using Edison.Roshambo.Web.Infrastructure;
using Edison.Roshambo.Web.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.SignalR;

namespace Edison.Roshambo.Web.Hubs
{
    [Authorize]
    public class GamesHub : Hub
    {
        public static readonly object Locker = new object();

        // todo: refactor
        // this logic must be encapsulated in separated entity
        public async Task SendShape(int gameId, int roundNumber, int shapeId)
        {
            try
            {
                var currentUserName = HttpContext.Current.User.Identity.Name;
                var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
                var currentShape = context.Set<GameShape>().Single(s => s.ShapeId.Equals(shapeId));
                GameShape ownerShape = null, oppShape = null;


                var game = context.Games.First(g => g.GameId.Equals(gameId));
                var lobbyOwnerName = game.LobbyOwner.UserName;
                // problem place - how to add round only once?
                var round = game.Rounds.Single(gr => gr.IdGame.Equals(gameId) && gr.RoundNumber.Equals(roundNumber));

                var lobbyName = game.Lobby.LobbyName;
               
                if (currentUserName.Equals(lobbyOwnerName))
                {
                    round.LobbyOwnerShapeName = currentShape.ShapeName;
                    ownerShape = currentShape;
                }
                else
                {
                    round.OpponentShapeName = currentShape.ShapeName;
                    oppShape = currentShape;
                }
                await context.SaveChangesAsync();

                Clients.Caller.shapeWasSent(gameId, roundNumber, shapeId, null);
                
                // round winner can be resolved now
                if (round.OpponentShapeName != null && round.LobbyOwnerShapeName != null)
                {
                    if (oppShape == null)
                        oppShape = context.Set<GameShape>().Single(s => s.ShapeName.Equals(round.OpponentShapeName));
                    if (ownerShape == null)
                        ownerShape = context.Set<GameShape>().Single(s => s.ShapeName.Equals(round.LobbyOwnerShapeName));

                    PlayerMetadata owner = new PlayerMetadata(lobbyOwnerName, ownerShape.ShapeName);

                    // todo: fix: exception raises here sometimes
                    PlayerMetadata opponent = new PlayerMetadata(game.Lobby.Players.First().User.UserName, round.OpponentShapeName);

                    // define round results, winner
                    var winnerUsername = WinnerDeterminant.DetermineWinner(owner, opponent);
                    round.IdRoundWinner = GetIdRoundWinner(winnerUsername, context);
                    await context.SaveChangesAsync();
                    // define info about tips using and send to client
                    var data = new { OwnerShapeId = ownerShape.ShapeId, OpponentShapeId = oppShape.ShapeId, WinnerUsername = winnerUsername};
                    Clients.Group(lobbyName).roundEnded(data);
                }
            }
            catch (Exception e)
            {
                Clients.Caller.shapeWasSent(null, null, null, e.ToString());
            }
        }


        //todo: refactor 
        public async Task UseTip(int gameId, int roundNumber)
        {
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
            var userName = HttpContext.Current.User.Identity.Name;
            
            try
            {
                var game = await context.Games.FindAsync(gameId);
                var lobbyName = game.Lobby.LobbyName; 

                var tip = await TipGenerator.GenerateTip(context, gameId, roundNumber, userName);
                Clients.Caller.tipWasUsed(tip);
                Clients.OthersInGroup(lobbyName).opponentUsedTip(new {GameId = gameId, RoundNumber = roundNumber});
            }
            catch (Exception e)
            {
                Clients.Caller.tipWasUsed(new {Error = e.ToString()});
            }
        }


        

        // this method is called only once from winner. 
        // just to simplify
        public async Task EndGame(int gameId, int ownerScores, int opponentScores)
        {
            // lobby changes its state to "Summarazing Results" 
            // lobby will be moved in state "Awaiting for players" only when users close the playing window

            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
            var game = context.Games.Find(gameId);
            var lobby = game.Lobby;
            var lobbyName = lobby.LobbyName;
            

            SetLobbyState(context, LobbyStateNames.Summarizing, lobbyName);

            var winnerUserName = HttpContext.Current.User.Identity.Name;
            game.LobbyOwnerScores = ownerScores;
            game.OpponentScores = opponentScores;
            game.WinnerUserName = winnerUserName;

            var data = new { WinnerUserName = winnerUserName, Error = string.Empty };

            try
            {
                await context.SaveChangesAsync();
                Clients.Group(lobbyName).gameEnded(data);
            }
            catch (Exception e)
            {
                Clients.Group(lobbyName).gameEnded(new {Error = e.ToString()});
            }
        }


        private int? GetIdRoundWinner(string userName, RoshamboContext context)
        {
            if (string.IsNullOrEmpty(userName)) return null;
            var user = context.Users.Single(u => u.UserName.Equals(userName));
            return user.Id;
        }


        public async Task ActualizeConnection()
        {
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();

            var userName = Context.User.Identity.Name;
            var conId = Context.ConnectionId;
            await ActualizeConnectionInternal(userName, conId, context);

            string joinedOpName;
            string joinedLobbyName;



            Clients.Caller.restoreClientData();
        }


        public async Task<IEnumerable<UserProjection>> GetOnlineUsers()
        {
            return await Task.FromResult(MvcApplication.OnlineUsers);
        }


        public async Task<IEnumerable<LobbyProjection>> GetOnlineLobbies()
        {
            try
            {
                var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
                var lobbies =
                    (await context.Lobbies.ToArrayAsync()).Select(
                        x => new LobbyProjection
                        {
                            LobbyName = x.LobbyName,
                            LobbyOwner = x.LobbyOwner.UserName,
                            LobbyState = x.LobbyState.Name,
                            LobbyId = x.LobbyId,
                            OpponentName = x.Players.Any() ? x.Players.First().User.UserName : null
                        }).ToArray();
                return lobbies;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return await Task.FromResult((IEnumerable<LobbyProjection>) null);
            }
        }


        private async Task ActualizeConnectionInternal(string userName, string connectionId, RoshamboContext context)
        {
            var user = context.Users.Single(x => x.UserName.Equals(userName));
            user.ConnectionId = connectionId; 

            // restore groups user participates in
            if (user.OwnLobby != null)
               await Groups.Add(connectionId, user.OwnLobby.LobbyName);

            if (user.Competitor != null)
                await Groups.Add(connectionId, user.Competitor.Lobby.LobbyName);
                await context.SaveChangesAsync();
        }


        public async Task JoinLobby(string lobbyName)
        {
            var userName = HttpContext.Current.User.Identity.Name;
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();

            try
            {
                SetOpponentToLobby(context, lobbyName, userName);
                // await ActualizeConnectionInternal(userName, Context.ConnectionId);

                var lobbyId = SetLobbyState(context, LobbyStateNames.ReadyToStart, lobbyName);

                var lobbyOwner = context.Lobbies.Single(x => x.LobbyName.Equals(lobbyName)).LobbyOwner;
                var opponentConId = context.Users.Single(u => u.UserName.Equals(userName)).ConnectionId; 

                await Groups.Add(Context.ConnectionId, lobbyName);

                // notification for joiner
                var data = new
                {
                    LobbyName = lobbyName,
                    LobbyOwnerName = lobbyOwner.UserName,
                    OpponentName = lobbyOwner.UserName,
                    LobbyId = lobbyId,
                    CurrentUserName = userName                    
                };

                Clients.Caller.lobbyJoined(data);

                // notification for lobby owner
                var dataToOwner = new
                {
                    LobbyId = lobbyId,
                    LobbyName = lobbyName,
                    OpponentName = userName,
                    CurrentUserName = lobbyOwner.UserName
                };


                Clients.Client(lobbyOwner.ConnectionId).lobbyWasJoined(dataToOwner);

                // update all other clients
                var dataToOthers = new
                {
                    LobbyId = lobbyId,
                    OpponentName = userName
                };

                Clients.AllExcept(lobbyOwner.ConnectionId, opponentConId).lobbyWasJoinedAll(dataToOthers);
            }
            catch (Exception e)
            {
                var data = new {Error = e.ToString()};
                Clients.Caller.lobbyJoined(data);
            }
        }

        //todo: refactor it
        // remove lobby if called from lobby owner
        public async Task LeaveLobby(string lobbyName)
        {
            try
            {
                var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
                var userName = HttpContext.Current.User.Identity.Name;
                var user = context.Users.First(u => u.UserName.Equals(userName));

                var lobby = context.Lobbies.FirstOrDefault(l => l.LobbyName.Equals(lobbyName));

                // sometimes gets "sequence contains no elements"
                // это костыль
                // todo: clarify this
                if (lobby == null)
                    return;


                var opponent = lobby.Players.FirstOrDefault();
                // leave lobby  button already was clicked
                if (opponent == null)
                    return;

                var criticalTime = lobby.LobbyState.Name.Equals(LobbyStateNames.Playing);

                if (criticalTime)
                {
                    user.IsBlocked = true;
                    user.BlockingTime = DateTime.Now;
                }
                var opConId = opponent.User.ConnectionId;
                var opName = opponent.User.UserName;

                opponent.User.Competitor = null;

                lobby.Players.Remove(opponent);
                context.SaveChanges();
                Game game;

                // opponents leaves the game
                if (opponent.IdUser.Equals(user.Id))
                {
                    if (criticalTime)
                    {
                        game =
                            context.Games.Single(
                                g =>
                                    g.IdLobbyOwner.Equals(lobby.LobbyOwner.Id) && string.IsNullOrEmpty(g.WinnerUserName));
                        game.WinnerUserName = lobby.LobbyOwner.UserName;
                        var message = string.Format(HubResponseMessages.LobbyChallengerLeftLobbyInCriticalTime, userName);
                        Clients.Group(lobbyName, opConId).playerLeftTheGame(new {Message = message});

                        // construct message
                        var data = HubResponseMessages.UserHaveBeenBlockedForAWhile;
                        var time = ConfigurationManager.AppSettings[AppSettingsKeys.UserBlockingTime];
                        message = string.Format(data, time);

                        // if user has lobby - block it too
                        var opLobby = context.Lobbies.FirstOrDefault(l => l.LobbyOwner.Id.Equals(user.Id));
                        if (opLobby != null)
                        {
                            SetLobbyState(context, LobbyStateNames.Blocked, opLobby.LobbyName);
                        }

                        SetLobbyState(context, LobbyStateNames.AwaitingToPlayers, lobbyName);
                        // send message about locking
                        Clients.Client(user.ConnectionId).userHasBeenBlocked(new {Message = message});
                    }
                    else
                    {
                        // lobby state must be changed before opponent left lobby due to client logic
                        SetLobbyState(context, LobbyStateNames.AwaitingToPlayers, lobbyName);
                        Clients.Client(lobby.LobbyOwner.ConnectionId).opponentLeftLobby(new {OpponentName = userName});
                        Clients.Caller.userLeftLobby(new {lobby.LobbyId, lobby.LobbyName});
                    }
//                    SetLobbyState(context, LobbyStateNames.AwaitingToPlayers, lobbyName);
                }
                // lobby owner leaves the game
                else
                {
                    var message = string.Format(HubResponseMessages.LobbyOwnerLeftTheGame, lobby.LobbyOwner.UserName);
                    if (criticalTime)
                    {
                        game = context.Games.Single(g => g.IdLobbyOwner.Equals(lobby.LobbyOwner.Id) && string.IsNullOrEmpty(g.WinnerUserName));

                        var now = DateTime.Now;
                        lobby.BlockingTime = now;
                        game.WinnerUserName = opName;
                        SetLobbyState(context, LobbyStateNames.Blocked, lobbyName);

                        Clients.Group(lobbyName, Context.ConnectionId).lobbyOwnerLeftTheGame(new {Message = message});
                        // Clients.All.lobbyHasBeenBlockedAll(new {lobby.LobbyName, BlockingTime = TimeSpan.FromMinutes(1)});

                        var time = ConfigurationManager.AppSettings[AppSettingsKeys.UserBlockingTime];
                        message = string.Format(HubResponseMessages.LobbyBlockingMessage, time);

                        //message to lobby owner about lobby blocking
                        // is not needed here actually
                        var data = new {Message = message, LobbyName = lobbyName};
                        Clients.Client(user.ConnectionId).userHasBeenBlocked(new {Message = message});
                        Clients.Client(user.ConnectionId).lobbyHasBeenBlocked(data);
                    }
                    else
                    {
                        SetLobbyState(context, LobbyStateNames.AwaitingToPlayers, lobbyName);
                    }
                }

                await Groups.Remove(opConId, lobby.LobbyName);
                context.SaveChanges();
            }
            catch (Exception e)
            {
                Clients.Caller.userLeftLobby(new {Error = e.ToString()});
            }
        }


        // not  tested
        public async Task RemoveLobby(string lobbyName)
        {
            var userName = HttpContext.Current.User.Identity.Name;
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
            var user =
                context.Users.FirstOrDefault(u => u.UserName.Equals(userName) && u.OwnLobby.LobbyName.Equals(lobbyName));

            if (user != null)
            {
                user.OwnLobby = null;
                await context.SaveChangesAsync();
                Clients.All.tryRemoveLobby(new {LobbyId = user.Id, LobbyName = lobbyName, Result = true});
            }
        }


        // possible this method can be reduced later
        // this one just adds one record to collections of rounds
        public async Task StartRound(int gameId, int roundNumber)
        {
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
            var game = context.Games.Single(g => g.GameId.Equals(gameId));
            var notAdded = await Task.FromResult(false);
            lock (Locker)
            {
                var round = game.Rounds.SingleOrDefault(r => r.IdGame.Equals(gameId) && r.RoundNumber.Equals(roundNumber));
                if (round == null)
                {
                    round = new GameRound()
                    {
                        RoundNumber = roundNumber
                    };
                    game.Rounds.Add(round);
                    context.SaveChanges();
                }
            }
            
        }

        //add opponent to Players table
        private void SetOpponentToLobby(RoshamboContext context, string lobbyName, string opponentName)
        {
            var opponent = context.Users.FirstOrDefault(u => u.UserName.Equals(opponentName));

            var lobby = context.Lobbies.FirstOrDefault(l => l.LobbyName.Equals(lobbyName));
            if (lobby != null && opponent != null)
            {
                opponent.ConnectionId = Context.ConnectionId;
                var competitor = new Competitor {User = opponent, Lobby = lobby};
                context.Set<Competitor>().Add(competitor);
                context.SaveChanges();
            }
        }

        private int SetLobbyState(RoshamboContext context, string lobbyStateName, string lobbyName)
        {
            try
            {
                var lobbyId = -1;
                var lobby = context.Lobbies.FirstOrDefault(l => l.LobbyName.Equals(lobbyName));
                var lobbyState = context.Set<LobbyState>().First(x => x.Name.Equals(lobbyStateName));

                if (lobby != null)
                {
                    lobbyId = lobby.LobbyId;
                    lobby.IdLobbyState = lobbyState.LobbyStateId;
                    context.SaveChanges();
                    Clients.All.lobbyStateChanged(new {lobby.LobbyId, LobbyName = lobbyName, LobbyState = lobbyState.Name});
                }
                return lobbyId;
            }
            catch (Exception e)
            {
                return -1;
            }
        }

        //todo: выделить в репозиторий/GameManager что-ли?
        public async Task StartGame(string opponentName)
        {
            var userName = HttpContext.Current.User.Identity.Name;
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
            var game =
                context.Set<Game>()
                    .FirstOrDefault(
                        g => g.LobbyOwner.UserName.Equals(userName) && string.IsNullOrEmpty(g.WinnerUserName));
            var lobby = context.Lobbies.FirstOrDefault(l => l.LobbyOwner.UserName.Equals(userName));

            if (game != null || lobby == null) return;

            var oppId = lobby.Players.First().User.ConnectionId;
            var user = context.Users.FirstOrDefault(u => u.UserName.Equals(userName));


            try
            {
                game = new Game
                {
                    OpponentName = opponentName,
                    IdLobbyOwner = user.Id,
                    IdLobby = lobby.LobbyId
                };
                var addedGame = context.Set<Game>().Add(game);
                await context.SaveChangesAsync();
                
                SetLobbyState(context, LobbyStateNames.Playing, lobby.LobbyName);

                var data = new {addedGame.GameId, LobbyOwnerName = userName, OpponentName = opponentName, lobby.LobbyName};
                Clients.OthersInGroup(lobby.LobbyName).correctLobbyOwning();

                // opponent losts his group membership somehow
                // todo: clarify - why?
                // so addding it again
                await Groups.Add(oppId, lobby.LobbyName);
                Clients.Group(lobby.LobbyName).gameStarted(data);
            }
            catch (Exception exc)
            {
                 Clients.Caller.gameStarted(new  { Error = exc.ToString()});
            }
        }

        public async Task BlockUser(string userName)
        {
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
            var user = context.Users.FirstOrDefault(x => x.UserName.Equals(userName));
            if (user != null)
            {
                var userConnectionId = user.ConnectionId;
                user.IsBlocked = true;
                user.BlockingTime = DateTime.Now;
                await context.SaveChangesAsync();

                var data = HubResponseMessages.UserHaveBeenBlockedForAWhile;
                var time = ConfigurationManager.AppSettings[AppSettingsKeys.UserBlockingTime];
                var message = string.Format(data, time);
                Clients.Client(userConnectionId).haveBeenBlocked(message);
            }
        }


        public async Task UnblockUser(string userName)
        {
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
            var user = context.Users.FirstOrDefault(x => x.UserName.Equals(userName));
            if (user != null)
            {
                var userConnectionId = user.ConnectionId;
                user.IsBlocked = false;
                user.BlockingTime = null;
                await context.SaveChangesAsync();
                var message = HubResponseMessages.UserHaveBeenUnblocked;

                var lobby = context.Lobbies.FirstOrDefault(x => x.LobbyOwner.Id.Equals(user.Id));
                if (lobby != null)
                {
                    SetLobbyState(context, LobbyStateNames.AwaitingToPlayers, lobby.LobbyName);
                }
                Clients.Client(userConnectionId).userHasBeenUnblocked(message);
            }
        }


        public async Task CreateLobby(string lobbyName)
        {
            var userName = HttpContext.Current.User.Identity.Name;
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();

            try
            {
                var user = context.Users.Single(u => u.UserName.Equals(userName));
                var lobby =
                    await
                        context.Lobbies.FirstOrDefaultAsync(
                            l => l.LobbyId.Equals(user.Id) || l.LobbyName.Equals(lobbyName));

                if (lobby == null)
                {
                    lobby = new Lobby
                    {
                        LobbyName = lobbyName,
                        IdLobbyState = 1
                    };

                    user.OwnLobby = lobby;
                    context.Entry(user).State = EntityState.Modified;
                    user.ConnectionId = Context.ConnectionId;
                    await context.SaveChangesAsync();
                    await Groups.Add(Context.ConnectionId, lobbyName);
                    Clients.All.tryCreateLobby(new
                    {
                        Result = true,
                        Message = string.Format(HubResponseMessages.LobbyCreated, lobbyName),
                        Lobby = new LobbyProjection(lobbyName, userName, lobby.IdLobbyState, lobby.LobbyId)
                    });
                    return;
                }
                Clients.Caller.tryCreateLobby(
                    new {Result = false, Message = string.Format(HubResponseMessages.LobbyAlreadyExists, lobbyName)});
            }
            catch (Exception e)
            {
                Clients.Caller.tryCreateLobby(
                    new {Result = false, Message = string.Format(HubResponseMessages.Error, e.Message)});
            }
        }
    }
}