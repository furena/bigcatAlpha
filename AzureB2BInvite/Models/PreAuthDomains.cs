﻿using B2BPortal.Data;
using B2BPortal.Common.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using B2BPortal.Data.Models;
using B2BPortal.Common.Enums;
using B2BPortal.Common.Models;
using System.Web.Mvc;
using AzureB2BInvite.Utils;
using Newtonsoft.Json.Linq;

namespace AzureB2BInvite.Models
{
    [ModelBinder(typeof(PreAuthDomainModelBinder))]
    public class PreAuthDomain: DocModelBase, IDocModelBase
    {
        public PreAuthDomain()
        {
            DomainRedemptionSettings = new RedemptionSettings();
            InviteTemplateContent = new InviteTemplate();
            Groups = new List<GroupObject>();
        }
        /// <summary>
        /// The UPN of the user creating this PreAuth record
        /// </summary>
        [DisplayName("Created By")]
        [ScaffoldColumn(false)]
        [JsonProperty(PropertyName = "authUser")]
        public string AuthUser { get; set; }

        /// <summary>
        /// Date this PreAuth record was created
        /// </summary>
        [DisplayName("Create Date")]
        [ScaffoldColumn(false)]
        [JsonProperty(PropertyName = "createDate")]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Date this PreAuth record was updated
        /// </summary>
        [DisplayName("Last Updated")]
        [ScaffoldColumn(false)]
        [JsonProperty(PropertyName = "lastUpdated")]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// If this domain name matches a pre-authenticated guest requester, the guest will 
        /// automatically be enrolled and an invite sent out. Optionally, if there are any items
        /// in the Groups array, the new Guest user will be added to those Groups.
        /// </summary>
        [DisplayName("Domain name")]
        [Required(ErrorMessage = "Domain name (example.com) is required")]
        [JsonProperty(PropertyName = "domain")]
        public string DomainName { get; set; }

        /// <summary>
        /// RecordID for the email body template to use for pre-auth invitations handled by this record.
        /// </summary>
        [ScaffoldColumn(false)]
        [DisplayName("Invitation Template")]
        [JsonProperty(PropertyName = "inviteTemplateId")]
        public string InviteTemplateId { get; set; }

        /// <summary>
        /// Default site-wide redemption settings - will be in effect for non-preauthed domain invitations
        /// </summary>
        [DisplayName("Domain Redemption Settings")]
        [JsonProperty(PropertyName = "domainRedemptionSettings")]
        public RedemptionSettings DomainRedemptionSettings { get; set; }

        /// <summary>
        /// Settable by Global Admin only, "Member" or "Guest" (for non-GA, "Guest" is RO default)
        /// </summary>
        [DisplayName("Member Type")]
        [JsonProperty(PropertyName = "memberType")]
        public MemberType MemberType { get; set; }

        /// <summary>
        /// If domain is validated as an AAD tenant, profile may be set to auto-approve invitation requests
        /// </summary>
        [DisplayName("Auto Approve?")]
        [JsonProperty(PropertyName = "autoApprove")]
        public bool AutoApprove { get; set; }

        /// <summary>
        /// Optional - Users matching the domain name will be automatically added to each group in this list
        /// </summary>
        [JsonProperty(PropertyName = "groups")]
        [DisplayName("Group Assignments")]
        public List<GroupObject> Groups { get; set; }

        [JsonIgnore]
        [ScaffoldColumn(false)]
        public InviteTemplate InviteTemplateContent { get; private set; }

        public static async Task<IEnumerable<PreAuthDomain>> GetDomains(Expression<Func<PreAuthDomain, bool>> predicate = null)
        {
            var res = (await DocDBRepo.DB<PreAuthDomain>.GetItemsAsync(predicate)).OrderByDescending(c => c.LastUpdated);
            return res;
        }

        public static async Task<PreAuthDomain> GetDomain(string id)
        {
            var res = (await DocDBRepo.DB<PreAuthDomain>.GetItemAsync(id));
            if (res.InviteTemplateId != null)
            {
                res.InviteTemplateContent = (await DocDBRepo.DB<InviteTemplate>.GetItemAsync(res.InviteTemplateId));
            }
            return res;
        }
        public static async Task<PreAuthDomain> GetDomainByName(string domainName)
        {
            var res = (await DocDBRepo.DB<PreAuthDomain>.GetItemsAsync(d => d.DomainName.ToLower() == domainName.ToLower())).SingleOrDefault();
            if (res == null) return null;

            if (res.InviteTemplateId != null)
            {
                res.InviteTemplateContent = (await DocDBRepo.DB<InviteTemplate>.GetItemAsync(res.InviteTemplateId));
            }
            return res;
        }

        public static async Task<PreAuthDomain> AddDomain(PreAuthDomain domain)
        {
            domain.CreateDate = DateTime.UtcNow;
            domain.LastUpdated = DateTime.UtcNow;
            
            return (await DocDBRepo.DB<PreAuthDomain>.CreateItemAsync(domain));
        }

        public static async Task<PreAuthDomain> UpdateDomain(PreAuthDomain domain)
        {
            domain.LastUpdated = DateTime.UtcNow;

            domain = (await DocDBRepo.DB<PreAuthDomain>.UpdateItemAsync(domain));

            return domain;
        }
        public static async Task<dynamic> DeleteDomain(PreAuthDomain domain)
        {
            return (await DocDBRepo.DB<PreAuthDomain>.DeleteItemAsync(domain));
        }

        /// <summary>
        /// This method cleans up any existing domain records from the first version. That version stored an array of strings with
        /// the group id in each. The current version stores each group as a GroupObject with the name and id. This will retrieve the ids
        /// and update with the new object.
        /// </summary>
        /// <returns></returns>
        public static async Task<IEnumerable<PreAuthDomain>> RefreshAllPreAuthGroupData()
        {
            var groups = await new GraphUtil().GetGroups(null);
            dynamic domains = await DocDBRepo.DB<PreAuthDomain>.GetAllItemsGenericAsync();
            foreach (var domain in domains)
            {
                List<dynamic> gl = ((domain as dynamic).groups as JArray).ToList<dynamic>();
                if (JsonConvert.SerializeObject(gl).IndexOf("groupName") == -1)
                {
                    //needs updated
                    for (var i = 0; i < gl.Count; i++)
                    {
                        string id = gl[i];
                        var newG = groups.SingleOrDefault(g => g.GroupId == id);
                        if (newG != null)
                        {
                            gl.Remove(gl[i]);
                            gl.Insert(i, newG);
                        }
                    }
                    (domain as dynamic).groups = gl;

                    var newDomain = JsonConvert.DeserializeObject<PreAuthDomain>(JsonConvert.SerializeObject(domain));
                    await DocDBRepo.DB<PreAuthDomain>.UpdateItemAsync(newDomain);
                }
            }
            return await GetDomains();
        }
    }
}