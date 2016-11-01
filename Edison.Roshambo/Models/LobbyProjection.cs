using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Edison.Roshambo.Web.Infrastructure;

namespace Edison.Roshambo.Web.Models
{
    public class LobbyProjection
    {
        public string LobbyOwner { get; set; }
        public string LobbyState { get; set; }
        public string LobbyName { get; set; }
        public int LobbyId { get; set; }
        public string OpponentName { get; set; }

        public LobbyProjection(string name, string owner, int stateId, int id, string opponentName = null)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            
            LobbyName = name;
            LobbyOwner = owner;
            LobbyState = LobbyStateMapper.Map[stateId];
            LobbyId = id;
            OpponentName = opponentName;
        }

        public LobbyProjection()
        {
        }
    }
}