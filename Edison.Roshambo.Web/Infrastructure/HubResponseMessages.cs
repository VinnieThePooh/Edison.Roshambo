namespace Edison.Roshambo.Web.Infrastructure
{
    public class HubResponseMessages
    {
        public const string LobbyCreated = "Lobby with name {0} was created";
        public const string LobbyAlreadyExists = "Lobby with name {0} already exists or you have one lobby";
        public const string Error = "Lobby was not created due to error: {0}";
        public const string LobbyJoined = "You successfully joined the lobby";
        public const string LobbyOwnerLeftTheGame = "Lobby owner {0} left the game. Lobby will be prevented from playing for one minute";
        public const string LobbyBlockingMessage = "Your lobby has been banned for {0} minutes due to leaving the game while playing";
        public const string LobbyChallengerLeftLobby = "Player {0} left your lobby";
        public const string LobbyChallengerLeftLobbyInCriticalTime = "Player {0} left your lobby. He got autolose and will be prevented from playing for one minute";
        public const string UserHaveBeenBlockedForAWhile = "You have been blocked for {0} minute(s) due to leaving lobby while playing";
        public const string UserHaveBeenUnblocked = "You have been unblocked and can continue playing Roshambo";
        public const string YouSuccessfullyJoinedLobby = "You successfully joined lobby";
        public const string UserJoinedYourLobby = "{0} joined your lobby";
        public const string YouSuccessfullyLeftLobby = "You successfully left lobby";
    }
}