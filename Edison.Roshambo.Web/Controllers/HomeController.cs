using System.Web.Mvc;

namespace Edison.Roshambo.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Roshambo is played here!!!";
            return View();
        }
        
    }
}