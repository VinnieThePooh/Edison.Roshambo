using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Edison.Roshambo.Domain.DataAccess;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Edison.Roshambo.Domain.Models
{
   public class CustomUser: IdentityUser<int, CustomIdentityUserLogin, CustomIdentityUserRole, CustomIdentityUserClaim>
   {
       public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<CustomUser, int> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // only 5 default claims are stored.
            return userIdentity;
        }

       public virtual Lobby OwnLobby { get; set; }
       public int? IdCompetitor { get; set; }
       public virtual Competitor Competitor { get; set; }
       public virtual ICollection<Game> Games { get; set; }
       public virtual ICollection<GameRound> GameRounds { get; set; }
       public string ConnectionId { get; set; }
       public bool IsBlocked { get; set; }
       
       public DateTime? BlockingTime { get; set; }

       public CustomUser()
       {
            Games = new List<Game>();
            GameRounds = new List<GameRound>();
        }

       public CustomUser(string connectionId):this()
       {
           if (connectionId == null) throw new ArgumentNullException(nameof(connectionId));
           ConnectionId = connectionId;
       }
    }
}
