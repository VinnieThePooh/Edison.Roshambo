using System;
using Edison.Roshambo.Domain.DataAccess;
using Edison.Roshambo.Domain.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace Edison.Roshambo.Web
{
    
    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class DefaultUserManager : UserManager<CustomUser,int>
    {
        public DefaultUserManager(IUserStore<CustomUser,int> store) : base(store) { }
        public static DefaultUserManager Create(IdentityFactoryOptions<DefaultUserManager> options, IOwinContext context)
        {
            var manager = new DefaultUserManager(new UserStore<CustomUser,CustomIdentityRole, int, CustomIdentityUserLogin,CustomIdentityUserRole,CustomIdentityUserClaim>(context.Get<RoshamboContext>()));
            manager.UserValidator = new UserValidator<CustomUser,int>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 4,
                RequireNonLetterOrDigit = false,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = 
                    new DataProtectorTokenProvider<CustomUser,int>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    public class DefaultSignInManager : SignInManager<CustomUser, int>
    {
        public DefaultSignInManager(DefaultUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public static DefaultSignInManager Create(IdentityFactoryOptions<DefaultSignInManager> options, IOwinContext context)
        {
            return new DefaultSignInManager(context.GetUserManager<DefaultUserManager>(), context.Authentication);
        }
    }
}
