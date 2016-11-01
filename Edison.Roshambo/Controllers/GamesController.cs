using System.Linq;
using System.Web;
using System.Web.Mvc;
using Edison.Roshambo.Domain.DataAccess;
using Edison.Roshambo.Web.Hubs;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.Identity.Owin;

namespace Edison.Roshambo.Web.Controllers
{
    [System.Web.Mvc.Authorize]
    public class GamesController : Controller
    {
        // main games page
        public ActionResult Index()
        {
            var context = HttpContext.GetOwinContext().Get<RoshamboContext>();
            var userName = HttpContext.User.Identity.Name;
            var user = context.Users.Single(x => x.UserName.Equals(userName));
            return View(user);
        }
    }
}