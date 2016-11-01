using Edison.Roshambo.Web;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Edison.Roshambo.Web.Startup))]
namespace Edison.Roshambo.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
