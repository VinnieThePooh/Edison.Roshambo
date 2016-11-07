using System.Linq;
using Edison.Roshambo.Web.Hubs;
using Edison.Roshambo.Web.Models;
using Microsoft.AspNet.SignalR;

namespace Edison.Roshambo.Web.Infrastructure
{
    public static class OnlineUsersTracker
    {
        static readonly IHubContext GamesHubContext = GlobalHost.ConnectionManager.GetHubContext<GamesHub>();

        internal static void RemoveOnlineUser(UserProjection projection)
        {
            var user = MvcApplication.OnlineUsers.FirstOrDefault(u => u.UserEmail.Equals(projection.UserEmail));
            if (user != null)
            {
                MvcApplication.OnlineUsers.Remove(user);
                GamesHubContext.Clients.All.userLeftSite(projection);
            }
        }

        internal static void AddOnlineUser(UserProjection projection)
        {
            var user = MvcApplication.OnlineUsers.FirstOrDefault(u => u.UserEmail.Equals(projection.UserEmail));
            if (user == null)
            {
                MvcApplication.OnlineUsers.Add(projection);
                GamesHubContext.Clients.All.userJoinedSite(projection);
            }
        }

    }
}