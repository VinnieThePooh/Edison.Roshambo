using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edison.Roshambo.Domain.Models
{
   public class Lobby
    {
        [ForeignKey("LobbyOwner")]
       public int LobbyId { get; set; }
       public string LobbyName { get; set; }
       public int IdLobbyState { get; set; }
       public virtual LobbyState LobbyState { get; set; }
       public virtual ICollection<Competitor> Players { get; set; }
       public virtual CustomUser LobbyOwner { get; set; }
       public virtual ICollection<Game> Games { get; set; }
       public DateTime? BlockingTime { get; set; }

       public Lobby()
       {
           Players = new List<Competitor>();
           Games = new List<Game>();
       }
    }
}
