using B2BPortal.Data;
using Microsoft.Azure.WebJobs;

namespace BatchInviteProcessor
{
    public class QueueNameResolver : INameResolver
    {
        public string Resolve(string name)
        {
            return StorageRepo.QueueName;
        }
    }
}
