using AzureB2BInvite.AuthCache;
using B2BPortal.Common.Models;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AzureB2BInvite
{
    public class GraphUtil
    {
        private GraphServiceClient _client;
        private CacheUser _user;

        public GraphServiceClient Client {
            get {
                return _client;            
            }
        }

        public GraphUtil(CacheUser user = null)
        {
            _user = user;
            AuthenticationResult authResult=null;
            // Get auth token
            var task = Task.Run(async () => {
                authResult = await AdalUtil.AuthenticateApp(null, user);
            });
            task.Wait();

            string accessToken = authResult.AccessToken;

            _client = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                        return Task.FromResult(0);
                    }));
        }

        public async Task<User> GetUser(string upn)
        {
           return await (_client.Users[upn]).Request().GetAsync();
        }

        public async Task<IEnumerable<GroupObject>> GetGroups()
        {
            var res = await (_client.Groups).Request().OrderBy("displayName").GetAsync();
            IEnumerable<GroupObject> groups = res.Select(g => new GroupObject(g.DisplayName, g.Id)).ToList();
            return groups;
        }

        public async Task<IEnumerable<GroupObject>> GetGroups(string filter)
        {
            if (filter == null)
            {
                return await GetGroups();
            }
            var s = string.Format("startswith(displayName,'{0}')", filter);
            var res = await (_client.Groups).Request().Filter(s).GetAsync();
            IEnumerable<GroupObject> groups = res.Select(g => new GroupObject(g.DisplayName, g.Id)).ToList();
            return groups;
        }

        public async Task<User> SetUser(User user)
        {
            try
            {
                var u = _client.Users[user.UserPrincipalName].Request();

                user = await u.UpdateAsync(user);

                return user;
            }
            catch (Exception ex)
            {
                throw new Exception("Problem updating user", ex);
            }
        }
    }
}
