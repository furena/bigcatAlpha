using AzureB2BInvite;
using B2BPortal.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Filters;

namespace B2BPortal.Infrastructure.Filters
{
    public class CheckVisitorAttribute : ActionFilterAttribute, IAuthenticationFilter
    {
        public void OnAuthentication(AuthenticationContext filterContext)
        {
            var user = filterContext.Principal.Identity;
            //bail out if unauthenticated or if we're already logged into the home tenant
            if (!user.IsAuthenticated || (user.GetClaim(CustomClaimTypes.AuthType) == AuthTypes.Local))
            {
                return;
            }

            //check maintenance mode for all other authenticated users (currently, show maint page or allow logoff)
            //if (Settings.TenantID != currTenant && !Settings.VisitorAllowedPaths.Any(s => filterContext.HttpContext.Request.RawUrl.ToLower() == s))
            //{
            //    filterContext.Result = new ViewResult
            //    {
            //        ViewName = "~/Views/Shared/PreAuthError.cshtml"
            //    };
            //}
        }

        public void OnAuthenticationChallenge(AuthenticationChallengeContext filterContext)
        {

        }
    }
}

