using AzureB2BInvite.AuthCache;
using AzureB2BInvite.Models;
using B2BPortal.Common.Utils;
using B2BPortal.Data;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using System.Xml;

namespace AzureB2BInvite
{
    public static class AdalUtil
    {
        public static async Task<AuthenticationResult> AuthenticateApp()
        {
            return await AuthenticateApp(null, null);
        }

        public static async Task<AuthenticationResult> AuthenticateApp(string graphResource, CacheUser user)
        {
            AuthenticationResult authResult = null;
            AuthenticationContext authContext = null;
            try
            {
                string resource = graphResource ?? Settings.GraphResource;
                var clientCred = new ClientCredential(Settings.AppClientId_Admin, Settings.AppClientSecret_Admin);

                if (user != null)
                {
                    authContext = new AuthenticationContext(string.Format(Settings.AADInstanceLocal, Settings.TenantID), new AdalCosmosTokenCache(user.UserObjId, user.HostName));
                    var tc = authContext.TokenCache.ReadItems();
                    authResult = await authContext.AcquireTokenSilentAsync(resource, clientCred, new UserIdentifier(user.UserObjId, UserIdentifierType.UniqueId));
                }
                else
                {
                    authContext = new AuthenticationContext(string.Format(Settings.AADInstanceLocal, Settings.TenantID));
                    authResult = await authContext.AcquireTokenAsync(resource, clientCred);
                }

                return authResult;
            }
            catch(AdalSilentTokenAcquisitionException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public static AdalResponse CallGraph(string uri, dynamic postContent = null, bool isUpdate = false, string graphResource = null, CacheUser user = null, string accessToken = null)
        {
            string resource = graphResource ?? Settings.GraphResource;

            var res = new AdalResponse
            {
                Successful = true
            };
            HttpResponseMessage response = null;
            try
            {
                AuthenticationResult authResult = null;

                if (accessToken == null)
                {
                    // Get auth token
                    var task = Task.Run(async () => {
                        authResult = await AuthenticateApp(resource, user);
                    });
                    task.Wait();

                    accessToken = authResult.AccessToken;
                }

                var bearer = new AuthenticationHeaderValue("Bearer", accessToken);

                //getting inconsistent results (chunked vs not chunked) with async calls - switching to webclient/sync
                if (postContent != null)
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(300);
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        httpClient.DefaultRequestHeaders.Add("client-request-id", Guid.NewGuid().ToString());
                        httpClient.DefaultRequestHeaders.GetValues("client-request-id").Single();
                        string serialized = JsonConvert.SerializeObject(postContent);

                        HttpContent content = new StringContent(serialized, Encoding.UTF8, "application/json");

                        if (isUpdate)
                        {
                            var method = new HttpMethod("PATCH");
                            var request = new HttpRequestMessage(method, uri)
                            {
                                Content = content
                            };

                            response = httpClient.SendAsync(request).Result;
                        }
                        else
                        {
                            response = httpClient.PostAsync(uri, content).Result;
                        }
                        res.ResponseContent = response.Content.ReadAsStringAsync().Result;
                        res.StatusCode = response.StatusCode;
                        res.Message = response.ReasonPhrase;
                        if (!response.IsSuccessStatusCode)
                        {
                            res.Successful = false;
                            var serverError = JsonConvert.DeserializeObject<GraphError>(res.ResponseContent);
                            var reason = (response == null ? "N/A" : response.ReasonPhrase);
                            var serverErrorMessage = (serverError.Error == null) ? "N/A" : serverError.Error.Message;
                            res.Message = string.Format("(Server response: {0}. Server detail: {1})", reason, serverErrorMessage);
                            return res;
                        }
                    }
                }
                else
                {
                    using (var webClient = new WebClient())
                    {
                        webClient.Headers.Add("Authorization", bearer.ToString());
                        webClient.Headers.Add("client-request-id", Guid.NewGuid().ToString());

                        res.ResponseContent = webClient.DownloadString(uri);
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                res.Successful = false;
                res.Message = ex.Message;
                return res;
            }
        }

        public static OIDConfigResponse FindPublicAADTenant(string domainName)
        {
            var uri = string.Format("https://login.windows.net/{0}/.well-known/openid-configuration", domainName);
            string res = "";
            using (var web = new WebClient())
            {
                try
                {
                    res = web.DownloadString(uri);
                }
                catch (WebException exception)
                {
                    using (var reader = new StreamReader(exception.Response.GetResponseStream()))
                    {
                        res = reader.ReadToEnd();
                    }
                }
                return JsonConvert.DeserializeObject<OIDConfigResponse>(res);
            }
        }

        /// <summary>
        ///Retrieves tenant branding for use on the portal and in custom emails. A screen-scraping workaround. 
        ///Watch for a better way in the future and/or be aware that if this script block ever
        ///moves on the page, this might break.
        /// </summary>
        public static void GetTenantBranding()
        {
            var uri = string.Format("https://login.microsoftonline.com/{0}/oauth2/authorize", Settings.TenantID);
            Settings.Branding = new TenantBranding();
            using (var web = new WebClient())
            {
                try
                {
                    web.Headers.Add("content-type", "application/x-www-form-urlencoded");
                    var res = web.UploadString(uri, "POST", string.Format("client_id={0}", Settings.AppClientId_Admin));
                    var doc = new HtmlDocument();
                    doc.LoadHtml(res);
                    var scriptBlock = doc.DocumentNode.SelectSingleNode("//script");

                    var strdata = scriptBlock.InnerText;
                    strdata = scriptBlock.InnerText.Replace("//<![CDATA[\n$Config=", "");
                    strdata = strdata.Remove(strdata.Length - 7, 7);
                    dynamic data = JsonConvert.DeserializeObject(strdata);
                    dynamic brand = data.staticTenantBranding[0];

                    Settings.Branding.BannerLogo.Name = brand.BannerLogo?.ToString();
                    Settings.Branding.Illustration.Name = brand.Illustration?.ToString();
                    Settings.Branding.TileDarkLogo.Name = brand.TileDarkLogo?.ToString();
                    Settings.Branding.TileLogo.Name = brand.TileLogo?.ToString();
                }
                catch(Exception ex)
                {
                    Logging.WriteToAppLog("Unable to retrieve tenant branding", System.Diagnostics.EventLogEntryType.Error, ex);
                }
            }
        }
    }
    public class TenantBranding
    {
        public BrandingImage BannerLogo { get; set; }
        public BrandingImage TileLogo { get; set; }
        public BrandingImage TileDarkLogo { get; set; }
        public BrandingImage Illustration { get; set; }

        public TenantBranding()
        {
            BannerLogo = new BrandingImage();
            TileLogo = new BrandingImage();
            TileDarkLogo = new BrandingImage();
            Illustration = new BrandingImage();
        }
        public class BrandingImage
        {
            private string _name;
            public string Name {
                get {
                    return Name;
                }
                set {
                    _name = value;
                    if (_name == null || _name.Length == 0)
                        return;
                    Image = GetImage(value);
                }
            }
            public byte[] Image { get;  set; }
            private byte[] GetImage(string path)
            {
                using (var web = new WebClient())
                {
                    return web.DownloadData(path);
                }
            }
        }
    }

    public class OIDConfigResponse
    {
        [JsonProperty(PropertyName = "authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }

        [JsonProperty(PropertyName = "token_endpoint")]
        public string TokenEndpoint { get; set; }

        [JsonProperty(PropertyName = "token_endpoint_auth_methods_supported")]
        public string[] TokenEndpointAuthMethodsSupported { get; set; }

        [JsonProperty(PropertyName = "jwks_uri")]
        public string JwksUri { get; set; }

        [JsonProperty(PropertyName = "response_modes_supported")]
        public string[] ResponseModesSupported { get; set; }

        [JsonProperty(PropertyName = "subject_types_supported")]
        public string[] SubjectTypesSupported { get; set; }

        [JsonProperty(PropertyName = "id_token_signing_alg_values_supported")]
        public string[] IdTokenSigningAlgValuesSupported { get; set; }

        [JsonProperty(PropertyName = "http_logout_supported")]
        public bool HttpLogoutSupported { get; set; }

        [JsonProperty(PropertyName = "frontchannel_logout_supported")]
        public bool FrontchannelLogoutSupported { get; set; }

        [JsonProperty(PropertyName = "end_session_endpoint")]
        public string EndSessionEndpoint { get; set; }

        [JsonProperty(PropertyName = "response_types_supported")]
        public string[] ResponseTypesSupported { get; set; }

        [JsonProperty(PropertyName = "scopes_supported")]
        public string[] ScopesSupported { get; set; }

        [JsonProperty(PropertyName = "issuer")]
        public string Issuer { get; set; }

        [JsonProperty(PropertyName = "claims_supported")]
        public string[] ClaimsSupported { get; set; }

        [JsonProperty(PropertyName = "microsoft_multi_refresh_token")]
        public string MicrosoftMultiRefreshToken { get; set; }

        [JsonProperty(PropertyName = "check_session_iframe")]
        public string CheckSessionIframe { get; set; }

        [JsonProperty(PropertyName = "userinfo_endpoint")]
        public string UserInfoEndpoint { get; set; }

        [JsonProperty(PropertyName = "tenant_region_scope")]
        public string TenantRegionScope { get; set; }

        [JsonProperty(PropertyName = "cloud_instance_name")]
        public string CloudInstanceName { get; set; }

        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }
    }

    public class OIDConfigError
    {
        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        [JsonProperty(PropertyName = "error_description")]
        public string ErrorDescription { get; set; }

        [JsonProperty(PropertyName = "error_codes")]
        public string[] ErrorCodes { get; set; }

        [JsonProperty(PropertyName = "Timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty(PropertyName = "trace_id")]
        public string TraceID { get; set; }

        [JsonProperty(PropertyName = "correlation_id")]
        public string CorrelationId { get; set; }
    }

    public class AdalResponse
    {
        public string ResponseContent { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public bool Successful { get; set; }
        public string Message { get; set; }
    }
}