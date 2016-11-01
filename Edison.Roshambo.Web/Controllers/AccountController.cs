using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Edison.Roshambo.Domain.Models;
using Edison.Roshambo.Web.Hubs;
using Edison.Roshambo.Web.Models;
using Edison.Roshambo.Web.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Security;

namespace Edison.Roshambo.Web.Controllers
{
    [System.Web.Mvc.Authorize]
    public class AccountController : Controller
    {
        private DefaultSignInManager _signInManager;
        private DefaultUserManager _userManager;
        private IHubContext _gamesHubContext = GlobalHost.ConnectionManager.GetHubContext<GamesHub>();

        public AccountController()
        {
        }

        public AccountController(DefaultUserManager userManager, DefaultSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public DefaultSignInManager SignInManager
        {
            get { return _signInManager ?? HttpContext.GetOwinContext().Get<DefaultSignInManager>(); }
            private set { _signInManager = value; }
        }

        public DefaultUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<DefaultUserManager>(); }
            private set { _userManager = value; }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result =
                await
                    SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe,
                        shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                {
                    var newUser = new UserProjection()
                    {
                        UserEmail = model.Email,
                        UserName = model.Email
                    };

                    AddOnlineUser(newUser);
                    _gamesHubContext.Clients.All.newUserAdded(newUser);
                    return RedirectToLocal(returnUrl);
                }
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }


        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new CustomUser {UserName = model.Email, Email = model.Email};
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, false, false);
                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return View("ForgotPasswordConfirmation");
            }

            return View(model);
        }

        

        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

            var userName = HttpContext.User.Identity.Name;
            var userProjection = new UserProjection() {UserEmail = userName, UserName = userName};

            RemoveOnlineUser(userProjection);
            _gamesHubContext.Clients.All.userLeft(userProjection);
            return RedirectToAction("Index", "Home");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers


        private void RemoveOnlineUser(UserProjection projection)
        {
            var user = MvcApplication.OnlineUsers.FirstOrDefault(u => u.UserEmail.Equals(projection.UserEmail));
            if (user != null)
                MvcApplication.OnlineUsers.Remove(user);
        }

        private void AddOnlineUser(UserProjection projection)
        {
            var user = MvcApplication.OnlineUsers.FirstOrDefault(u => u.UserEmail.Equals(projection.UserEmail));
            if (user == null)
                MvcApplication.OnlineUsers.Add(projection);
        }


        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Games");
        }

        #endregion
    }
}

