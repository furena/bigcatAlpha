using AzureB2BInvite;
using AzureB2BInvite.Models;
using B2BPortal.Infrastructure;
using B2BPortal.Infrastructure.Filters;
using B2BPortal.Common.Helpers;
using B2BPortal.Common.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Westwind.Web.Utilities;
using B2BPortal.Data.Models;
using B2BPortal.Common.Utils;
using B2BPortal.Data;
using System.Security.Claims;
using AzureB2BInvite.Rules;
using static AzureB2BInvite.Rules.GuestRequestRules;

namespace B2BPortal.api
{
    [AuthorizedInviter]
    public class AdminController : ApiController
    {
        [HttpGet]
        public TemplateSettings GetStaticDefaultTemplate()
        {
            return new TemplateSettings
            {
                SubjectTemplate = Settings.CurrSiteConfig.InviteTemplateContent.SubjectTemplate,
                BodyTemplate = Settings.CurrSiteConfig.InviteTemplateContent.TemplateContent
            };
        }
        public async Task<IEnumerable<GuestRequest>> GetHistory([FromUri]HistoryFilter filter)
        {
            IEnumerable<GuestRequest> history = (await GuestRequestRules.GetHistory(filter)).OrderByDescending(h => h.RequestDate);
            return history;
        }

        [HttpGet]
        public async Task<IEnumerable<BulkInviteSubmission>> GetBatchPending()
        {
            return (await BulkInviteSubmission.GetItemsPending()).OrderByDescending(c => c.SubmissionDate);
        }

        [HttpGet]
        public async Task<IEnumerable<BulkInviteSubmission>> GetBatchHistory(int daysHistory = 20)
        {
            IEnumerable<BulkInviteSubmission> res = null;
            try
            {
                res = (await BulkInviteSubmission.GetItemHistory(daysHistory)).OrderByDescending(c => c.SubmissionDate);
            }
            catch (Exception)
            {
                //see if this domain needs the group object updated
                res = await BulkInviteSubmission.RefreshAllPreAuthGroupData(daysHistory);
            }

            return res;
        }
        public async Task<IEnumerable<BulkInviteResults>> GetBatchProcessingHistory(string id)
        {
            IEnumerable<BulkInviteResults> history = (await GuestRequestRules.GetBatchSubmissionHistory(id));
            return history;
        }

        [HttpGet]
        public async Task<BatchDTO> GetBatchItem(string id)
        {
            return new BatchDTO
            {
                Submission = await BulkInviteSubmission.GetItem(id),
                BatchResults = await BulkInviteResults.GetItems(id)
            };
        }

        [HttpPost]
        public HttpResponseMessage RequeueRequest(dynamic data)
        {
            string signInUserId = User.Identity.GetClaim(Settings.ObjectIdentifier);

            //queue the request for processing
            var queue = new BatchQueueItem
            {
                BulkInviteSubmissionId = data.id,
                InvitingUserId = signInUserId,
                ProfileUrl = Utils.GetProfileUrl(Request.RequestUri),
                UserSourceHostName = Utils.GetFQDN(Request)
            };

            StorageRepo.AddQueueItem(queue);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
        
        [HttpPost]
        public async Task<HttpResponseMessage> KillQueuedRequest(dynamic data)
        {
            var id = data.id.ToString();
            await BulkInviteSubmission.KillBatch(id);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
        
        [HttpPost]
        public async Task<HttpResponseMessage> DeleteQueuedRequest(dynamic data)
        {
            var id = data.id.ToString();
            await BulkInviteSubmission.DeleteBatch(id);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        [HttpPost]
        public async Task<BulkInviteSubmission> SendBulkInvitations(BulkInviteSubmission bulkRequest)
        {
            if (!User.Identity.IsInRole(Roles.CompanyAdministrator))
            {
                //enforcing rule: only Company Administrator can invite Members
                bulkRequest.MemberType = MemberType.Guest;
            }
            
            bulkRequest.InvitationMessage = HtmlSanitizer.SanitizeHtml(bulkRequest.InvitationMessage);
            bulkRequest.SubmissionDate = DateTime.UtcNow;

            //this call adds the batch to the DB and creates a pending item for each guest
            bulkRequest = await BulkInviteSubmission.AddItem(bulkRequest, User.Identity.Name);

            string userOid = User.Identity.GetClaim(Settings.ObjectIdentifier);

            //queue the request for processing
            var queue = new BatchQueueItem
            {
                BulkInviteSubmissionId = bulkRequest.Id,
                InvitingUserId = userOid,
                ProfileUrl = Utils.GetProfileUrl(Request.RequestUri),
                UserSourceHostName = Utils.GetFQDN(Request)
            };

            StorageRepo.AddQueueItem(queue);
            
            return bulkRequest;
        }
        
        [HttpGet]
        public async Task<IEnumerable<BulkInviteSubmission>> GetBulkProcessingStatus()
        {
            var batches = (await BulkInviteSubmission.GetItemsPending()).OrderByDescending(c => c.SubmissionDate);
            return batches;
        }

        [HttpGet]
        public async Task<StatusDTO> GetBulkItemStatus()
        {
            BulkResponse response = null;
            var submissionId = Request.GetQueryNameValuePairs().Single(s => s.Key == "submissionId").Value;
            var email = Request.GetQueryNameValuePairs().Single(s => s.Key == "email").Value;

            var request = await BulkInviteSubmission.GetGuestItemDetail(submissionId, email);
            var result = (await DocDBRepo.DB<BulkInviteResults>.GetItemsAsync(d => d.SubmissionId == submissionId)).LastOrDefault();
            if (result != null)
            {
                response = result.InvitationResults.Responses.SingleOrDefault(r => r.Body.InvitedUserEmailAddress == email);
            }

            return new StatusDTO
            {
                Request = request,
                Response = response
            };
        }
    }

    public class TemplateSettings
    {
        public string SubjectTemplate { get; set; }
        public string BodyTemplate { get; set; }
    }
    public class StatusDTO
    {
        public GuestRequest Request { get; set; }
        public BulkResponse Response { get; set; }
    }
    public class BatchDTO
    {
        public BulkInviteSubmission Submission { get; set; }
        public IEnumerable<BulkInviteResults> BatchResults { get; set; }
    }
}
