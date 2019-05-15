using System;
using System.Collections.Generic;
using System.Linq;
using static AzureB2BInvite.AdalUtil;
using AzureB2BInvite.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace AzureB2BInvite
{
    public class ProfileManager
    {
        public static AADUserProfile GetUserProfile(string upn)
        {
            AADUserProfile res = null;
            var userUri = string.Format("{0}/{1}/users/{2}", Settings.GraphResource, Settings.GraphApiVersion, upn);
            var serverResponse = CallGraph(userUri);
            if (serverResponse.ResponseContent != null)
            {
                res = JsonConvert.DeserializeObject<AADUserProfile>(serverResponse.ResponseContent);
            }
            return res;
        }

        public static void UpdateProfile(dynamic userData, string upn)
        {
            var userUri = string.Format("{0}/{1}/users/{2}", Settings.GraphResource, Settings.GraphApiVersion, upn);
            var serverResponse = CallGraph(userUri, userData, true);
        }

        public static async Task<string> GetRedirUrl(string upn)
        {
            var domainName = upn.Split('@')[1];
            var domProfile = (await PreAuthDomain.GetDomains(d => d.DomainName == domainName)).SingleOrDefault();
            var res = (domProfile == null) ? Settings.CurrSiteConfig.SiteRedemptionSettings.InviteRedirectUrl :
                (!string.IsNullOrEmpty(domProfile.DomainRedemptionSettings.InviteRedirectUrl))
                    ? domProfile.DomainRedemptionSettings.InviteRedirectUrl
                        : Settings.CurrSiteConfig.SiteRedemptionSettings.InviteRedirectUrl;

            return res;
        }
    }
}