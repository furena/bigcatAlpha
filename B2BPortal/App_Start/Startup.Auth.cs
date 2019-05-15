using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Notifications;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Configuration;
using System.IdentityModel.Tokens;
using B2BPortal.Infrastructure;
using System;
using System.Web;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using AzureB2BInvite.AuthCache;
using System.IdentityModel.Claims;
using B2BPortal.Common.Helpers;
using B2BPortal.Common.Utils;
using AzureB2BInvite;
using Microsoft.IdentityModel.Tokens;

namespace B2BPortal
{
    public partial class Startup
    {
        public const string AcrClaimType = "http://schemas.microsoft.com/claims/authnclassreference";

        // App config settings
        private static string clientId_admin = ConfigurationManager.AppSettings["ida:ClientId_Admin"];
        private static string clientSecret_admin = ConfigurationManager.AppSettings["ida:ClientSecret_Admin"];
        private static string clientId_preAuth = ConfigurationManager.AppSettings["ida:ClientId_PreAuth"];

        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string aadInstanceMulti = ConfigurationManager.AppSettings["ida:AadInstanceMulti"];
        private static string aadInstanceLocal = ConfigurationManager.AppSettings["ida:AadInstanceLocal"];

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            var authProvider = new CookieAuthenticationProvider
            {
                OnResponseSignIn = ctx =>
                {
                    StartupAuth.InitAuth(ctx);
                }
            };

            var cookieOptions = new CookieAuthenticationOptions
            {
                Provider = authProvider
            };

            app.UseCookieAuthentication(cookieOptions);

            // Required for AAD Multitenant (Pre-auth access to home tenant during sign-up)
            OpenIdConnectAuthenticationOptions multiOptions = new OpenIdConnectAuthenticationOptions
            {
                Authority = aadInstanceMulti,
                ClientId = clientId_preAuth,
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthenticationFailed = OnAuthenticationFailed,
                    RedirectToIdentityProvider = (context) =>
                    {
                        var issuer = "";
                        if (context.Request.QueryString.HasValue)
                        {
                            var user = HttpUtility.ParseQueryString(context.Request.QueryString.Value)["user"];
                            if (user.Length > 0)
                            {
                                var domain = user.Split('@')[1];
                                issuer = string.Format(aadInstanceLocal + "/oauth2/authorize?login_hint={1}", domain, user);
                            }
                        }

                        string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;
                        context.ProtocolMessage.RedirectUri = appBaseUrl + "/";
                        context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl;
                        if (issuer != "")
                        {
                            context.ProtocolMessage.IssuerAddress = issuer;
                        }
                        return Task.FromResult(0);
                    },
                },
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    SaveSigninToken = true
                },
                AuthenticationType = AuthTypes.B2EMulti,
            };
            app.UseOpenIdConnectAuthentication(multiOptions);

            // Required for AAD B2B
            OpenIdConnectAuthenticationOptions b2bOptions = new OpenIdConnectAuthenticationOptions
            {
                Authority = string.Format(aadInstanceLocal, tenant),
                ClientId = clientId_admin,
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = (context) =>
                    {
                        string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;
                        context.ProtocolMessage.RedirectUri = appBaseUrl + "/";
                        context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl;
                        return Task.FromResult(0);
                    },
                    AuthorizationCodeReceived = OnAuthorizationCodeReceived,
                    AuthenticationFailed = OnAuthenticationFailed
                },
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                },
                AuthenticationType = AuthTypes.Local,
                ResponseType = Microsoft.IdentityModel.Protocols.OpenIdConnectResponseTypes.CodeIdToken
            };
            app.UseOpenIdConnectAuthentication(b2bOptions);
        }

        // Used for avoiding yellow-screen-of-death
        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            notification.HandleResponse();
            if (notification.Exception.Message == "access_denied")
            {
                notification.Response.Redirect("/?message=access_denied");
            }
            else
            {
                notification.Response.Redirect("/Home/Error?message=" + notification.Exception.Message);
            }

            return Task.FromResult(0);
        }

        private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification context)
        {
            try
            {
                var code = context.Code;

                // Create a Client Credential Using an Application Key
                ClientCredential credential = new ClientCredential(clientId_admin, clientSecret_admin);
                string userObjId = context.AuthenticationTicket.Identity.GetClaim(Settings.ObjectIdentifier);

                Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(string.Format(aadInstanceLocal, tenant), new AdalCosmosTokenCache(userObjId, Utils.GetFQDN(context.Request)));
                AuthenticationResult result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                    code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, Settings.GraphResource);
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Error processing the retrieved auth code. ", ex);
                Logging.WriteToAppLog(newEx.Message, System.Diagnostics.EventLogEntryType.Error, newEx);
                throw newEx;
            }
       }
    }
}
