using System.Collections.Generic;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Edison.Roshambo.Web.Hubs;
using Edison.Roshambo.Web.Models;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;

namespace Edison.Roshambo.Web
{
    public class MvcApplication : HttpApplication
    {
        public static IList<UserProjection> OnlineUsers { get; } = new List<UserProjection>();

        protected void Application_Start()
        {
//            ReplaceDefaultSerializer();
            Database.SetInitializer(new CustomCreateDbIfNotExist());
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

        }


        protected void Session_End()
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<GamesHub>();
            var user = HttpContext.Current.User;

            hubContext.Clients.All.userLeft(); 
        }


        private void ReplaceDefaultSerializer()
        {
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            var serializer = JsonSerializer.Create(serializerSettings);
            GlobalHost.DependencyResolver.Register(typeof(JsonSerializer), () => serializer);
        }
    }
}
