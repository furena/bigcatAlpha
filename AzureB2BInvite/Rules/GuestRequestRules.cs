using AzureB2BInvite.Models;
using B2BPortal.Common.Enums;
using B2BPortal.Common.Helpers;
using B2BPortal.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;

namespace AzureB2BInvite.Rules
{
    public static class GuestRequestRules
    {
        public static async Task<GuestRequest> SignUpAsync(GuestRequest request, string profileUrl)
        {
            request.Init();
            var doc = await DocDBRepo.DB<GuestRequest>.CreateItemAsync(request);
            
            if (request.PreAuthed)
            {
                //check to see if this domain has been approved for pre-authentication
                var approvedDomainSettings = await GetMatchedDomain(request.EmailAddress);
                if (approvedDomainSettings != null && approvedDomainSettings.AutoApprove)
                {
                    request.Disposition = Disposition.AutoApproved;
                    request = await ExecuteDispositionAsync(request, approvedDomainSettings.AuthUser, profileUrl, approvedDomainSettings);
                }
            }

            return request;
        }
        public static async Task<PreAuthDomain> GetMatchedDomain(string email)
        {
            var d = email.Split('@');
            PreAuthDomain res = null;
            if (d.Length == 2)
            {
                var domain = d[1];
                res = await PreAuthDomain.GetDomainByName(domain);
            }
            return res;
        }

        public static async Task<GuestRequest> ExecuteDispositionAsync(GuestRequest request, string approver, string profileUrl, PreAuthDomain domainSettings = null)
        {
            request.AuthUser = approver;
            request.LastModDate = DateTime.UtcNow;
           
            if (request.Disposition == Disposition.Approved || request.Disposition == Disposition.AutoApproved)
            {
                //INVITE (request updated internally)
                var mgr = new InviteManager(profileUrl, null);
                request = await mgr.ProcessInvitationAsync(request, domainSettings);
            }
            else if (request.Disposition == Disposition.Denied)
            {
                //UPDATE GuestRequest to set denied status
                request = await UpdateAsync(request);
            }

            return request;
        }

        public static async Task<PreAuthDomain> GetPreauthDomain(string domainName)
        {
            return (await DocDBRepo.DB<PreAuthDomain>.GetItemsAsync(d => d.DomainName == domainName)).SingleOrDefault();
        }

        public static async Task<GuestRequest> GetUserAsync(string email)
        {
            return (await DocDBRepo.DB<GuestRequest>.GetItemsAsync(r => r.EmailAddress == email)).SingleOrDefault();
        }

        public static async Task<GuestRequest> UpdateAsync(GuestRequest Request)
        {
            return (await DocDBRepo.DB<GuestRequest>.UpdateItemAsync(Request));
        }

        public static async Task<dynamic> DeleteAsync(GuestRequest Request)
        {
            return (await DocDBRepo.DB<GuestRequest>.DeleteItemAsync(Request));
        }

        public static async Task<IEnumerable<GuestRequest>> GetBatchRequest(string submissionId)
        {
            return (await DocDBRepo.DB<GuestRequest>.GetItemsAsync(b => b.BatchProcessId == submissionId));
        }

        public static async Task<IEnumerable<GuestRequest>> GetPendingRequestsAsync()
        {
            return await DocDBRepo.DB<GuestRequest>.GetItemsAsync(r => r.Disposition == Disposition.Pending);
        }

        public static async Task<IEnumerable<GuestRequest>> GetHistory(HistoryFilter filter = null)
        {
            List<Expression<Func<GuestRequest, bool>>> parms = new List<Expression<Func<GuestRequest, bool>>>
            {
                q => q.Disposition != Disposition.Pending && q.Disposition != Disposition.QueuePending
            };

            if (filter != null)
            {
                if (filter.AuthUser != null)
                {
                    parms.Add(q => q.AuthUser == filter.AuthUser);
                }
                if (filter.RequestDateFrom != null)
                {
                    parms.Add(q => q.RequestDate >= filter.RequestDateFrom && q.RequestDate <= filter.RequestDateTo);
                }
            }

            return await DocDBRepo.DB<GuestRequest>.GetItemsAsync(parms.CombineAnd());
        }

        public static async Task<IEnumerable<BulkInviteResults>> GetBatchSubmissionHistory(string submissionId)
        {
            var res = await DocDBRepo.DB<BulkInviteResults>.GetItemsAsync(r => r.SubmissionId == submissionId);
            return res.OrderByDescending(r => r.ProcessingDate).ToList();
        }

        public class HistoryFilter
        {
            public string AuthUser { get; set; }
            public DateTime RequestDateFrom { get; set; }
            public DateTime RequestDateTo { get; set; }
        }
    }
}