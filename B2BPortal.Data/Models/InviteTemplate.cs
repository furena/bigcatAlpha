using B2BPortal.Common.Interfaces;
using B2BPortal.Data.Models;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace B2BPortal.Data
{
    public class InviteTemplate : DocModelBase, IDocModelBase, IInviteTemplate
    {
        /// <summary>
        /// Site name displayed on home page (above description)
        /// </summary>
        [DisplayName("Template Name")]
        [Required]
        [JsonProperty(PropertyName = "templateName")]
        public string TemplateName { get; set; }

        /// <summary>
        /// Custom message added to emailed invitation
        /// </summary>
        [DisplayName("Template Content")]
        [AllowHtml]
        [Required]
        [JsonProperty(PropertyName = "templateContent")]
        public string TemplateContent { get; set; }

        /// <summary>
        /// Welcome message displayed on the home page - HTML allowed
        /// </summary>
        [DisplayName("Subject Template")]
        [JsonProperty(PropertyName = "subjectTemplate")]
        public string SubjectTemplate { get; set; }

        /// <summary>
        /// Date this version of settings was committed
        /// </summary>
        [ScaffoldColumn(false)]
        [DisplayName("Last Updated")]
        [JsonProperty(PropertyName = "lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [ScaffoldColumn(false)]
        [DisplayName("Template Version")]
        [JsonProperty(PropertyName = "templateVersion")]
        public int TemplateVersion { get; set; }

        [DisplayName("Template Author")]
        [JsonProperty(PropertyName = "templateAuthor")]
        public string TemplateAuthor { get; set; }

        public static async Task<InviteTemplate> GetTemplate(string inviteTemplateId)
        {
            var res = await DocDBRepo.DB<InviteTemplate>.GetItemAsync(inviteTemplateId);
            return res;
        }
    }
}