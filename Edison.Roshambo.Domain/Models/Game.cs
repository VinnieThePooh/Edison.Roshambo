using System;
using System.Collections.Generic;

namespace Edison.Roshambo.Domain.Models
{
    public class Game
    {
        public Game()
        {
            GameDate = DateTime.Now;
            Rounds = new List<GameRound>();
        }

        public int GameId { get; set; }
        public int IdLobby { get; set; }
        public int IdLobbyOwner { get; set; }
        public string OpponentName { get; set; }
        public DateTime GameDate { get; set; }
        public virtual Lobby Lobby { get; set; }
        public string WinnerUserName { get; set; }
        public virtual CustomUser LobbyOwner { get; set; }
        public int LobbyOwnerScores { get; set; }
        public int OpponentScores { get; set; }
        public virtual ICollection<GameRound> Rounds { get; set; }
    }
}