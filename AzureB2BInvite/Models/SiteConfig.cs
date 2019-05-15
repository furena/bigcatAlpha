using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using B2BPortal.Data;
using B2BPortal.Data.Models;
using B2BPortal.Common.Interfaces;
using System.Web.Mvc;
using Westwind.Web.Utilities;

namespace AzureB2BInvite.Models
{
    public class SiteConfig : DocModelBase, IDocModelBase
    {
        public SiteConfig()
        {
            SiteRedemptionSettings = new RedemptionSettings();
            SiteRedemptionSettings.InviteRedirectUrl = string.Format("https://myapps.microsoft.com/{0}", Settings.Tenant);
            InviteTemplateContent = new InviteTemplate();
        }

        /// <summary>
        /// Site name displayed on home page (above description)
        /// </summary>
        [DisplayName("Site Name")]
        [JsonProperty(PropertyName = "siteName")]
        public string SiteName { get; set; }

        /// <summary>
        /// Name of inviting organization
        /// </summary>
        [DisplayName("Inviting Organization")]
        [JsonProperty(PropertyName = "invitingOrg")]
        public string InvitingOrg { get; set; }

         /// <summary>
        /// Welcome message displayed on the home page - HTML allowed
        /// </summary>
        [DisplayName("Welcome Message")]
        [JsonProperty(PropertyName = "welcomeMessage")]
        public string WelcomeMessage { get; set; }

        /// <summary>
        /// Is clicking the TOS checkbox required before allowing the guest request to be submitted?
        /// </summary>
        [DisplayName("Require TOS Agreement")]
        [JsonProperty(PropertyName = "requireTosAgreement")]
        public bool RequireTOSAgreement { get; set; }

        /// <summary>
        /// Full-text of Terms of Service - HTML permissible
        /// </summary>
        [DisplayName("TOS Document")]
        [AllowHtml]
        [JsonProperty(PropertyName = "tosDocument")]
        public string TOSDocument { get; set; }

        /// <summary>
        /// Default site-wide redemption settings - will be in effect for non-preauthed domain invitations
        /// </summary>
        [DisplayName("Site Redemption Settings")]
        [JsonProperty(PropertyName = "siteRedemptionSettings")]
        public RedemptionSettings SiteRedemptionSettings { get; set; }

        /// <summary>
        /// RecordID for the email body template to use for default pre-auth invitations not handled by a pre-auth domain record
        /// </summary>
        [ScaffoldColumn(false)]
        [DisplayName("Invitation Template")]
        [JsonProperty(PropertyName = "inviteTemplateId")]
        public string InviteTemplateId { get; set; }

        /// <summary>
        /// Date this version of settings was committed
        /// </summary>
        [ScaffoldColumn(false)]
        [DisplayName("Config Date")]
        [JsonProperty(PropertyName = "configDate")]
        public DateTime ConfigDate { get; set; }

        [ScaffoldColumn(false)]
        [JsonProperty(PropertyName = "configVersion")]
        [DisplayName("Config Version")]
        public int ConfigVersion { get; set; }

        /// <summary>
        /// UPN of author of this config version
        /// </summary>
        [JsonProperty(PropertyName = "configAuthor")]
        [DisplayName("Config Author")]
        public string ConfigAuthor { get; set; }

        [JsonIgnore]
        [ScaffoldColumn(false)]
        public InviteTemplate InviteTemplateContent { get; set; }
    }
}