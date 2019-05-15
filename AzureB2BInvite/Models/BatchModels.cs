using B2BPortal.Common.Interfaces;
using B2BPortal.Data;
using B2BPortal.Data.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AzureB2BInvite.Models
{

    public class BulkInviteResults : DocModelBase, IDocModelBase
    {
        [JsonProperty(PropertyName = "submissionId")]
        public string SubmissionId { get; set; }

        [JsonProperty(PropertyName = "invitationResults")]
        public GraphBatchResponse InvitationResults { get; set; }

        [JsonProperty(PropertyName = "errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty(PropertyName = "processingDate")]
        public DateTime ProcessingDate { get; set; }

        public BulkInviteResults(string submissionId = null)
        {
            if (submissionId != null)
            {
                SubmissionId = submissionId;
            }
            InvitationResults = new GraphBatchResponse
            {
                Responses = new List<BulkResponse>()
            };
            ProcessingDate = DateTime.UtcNow;
        }

        public static async Task<IEnumerable<BulkInviteResults>> GetItems(string submissionId)
        {
            return (await DocDBRepo.DB<BulkInviteResults>.GetItemsAsync(d => d.SubmissionId == submissionId)).OrderByDescending(c => c.ProcessingDate).ToList();
        }

        public static async Task<BulkInviteResults> AddItem(BulkInviteResults results)
        {
            return await DocDBRepo.DB<BulkInviteResults>.CreateItemAsync(results);
        }
        public static async Task<BulkInviteResults> DeleteItem(BulkInviteResults results)
        {
            var res = await DocDBRepo.DB<BulkInviteResults>.DeleteItemAsync(results);
            return (BulkInviteResults)res;
        }
    }

    public class GraphBatch
    {
        [JsonProperty(PropertyName = "requests")]
        public List<BulkInviteRequest> Requests { get; set; }

        public GraphBatch()
        {
            Requests = new List<BulkInviteRequest>();
        }
    }

    public class GraphBatchResponse
    {
        [JsonProperty(PropertyName = "responses")]
        public List<BulkResponse> Responses { get; set; }

        [JsonProperty(PropertyName = "nextLink")]
        public string NextLink { get; set; }
    }

    public class BulkResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "headers")]
        public dynamic Headers { get; set; }

        [JsonProperty(PropertyName = "body")]
        public GraphInvitation Body { get; set; }
    }

    public class BulkInviteRequest
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "guestRequestId")]
        public string GuestRequestId { get; set; }
        
        [JsonIgnore]
        public GuestRequest Request { get; set; }

        [JsonProperty(PropertyName = "method")]
        public string Method { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "headers")]
        public dynamic Headers { get; set; }

        [JsonProperty(PropertyName = "body")]
        public GraphInvitation Body { get; set; }
    }

    public class BatchQueueItem
    {
        [JsonProperty(PropertyName = "bulkInviteSubmissionId")]
        public string BulkInviteSubmissionId { get; set; }

        [JsonProperty(PropertyName = "invitingUserId")]
        public string InvitingUserId { get; set; }

        [JsonProperty(PropertyName = "userSourceHost")]
        public string UserSourceHostName { get; set; }

        [JsonProperty(PropertyName = "profileUrl")]
        public string ProfileUrl { get; set; }
    }
}
