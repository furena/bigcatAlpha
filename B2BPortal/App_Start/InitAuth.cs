using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Web;
using Microsoft.Owin.Security.Cookies;
using B2BPortal.Infrastructure;
using System.Threading.Tasks;
using AzureB2BInvite;
using Newtonsoft.Json;
using B2BPortal.Common.Helpers;
using B2BPortal.Common.Utils;
using B2BPortal.Common.Models;

namespace B2BPortal
{
    public static class StartupAuth
    {
        public static ClaimsIdentity InitAuth(CookieResponseSignInContext ctx)
        {
            var ident = ctx.Identity;
            var hctx =
                (HttpContextWrapper)
                    ctx.Request.Environment.Single(e => e.Key == "System.Web.HttpContextBase").Value;

            var initResults = InitAuth(ident, hctx);
            return initResults;
        }

        private static ClaimsIdentity InitAuth(ClaimsIdentity ident, HttpContextBase hctx)
        {
            try
            {
                //if this is a multi-tenant visitor, we don't need to do anything else here
                if (ident.GetClaim("aud") == Settings.AppClientId_Preauth)
                {
                    ident.AddClaim(new Claim(CustomClaimTypes.AuthType, AuthTypes.B2EMulti));
                    return ident;
                }

                ident = TransformClaims(ident);

                return ident;
            }
            catch (Exception ex)
            {
                Debug.Assert(hctx.Session != null, "hctx.Session != null");
                hctx.Session["AuthError"] = "There was an error authenticating. Please contact the system administrator.";
                Logging.WriteToAppLog("Error during InitAuth.", EventLogEntryType.Error, ex);
                throw;
            }
        }
        private static ClaimsIdentity TransformClaims(ClaimsIdentity ident)
        {
            var issuer = ident.Claims.First().Issuer;

            ident.AddClaim(new Claim(CustomClaimTypes.AuthType, AuthTypes.Local));

            if (!ident.HasClaim(ClaimTypes.Email))
            {
                var name = ident.GetClaim(ClaimTypes.Name);
                if (name.IndexOf("@") > -1)
                {
                    ident.AddClaim(new Claim(ClaimTypes.Email, name));
                }
                else
                {
                    var upn = ident.GetClaim(ClaimTypes.Upn);
                    if (upn.IndexOf("@") > -1)
                    {
                        ident.AddClaim(new Claim(ClaimTypes.Email, upn));
                    }
                }
            }

            var response = InviteManager.GetDirectoryRoles(ident.GetClaim(CustomClaimTypes.ObjectIdentifier));
            if (response.Successful)
            {
                foreach (var role in response.Roles)
                {
                    switch (role.Type)
                    {
                        case "#microsoft.graph.directoryRole":
                            ident.AddClaim(new Claim(ClaimTypes.Role, role.DisplayName));
                            break;

                        case "#microsoft.graph.group":
                            ident.AddClaim(new Claim(CustomClaimTypes.MemberOfGroup, JsonConvert.SerializeObject(new GroupObject(role.DisplayName, role.Id))));
                            break;
                    }
                }
            }
            else
            {
                Logging.WriteToAppLog("Error retrieving app roles", EventLogEntryType.Error, new Exception(response.ErrorMessage));
            }

            var fullName = ident.Claims.FirstOrDefault(c => c.Type == "name").Value;
            ident.AddClaim(new Claim(CustomClaimTypes.FullName, fullName));

            return ident;
        }
    }
}