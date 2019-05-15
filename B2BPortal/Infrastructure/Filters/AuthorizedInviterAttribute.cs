using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using B2BPortal.Infrastructure;
using AzureB2BInvite;
using B2BPortal.Common.Helpers;

namespace B2BPortal.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AuthorizedInviterAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Check authorization
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var currentUser = filterContext.HttpContext.User.Identity;
            var qs = filterContext.HttpContext.Request.Url.PathAndQuery;

            var redir = new RouteValueDictionary
                {
                    { "controller", "account" }, { "action", "signin" }, { "area", "" }
                };

            redir["redir"] = qs;

            if (!currentUser.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(redir);
                return;
            }

            if (currentUser.GetClaim(CustomClaimTypes.AuthType) != AuthTypes.Local)
            {
                filterContext.Result = new RedirectToRouteResult(redir);
                return;
            }

            if (!currentUser.IsInAnyRole(Settings.InviterRoleNames))
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "home" }, { "action", "error" }, { "area", "" }
                });
            }
        }
    }
}