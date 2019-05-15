using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using AzureB2BInvite.Models;
using Newtonsoft.Json;
using B2BPortal.Common.Utils;
using AzureB2BInvite;
using AzureB2BInvite.AuthCache;
using B2BPortal.Data;

namespace BatchInviteProcessor
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called invitations.
        public static void ProcessUserQueue([QueueTrigger("%queueName%")] BatchQueueItem batch, IBinder binder)
        {
            try
            {               
                var mgr = new InviteManager(batch);
                
                BulkInviteResults res;
                var task = Task.Run(async () => {
                    res = await mgr.BulkInvitations(batch.BulkInviteSubmissionId);
                });
                task.Wait();
            }
            catch (Exception ex)
            {
                Logging.WriteToAppLog(String.Format("Error processing queue '{0}'", StorageRepo.QueueName), System.Diagnostics.EventLogEntryType.Error, ex);
                throw;
            }
        }
        //this can be fleshed out with the addition of error triggering:
        //https://github.com/Azure/azure-webjobs-sdk-extensions#errortrigger
    }
}
