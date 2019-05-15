using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security;
using B2BPortal.Infrastructure;
using System.Security.Claims;
using B2BPortal.Common.Helpers;

namespace B2BPortal.Controllers
{
    public class AccountController : Controller
    {
        public void SignInWorkMulti()
        {
            if (Request.IsAuthenticated)
            {
                EndSession();
            }

            HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = "/"
                },
                AuthTypes.B2EMulti);
        }

        public void SignIn()
        {
            var currTenant = User.Identity.GetClaim(CustomClaimTypes.TenantId);

            if (User.Identity.GetClaim(CustomClaimTypes.AuthType)==AuthTypes.Local)
            {
                EndSession();
            }
            var redir = Request.QueryString["redir"];
            if (redir == null) redir = "/";
            HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = redir,
                },
                AuthTypes.Local);
        }

        public void SignOut()
        {
            if (ClaimsPrincipal.Current.FindFirst(Startup.AcrClaimType) != null)
            {
                // To sign out the user, you should issue an OpenIDConnect sign out request
                if (Request.IsAuthenticated)
                {
                    IEnumerable<AuthenticationDescription> authTypes = HttpContext.GetOwinContext().Authentication.GetAuthenticationTypes();
                    HttpContext.GetOwinContext().Authentication.SignOut(authTypes.Select(t => t.AuthenticationType).ToArray());
                    Request.GetOwinContext().Authentication.GetAuthenticationTypes();
                }
            }
            else
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                HttpContext.GetOwinContext().Authentication.SignOut(
                    new AuthenticationProperties(dict),
                    AuthTypes.Local,
                    CookieAuthenticationDefaults.AuthenticationType);
            }

        }

        public void EndSession()
        {
            // If AAD sends a single sign-out message to the app, end the user's session, but don't redirect to AAD for sign out.
            HttpContext.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
        }
    }
}