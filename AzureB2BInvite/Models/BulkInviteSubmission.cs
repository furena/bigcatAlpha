using AzureB2BInvite.Rules;
using B2BPortal.Common.Enums;
using B2BPortal.Common.Interfaces;
using B2BPortal.Common.Models;
using B2BPortal.Data;
using B2BPortal.Data.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AzureB2BInvite.Models
{
    /// <summary>
    /// Submitted from the web page to the controller. The email string is read and the Invitations
    /// collection is populated, then the object is stored in DocDB as a pending batch.
    /// </summary>
    public class BulkInviteSubmission : DocModelBase, IDocModelBase
    {
        [JsonProperty(PropertyName = "emailString")]
        public string EmailString { get; set; }

        [JsonProperty(PropertyName = "submissionDate")]
        public DateTime SubmissionDate { get; set; }

        [JsonProperty(PropertyName = "memberType")]
        public MemberType MemberType { get; set; }

        [JsonProperty(PropertyName = "groupList")]
        public GroupObject[] GroupList { get; set; }

        [JsonProperty(PropertyName = "invitationMessage")]
        public string InvitationMessage { get; set; }

        [JsonProperty(PropertyName = "inviteTemplateId")]
        public string InviteTemplateId { get; set; }

        [JsonProperty(PropertyName = "itemsSubmitted")]
        public int ItemsSubmitted { get; set; }

        [JsonProperty(PropertyName = "itemsProcessed")]
        public int ItemsProcessed { get; set; }

        [JsonProperty(PropertyName = "stopProcessing")]
        public bool StopProcessing { get; set; }

        public BulkInviteSubmission(DateTime submissionDate)
        {
            SubmissionDate = submissionDate;
        }

        public BulkInviteSubmission(): this(DateTime.UtcNow)
        {
        }

        public static async Task<BulkInviteSubmission> GetItem(string id)
        {
            return (await DocDBRepo.DB<BulkInviteSubmission>.GetItemsAsync(d => d.Id == id)).SingleOrDefault();
        }

        public static async Task<IEnumerable<BulkInviteSubmission>> GetItemsPending()
        {
            return (await DocDBRepo.DB<BulkInviteSubmission>.GetItemsAsync(d => d.ItemsProcessed < d.ItemsSubmitted)).ToList();
        }

        public static async Task<IEnumerable<BulkInviteSubmission>> GetItemHistory(int daysHistory)
        {
            DateTime cutoffDate = DateTime.UtcNow.AddDays(-1 * daysHistory);
            return (await DocDBRepo.DB<BulkInviteSubmission>.GetItemsAsync(d => d.SubmissionDate > cutoffDate)).ToList();
        }

        public static async Task<IEnumerable<GuestRequest>> GetGuestRequestsPending(string submissionId)
        {
            return (await DocDBRepo.DB<GuestRequest>.GetItemsAsync(d => d.BatchProcessId == submissionId && d.Disposition == Disposition.QueuePending && d.Status == null)).ToList();
        }

        public static async Task<GuestRequest> GetGuestItemDetail(string submissionId, string email)
        {
            return (await DocDBRepo.DB<GuestRequest>.GetItemsAsync(d => d.BatchProcessId == submissionId && d.EmailAddress == email)).SingleOrDefault();
        }

        public static async Task<BulkInviteSubmission> AddItem(BulkInviteSubmission submission, string authUser)
        {
            var guestList = Regex.Split(submission.EmailString, "\r\n|\r|\n");
            submission.ItemsSubmitted = guestList.Length;
            
            var item = (await DocDBRepo.DB<BulkInviteSubmission>.CreateItemAsync(submission));

            GuestRequest request;
            foreach(var guest in guestList)
            {
                request = new GuestRequest
                {
                    BatchProcessId = item.Id,
                    Disposition = Disposition.QueuePending,
                    RequestDate = DateTime.UtcNow,
                    LastModDate = DateTime.UtcNow,
                    EmailAddress = guest,
                    AuthUser = authUser
                };

                var doc = await DocDBRepo.DB<GuestRequest>.CreateItemAsync(request);
            }
            return item;
        }

        public static async Task<BulkInviteSubmission> UpdateItem(BulkInviteSubmission submission)
        {
            return (await DocDBRepo.DB<BulkInviteSubmission>.UpdateItemAsync(submission));
        }

        public static async Task<bool> KillBatch(string submissionId)
        {
            var item = await GetItem(submissionId);
            item.StopProcessing = true;
            await UpdateItem(item);
            return true;
        }

        public static async Task<bool> DeleteBatch(string submissionId)
        {
            var submission = await GetItem(submissionId);
            var requests = await GuestRequestRules.GetBatchRequest(submissionId);

            foreach (var request in requests)
            {
                await GuestRequestRules.DeleteAsync(request);
            }

            var results = await BulkInviteResults.GetItems(submissionId);
            foreach (var result in results)
            {
                await BulkInviteResults.DeleteItem(result);
            }

            await DeleteItem(submission);
            return true;
        }
        public static async Task<dynamic> DeleteItem(BulkInviteSubmission submission)
        {
            return (await DocDBRepo.DB<BulkInviteSubmission>.DeleteItemAsync(submission));
        }

        /// <summary>
        /// This method cleans up any existing batch records from the first version. That version stored an array of strings with
        /// the group id in each. The current version stores each group as a GroupObject with the name and id. This will retrieve the ids
        /// and update with the new object.
        /// </summary>
        /// <returns></returns>
        public static async Task<IEnumerable<BulkInviteSubmission>> RefreshAllPreAuthGroupData(int daysHistory)
        {
            var groups = await new GraphUtil().GetGroups(null);
            dynamic batches = await DocDBRepo.DB<BulkInviteSubmission>.GetAllItemsGenericAsync();
            foreach (var batch in batches)
            {
                List<dynamic> gl = ((batch as dynamic).groupList as JArray).ToList<dynamic>();
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
                    (batch as dynamic).groupList = gl;

                    var newBatch = JsonConvert.DeserializeObject<BulkInviteSubmission>(JsonConvert.SerializeObject(batch));
                    await DocDBRepo.DB<BulkInviteSubmission>.UpdateItemAsync(newBatch);
                }
            }
            return (await GetItemHistory(daysHistory)).OrderByDescending(c => c.SubmissionDate);
        }
    }
}
