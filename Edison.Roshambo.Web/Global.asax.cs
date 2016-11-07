using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Edison.Roshambo.Domain.DataAccess;
using Edison.Roshambo.Web.Infrastructure;
using Edison.Roshambo.Web.Models;
using Microsoft.AspNet.Identity.Owin;

namespace Edison.Roshambo.Web
{
    public class MvcApplication : HttpApplication
    {
        public static IList<UserProjection> OnlineUsers { get; } = new List<UserProjection>();

        protected void Application_Start()
        {
            Database.SetInitializer(new CustomCreateDbIfNotExist());
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }


        protected void Session_End()
        {
            var user = HttpContext.Current.User;
            var context = HttpContext.Current.GetOwinContext().Get<RoshamboContext>();

            var toFind = context.Users.Single(u => u.UserName.Equals(user.Identity.Name));
            OnlineUsersTracker.RemoveOnlineUser(new UserProjection() {UserName = toFind.UserName, UserEmail = toFind.Email});
        }
    }
}
