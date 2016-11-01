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
    //todo: fix adding user to group in every key server method 
    [Authorize]
    public class GamesHub : Hub
    {
        public Task AddUser(UserProjection projection)
        {
            return Task.Run(() =>
            {
                var user = MvcApplication.OnlineUsers.FirstOrDefault(u => u.UserEmail.Equals(projection.UserEmail));

                if (user == null)
                {
                    MvcApplication.OnlineUsers.Add(projection);
                    Clients.All.newUserAdded(projection);
                }
            });
        }

        public Task RemoveUser(UserProjection projection)
        {
            return Task.Run(() =>
            {
                var user = MvcApplication.OnlineUsers.FirstOrDefault(u => u.UserEmail.Equals(projection.UserEmail));

                if (user != null)
                {
                    MvcApplication.OnlineUsers.Remove(projection);
                    Clients.All.userLeft(projection);
                }
            });
        }

        public async Task<IEnumerable<UserProjection>> GetOnlineUsers()
        {
            var userName = Context.User.Identity.Name;
            var conId = Context.ConnectionId;
            await ActualizeUserConnection(userName, conId);
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


        private async Task ActualizeUserConnection(string userName, string connectionId)
        {
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
            var user = context.Users.Single(x => x.UserName.Equals(userName));
            user.ConnectionId = connectionId;
            await context.SaveChangesAsync();
        }


        public async Task JoinLobby(string lobbyName)
        {
            var userName = HttpContext.Current.User.Identity.Name;
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();

            SetOpponentToLobby(context, lobbyName, userName);
            var lobbyId = SetLobbyState(context, LobbyStateNames.ReadyToStart, lobbyName);

            var lobbyOwner = context.Lobbies.Single(x => x.LobbyName.Equals(lobbyName)).LobbyOwner;


            var opConnectionId = context.Users.Single(u => u.UserName.Equals(userName)).ConnectionId;
            await Groups.Add(Context.ConnectionId, lobbyName);

            // notification for joiner
            var data = new
                {
                    LobbyName = lobbyName,
                    LobbyOwnerName = lobbyOwner.UserName,
                    OpponentName = lobbyOwner.UserName,
                    CurrentUserName = userName,
                    Message = HubResponseMessages.YouSuccessfullyJoinedLobby
                };

            
            Clients.Caller.lobbyJoined(data);

            // notification for lobby owner
            var dataToOwner =  new {
                Message = string.Format(HubResponseMessages.UserJoinedYourLobby, Context.User.Identity.Name),
                LobbyId = lobbyId,
                LobbyName = lobbyName,
                OpponentName = userName,
                CurrentUserName = lobbyOwner.UserName};

           
            Clients.Client(lobbyOwner.ConnectionId).lobbyWasJoined(dataToOwner);

            // update all other clients
            var dataToOthers = new
            {
                LobbyId = lobbyId,
                OpponentName = userName
            };
            
            Clients.AllExcept(new [] {lobbyOwner.ConnectionId, opConnectionId}).lobbyWasJoinedAll(dataToOthers);
        }

        //todo: test it
        //todo: refactor
        // remove lobby if called from lobby owner
        public async Task LeaveLobby(string lobbyName)
        {
            try
            {
                var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
                var userName = HttpContext.Current.User.Identity.Name;
                var user = context.Users.First(u => u.UserName.Equals(userName));
                var lobby =context.Lobbies.First(l => l.LobbyName.Equals(lobbyName));

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

                Game game = context.Games.First(g => g.IdLobbyOwner.Equals(lobby.LobbyOwner.Id) && string.IsNullOrEmpty(g.WinnerUserName));
                
                lobby.Players.Remove(opponent);
                context.SaveChanges();
                
                if (opponent.IdUser.Equals(user.Id))
                {   
                    if (criticalTime)
                    {
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
                        // send message about locking
                        Clients.Client(user.ConnectionId).userHasBeenBlocked(new {Message = message});
                    }
                    else Clients.Group(lobbyName).addLobbyMessage(string.Format(HubResponseMessages.LobbyChallengerLeftLobby,userName));
                    SetLobbyState(context, LobbyStateNames.AwaitingToPlayers, lobbyName);
                }
                else
                {
                    var message = string.Format(HubResponseMessages.LobbyOwnerLeftTheGame, lobby.LobbyOwner.UserName);
                    if (criticalTime)
                    {
                        var now = DateTime.Now;
                        lobby.BlockingTime = now;
                        game.WinnerUserName = opName;
                        SetLobbyState(context, LobbyStateNames.Blocked, lobbyName);

                        Clients.Group(lobbyName, Context.ConnectionId).lobbyOwnerLeftTheGame(new {Message = message});
                        Clients.All.lobbyHasBeenBlockedAll(new {lobby.LobbyName, BlockingTime = TimeSpan.FromMinutes(1)});

                        var time = ConfigurationManager.AppSettings[AppSettingsKeys.UserBlockingTime];
                        message = string.Format(HubResponseMessages.LobbyBlockingMessage, time);

                        //message to lobby owner about lobby blocking
                        var data = new {Message = message, LobbyName = lobbyName};
                        Clients.Client(user.ConnectionId).userHasBeenBlocked(new { Message = message });
                        //Clients.Client(user.ConnectionId).lobbyHasBeenBlocked(data);
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
                
            }
        }
         

        // not  tested
        public async Task RemoveLobby(string lobbyName)
        {
            var userName = HttpContext.Current.User.Identity.Name;
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();
            var user = context.Users.FirstOrDefault(u => u.UserName.Equals(userName) && u.OwnLobby.LobbyName.Equals(lobbyName));

            if (user != null)
            {
                user.OwnLobby = null;
                await context.SaveChangesAsync();
                Clients.All.tryRemoveLobby(new {LobbyId = user.Id, LobbyName = lobbyName, Result = true});
            }
        }


        public async Task BeginRound(int gameId, int lobbyName, int roundNumber)
        {
            

        }


        private void SetOpponentToLobby(RoshamboContext context, string lobbyName, string opponentName)
        {
            var opponent = context.Users.FirstOrDefault(u => u.UserName.Equals(opponentName));

            var lobby = context.Lobbies.FirstOrDefault(l => l.LobbyName.Equals(lobbyName));
            if (lobby != null && opponent != null)
            {
                opponent.ConnectionId = Context.ConnectionId;
                var competitor = new Competitor {User = opponent, Lobby = lobby};
                context.Set<Competitor>().Add(competitor);
                try
                {
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    var k = 0;
                }
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
            var game = context.Set<Game>().FirstOrDefault(g => g.LobbyOwner.UserName.Equals(userName) && string.IsNullOrEmpty(g.WinnerUserName));
            var lobby = context.Lobbies.FirstOrDefault(l => l.LobbyOwner.UserName.Equals(userName));

            if (game != null || lobby == null) return;

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

                // adding owner to current group cause of membership resets after page reloading
                await Groups.Add(Context.ConnectionId, lobby.LobbyName);
                // передавать надо больше информации,+ gameStatus
                var data = new { addedGame.GameId, LobbyOwnerName = userName, OpponentName = opponentName, lobby.LobbyName};
                Clients.Group(lobby.LobbyName).gameStarted(data);
            }
            catch (Exception exc)
            {
                var k = 0;
            }
        }

        public async Task BlockUser(string  userName)
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
                var lobby = await context.Lobbies.FirstOrDefaultAsync(l => l.LobbyId.Equals(user.Id) || l.LobbyName.Equals(lobbyName));

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
        
        // записать в бд ход противника
        // если отправляется уже второй ход противника, то вернуть обоим результаты раунда
        public async Task DoRoundAction(int shapeId, int roundId)
        {
               
        }

        // надо ли?
        public async Task SummarizeGamesResults(int gameId)
        {
            

        }
    }
}