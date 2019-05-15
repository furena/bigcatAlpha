using AzureB2BInvite.AuthCache;
using AzureB2BInvite.Models;
using AzureB2BInvite.Rules;
using AzureB2BInvite.Utils;
using B2BPortal.Common.Enums;
using B2BPortal.Common.Models;
using B2BPortal.Common.Utils;
using B2BPortal.Data;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureB2BInvite
{
    /// <summary>
    /// https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/invitation_post
    /// </summary>
    public class InviteManager
    {
        private CacheUser _user;
        private string _profileUrl;
        private readonly string _accessToken;

        /// <summary>
        /// If a CacheUser is passed in during initialization, invitations will be sent in the context of that user. 
        /// If that user is a guest, and has inviter priviliges, invitations sent to others in that guest's tenant
        /// will be automatically added as guests, pre-verified.
        /// If the UserID is left null, invitations will be sent in the context of the Service Principal
        /// </summary>
        /// <param name="profileUrl"></param>
        /// <param name="userId"></param>
        public InviteManager(string profileUrl, CacheUser user)
        {
            _user = user;
            _profileUrl = profileUrl;
        }
        public InviteManager(BatchQueueItem batch)
        {
            _user = new CacheUser(batch.InvitingUserId, batch.UserSourceHostName);
            _profileUrl = batch.ProfileUrl;
            AuthenticationResult res = null;
            var task = Task.Run(async () => {
                res = await AdalUtil.AuthenticateApp(null, _user);
            });
            task.Wait();
            _accessToken = res.AccessToken;

        }

        public async Task<BulkInviteResults> BulkInvitations(string BulkInviteSubmissionId)
        {
            var res = new BulkInviteResults(BulkInviteSubmissionId);

            try
            {
                var batch = await BulkInviteSubmission.GetItem(BulkInviteSubmissionId);
                if (batch == null)
                {
                    return new BulkInviteResults
                    {
                        ErrorMessage = "Batch not found."
                    };
                }
                if (batch.StopProcessing)
                {
                    return new BulkInviteResults
                    {
                        ErrorMessage = "Batch was flagged to discontinue processing."
                    };
                }
                res = await ProcessBulkInvitations(batch);
                return res;
            }
            catch (Exception ex)
            {
                var msg = "Error processing bulk invitation";
                res.ErrorMessage = Logging.WriteToAppLog(msg, System.Diagnostics.EventLogEntryType.Error, ex);
                await BulkInviteResults.AddItem(res);
                return res;
            }
        }

        public async Task<BulkInviteResults> ProcessBulkInvitations(BulkInviteSubmission submission)
        {
            var res = new BulkInviteResults(submission.Id);

            try
            {
                var batch = new GraphBatch();
                int counter = 0;
                var inviteEndPoint = string.Format("/{0}/invitations", Settings.GraphApiVersion);
                var headerColl = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                };

                var items = await BulkInviteSubmission.GetGuestRequestsPending(submission.Id);
                foreach (var item in items)
                {
                    counter++;
                    // Setup invitation
                    GraphInvitation invitation = new GraphInvitation()
                    {
                        InvitedUserDisplayName = item.EmailAddress,
                        InvitedUserEmailAddress = item.EmailAddress,
                        InviteRedirectUrl = _profileUrl,
                        SendInvitationMessage = (!Settings.UseSMTP),
                        InvitedUserType = submission.MemberType.ToString()
                    };
                    
                    if (submission.InvitationMessage.Length > 0)
                    {
                        invitation.InvitedUserMessageInfo = new InvitedUserMessageInfo
                        {
                            CustomizedMessageBody = submission.InvitationMessage
                        };
                    }

                    batch.Requests.Add(new BulkInviteRequest
                    {
                        Id = counter.ToString(),
                        GuestRequestId = item.Id,
                        Request = item,
                        Method = "POST",
                        Headers = headerColl,
                        Url = inviteEndPoint,
                        Body = invitation
                    });
                }

                /* NOTE:
                 * This process is designed to leverage the Microsoft Graph batch processor:
                 *     https://developer.microsoft.com/en-us/graph/docs/concepts/json_batching
                 * However, the batch processor is currently (2018) in preview and limited to 20 submissions per request
                 * For the time being, we'll loop the collection and make individual synchronous calls
                */
                //res = SubmitToGraphBatch(batch, submission, userId);

                res = await SubmitLocally(batch, submission);


                return res;
            }
            catch (Exception ex)
            {
                var msg = "Error processing bulk invitation";
                res.ErrorMessage = Logging.WriteToAppLog(msg, System.Diagnostics.EventLogEntryType.Error, ex);
                await BulkInviteResults.AddItem(res);
                return res;
            }
        }

        //private BulkInviteResults SubmitToGraphBatch(GraphBatch batch, BulkInviteSubmission submission)
        //{
        //    //** NOTE: this method was last tested against the beta API, circa Q3 CY17 **
        //    var res = new BulkInviteResults();
        //    var batchEndPoint = string.Format("{0}/beta/$batch", Settings.GraphResource);
        //    var serverResponse = CallGraph(batchEndPoint, batch, false, null, _userId);

        //    if (serverResponse.Successful)
        //    {
        //        res.InvitationResults = JsonConvert.DeserializeObject<GraphBatchResponse>(serverResponse.ResponseContent);
        //        if (submission.GroupList.Length > 0)
        //        {
        //            foreach (var item in res.InvitationResults.Responses)
        //            {
        //                var groupsAdded = AddUserToGroup(item.Body.InvitedUser.Id, submission.GroupList.ToList());
        //                if (!groupsAdded.Success)
        //                {
        //                    var resErrors = string.Join(", ", groupsAdded.Responses.Where(r => !r.Successful).Select(r => r.Message));
        //                    res.ErrorMessage += string.Format("\n\rOne or more groups failed while assigning to user \"{0}\" ({1})", item.Body.InvitedUserEmailAddress, resErrors);
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        res.ErrorMessage = serverResponse.Message;
        //    }
        //    return res;
        //}

        private async Task<BulkInviteResults> SubmitLocally(GraphBatch batch, BulkInviteSubmission submission)
        {
            var res = new BulkInviteResults(submission.Id);

            GuestRequest itemRes;
            var mailTemplate = await TemplateUtilities.GetTemplate(submission.InviteTemplateId);

            try
            {
                foreach (var item in batch.Requests)
                {
                    itemRes = await SendGraphInvitationAsync(item.Body, submission.GroupList.ToList(), item.Request, null, mailTemplate, _accessToken);
                    if (itemRes.Status != "Error")
                    {
                        submission.ItemsProcessed += 1;
                        submission = await BulkInviteSubmission.UpdateItem(submission);
                    }

                    //update batch results
                    res.InvitationResults.Responses.Add(new BulkResponse
                    {
                        Status = itemRes.Status,
                        Body = itemRes.InvitationResult,
                        Id = itemRes.InvitationResult.id
                    });
                }

                await BulkInviteResults.AddItem(res);
                return res;
            }
            catch (Exception ex)
            {
                res.ErrorMessage = ex.Message;
                await BulkInviteResults.AddItem(res);
                return res;
            }
        }

        public async Task<GuestRequest> ProcessInvitationAsync(GuestRequest request, PreAuthDomain domainSettings = null)
        {
            var displayName = string.Format("{0} {1}", request.FirstName, request.LastName);

            var useCustomEmailTemplate = false;
            var redemptionSettings = Settings.CurrSiteConfig.SiteRedemptionSettings;
            var inviteTemplate = Settings.CurrSiteConfig.InviteTemplateContent;

            var memberType = MemberType.Guest;

            //use domain custom setting if exists, else use global site config setting
            if (domainSettings != null)
            {
                redemptionSettings = domainSettings.DomainRedemptionSettings;
                inviteTemplate = domainSettings.InviteTemplateContent;
                memberType = domainSettings.MemberType;

                if (!string.IsNullOrEmpty(domainSettings.InviteTemplateId))
                {
                    useCustomEmailTemplate = true;
                }
            }

            try
            {
                // Setup invitation
                GraphInvitation invitation = new GraphInvitation()
                {
                    InvitedUserDisplayName = displayName,
                    InvitedUserEmailAddress = request.EmailAddress,
                    InviteRedirectUrl = _profileUrl,
                    SendInvitationMessage = (!Settings.UseSMTP),
                    InvitedUserType = memberType.ToString()
                };
                if (useCustomEmailTemplate && invitation.SendInvitationMessage && domainSettings.InviteTemplateContent.TemplateContent != null)
                {
                    invitation.InvitedUserMessageInfo = new InvitedUserMessageInfo
                    {
                        CustomizedMessageBody = domainSettings.InviteTemplateContent.TemplateContent
                    };
                }
                var groups = new List<GroupObject>();

                if (domainSettings != null)
                {
                    if (domainSettings.Groups != null && domainSettings.Groups.Count > 0)
                    {
                        groups = domainSettings.Groups;
                    }
                }

                return await SendGraphInvitationAsync(invitation, groups, request, redemptionSettings.InviterResponseEmailAddr, inviteTemplate);
            }
            catch (Exception ex)
            {
                Logging.WriteToAppLog("", EventLogEntryType.Error, ex);
                request.InvitationResult = new GraphInvitation
                {
                    ErrorInfo = string.Format("Error: {0}", ex.Message),
                    Status = "Error"
                };
                return request;
            }
        }
        /// <summary>
        /// GraphInvitation is a model from https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/invitation
        /// Do not alter
        /// </summary>
        /// <param name="invitation">The GraphInvitation object transmitted to the B2B Guest API</param>
        /// <param name="groups">Azure AD groups to assign the guest user to</param>
        /// <param name="inviterResponseEmailAddr"></param>
        /// <param name="mailTemplate"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        private async Task<GuestRequest> SendGraphInvitationAsync(GraphInvitation invitation, List<GroupObject> groups, GuestRequest request, string inviterResponseEmailAddr = null, InviteTemplate mailTemplate = null, string accessToken = null)
        {
            AdalResponse serverResponse = null;
            GraphInvitation responseData = new GraphInvitation();

            try
            {
                var inviteEndPoint = string.Format("{0}/{1}/invitations", Settings.GraphResource, Settings.GraphApiVersion);

                // Invite user. Your app needs to have User.ReadWrite.All or Directory.ReadWrite.All to invite
                serverResponse = AdalUtil.CallGraph(inviteEndPoint, invitation, false, null, _user, accessToken);

                responseData = JsonConvert.DeserializeObject<GraphInvitation>(serverResponse.ResponseContent);
                request.InvitationResult = responseData;
                request.Status = responseData.Status;

                if (responseData.id == null)
                {
                    //API call failed - put the request back in the queue
                    responseData.ErrorInfo = string.Format("Error: Invite not sent - API error: {0}", serverResponse.Message);
                    request.InvitationResult = responseData;
                    request.Disposition = Disposition.Pending;
                }
                else
                {
                    //invitation API call successful
                    if (responseData.Status.Substring(0, 5) == "Error")
                    {
                        //API call was successful but something failed while trying to process the request - put the request back in the queue
                        request.Disposition = Disposition.Pending;
                    }
                    else
                    {
                        //check to see if groups should be assigned
                        if (groups.Count > 0)
                        {
                            try
                            {
                                var groupsAdded = AddUserToGroup(responseData.InvitedUser.Id, groups);
                                if (!groupsAdded.Success)
                                {
                                    //group assignment failed
                                    var resErrors = string.Join(", ", groupsAdded.Responses.Where(r => !r.Successful).Select(r => r.Message));
                                    var msg = string.Format("\n\rOne or more groups failed while assigning to user \"{0}\" ({1})", responseData.InvitedUserEmailAddress, resErrors);
                                    request.PostProcessingStatus += msg;
                                }
                            }
                            catch (Exception groupEx)
                            {
                                //group assignment errored
                                var msg = string.Format("\n\rAn error occured while assigning a group to user \"{0}\" ({1})", responseData.InvitedUserEmailAddress, groupEx.Message);
                                Logging.WriteToAppLog(msg, EventLogEntryType.Warning, groupEx);
                                request.PostProcessingStatus += msg;
                            }
                        }

                        //check to see if custom mail should be sent
                        if (Settings.UseSMTP)
                        {
                            try
                            {
                                mailTemplate = mailTemplate ?? Settings.CurrSiteConfig.InviteTemplateContent;

                                var emailSubject = mailTemplate.SubjectTemplate.Replace("{{InvitingOrgName}}", Settings.CurrSiteConfig.InvitingOrg);

                                string body = FormatEmailBody(responseData, inviterResponseEmailAddr, mailTemplate);
                                SendViaSMTP(emailSubject, body, invitation.InvitedUserEmailAddress);
                                request.MailSent = true;
                            }
                            catch (Exception mailEx)
                            {
                                var msg = string.Format("\n\rSending invitation failed for user \"{0}\" ({1})", responseData.InvitedUserEmailAddress, mailEx.Message);
                                Logging.WriteToAppLog(msg, EventLogEntryType.Warning, mailEx);
                                request.PostProcessingStatus += msg;
                            }
                        }
                    }
                }

                //all attempts complete, UPDATE GuestRequest
                await GuestRequestRules.UpdateAsync(request);
                return request;
            }
            catch (Exception ex)
            {
                var reason = (serverResponse == null ? "N/A" : serverResponse.ResponseContent);
                responseData.ErrorInfo = string.Format("Error: {0}<br>Server response: {1}", ex.Message, reason);
                request.InvitationResult = responseData;

                //UPDATE GuestRequest
                await GuestRequestRules.UpdateAsync(request);
                return request;
            }
        }

        private static GroupAddResult AddUserToGroup(string userId, List<GroupObject> groups)
        {
            var res = new GroupAddResult()
            {
                Success = true
            };
            var body = new GraphGroupAdd(userId);
            AdalResponse serverResponse = null;

            foreach (GroupObject group in groups)
            {
                var uri = string.Format("https://graph.microsoft.com/v1.0/groups/{0}/members/$ref", group.GroupId);
                serverResponse = AdalUtil.CallGraph(uri, body);
                res.Responses.Add(serverResponse);
                if (!serverResponse.Successful)
                {
                    res.Success = false;
                }
            }
            return res;
        }
        private class GroupAddResult
        {
            public bool Success { get; set; }
            public List<AdalResponse> Responses { get; set; }
            public GroupAddResult()
            {
                Responses = new List<AdalResponse>();
            }
        }

        private static string FormatEmailBody(GraphInvitation invitation, string inviterResponseEmailAddr, InviteTemplate mailTemplate)
        {
            var body = mailTemplate.TemplateContent;
            body = body.Replace("{{InvitingOrgName}}", Settings.CurrSiteConfig.InvitingOrg);
            body = body.Replace("{{InvitationLink}}", invitation.InviteRedeemUrl);
            body = body.Replace("{{InvitationStatus}}", invitation.Status);
            if (invitation.InvitedUserMessageInfo != null)
            {
                body = body.Replace("{{CustomMessage}}", invitation.InvitedUserMessageInfo.CustomizedMessageBody);
            }
            body = body.Replace("{{OrgContactEmail}}", inviterResponseEmailAddr);
            return body;
        }

        private static void SendViaSMTP(string subject, string mailBody, string email)
        {
            MailSender.SendMessage(email, subject, mailBody);
        }

        public static RoleResponse GetDirectoryRoles(string upn)
        {
            var res = new RoleResponse();

            AdalResponse serverResponse = null;
            var rolesUri = string.Format("{0}/{1}/users/{2}/memberOf", Settings.GraphResource, Settings.GraphApiVersion, upn);
            serverResponse = AdalUtil.CallGraph(rolesUri);
            res.Successful = serverResponse.Successful;
            res.ErrorMessage = serverResponse.Message;

            var list = new List<GraphMemberRole>();
            if (serverResponse.Successful)
            {
                JObject data = JObject.Parse(serverResponse.ResponseContent);
                IList<JToken> roles = data["value"].ToList();
                foreach (var role in roles)
                {
                    var item = JsonConvert.DeserializeObject<GraphMemberRole>(role.ToString());
                    list.Add(item);
                }
            }

            res.Roles = list;
            return res;
        }
    }

    public class RoleResponse
    {
        public IEnumerable<GraphMemberRole> Roles { get; set; }
        public bool Successful { get; set; }
        public string ErrorMessage { get; set; }
    }
}