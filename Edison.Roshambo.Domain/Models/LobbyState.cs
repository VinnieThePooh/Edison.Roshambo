using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edison.Roshambo.Domain.Models
{
    public class LobbyState
    {
        public int LobbyStateId { get; set; }
        public string Name { get; set; }
        public virtual  IList<Lobby> Lobbies { get; set; } = new List<Lobby>();
    }
}
