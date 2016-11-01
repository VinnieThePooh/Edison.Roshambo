using System.ComponentModel.DataAnnotations.Schema;

namespace Edison.Roshambo.Domain.Models
{
    public class GameRound
    {
        public GameRound(int idGame, int roundNumber)
        {
            IdGame = idGame;
            RoundNumber = roundNumber;
        }
        
        public int GameRoundId { get; set; }
        public int IdGame { get; set; }
        public bool OpponentUsedTip { get; set; }
        public bool LobbyOwnerUsedTip { get; set; }
        public int? IdRoundWinner { get; set; }
        public int RoundNumber { get; set; }
        public virtual Game ParentGame { get; set; }
        public string LobbyOwnerGameShapeName { get; set; }
        public string OpponentGameShapeName { get; set; }

        [ForeignKey("IdRoundWinner")]
        public  virtual CustomUser RoundWinner { get; set; }
    }
}