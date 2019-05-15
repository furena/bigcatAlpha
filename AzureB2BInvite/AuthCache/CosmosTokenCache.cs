using B2BPortal.Data;
using B2BPortal.Common.Interfaces;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using B2BPortal.Data.Models;
using System.Web.Security;
using B2BPortal.Common.Utils;
using Encryption;
using System.Security.Cryptography;

namespace AzureB2BInvite.AuthCache
{
    public class CacheUser
    {
        public string UserObjId { get; set; }
        public string HostName { get; set; }
        public CacheUser(string userObjId, string hostName)
        {
            UserObjId = userObjId;
            HostName = hostName;
        }
    }

    public class PerWebUserCache : DocModelBase, IDocModelBase
    {
        public string WebUserUniqueId { get; set; }
        public byte[] CacheBits { get; set; }
        public DateTime LastWrite { get; set; }
        public string HostName { get; set; }
        public byte[] Salt { get; set; }

        public static async Task<PerWebUserCache> GetCache(CacheUser user)
        {
            return (await DocDBRepo.DB<PerWebUserCache>.GetItemsAsync(u => u.WebUserUniqueId==user.UserObjId && u.HostName == user.HostName)).SingleOrDefault();
        }
        public static async Task<PerWebUserCache> AddEntry(PerWebUserCache cache)
        {
            return (await DocDBRepo.DB<PerWebUserCache>.CreateItemAsync(cache));
        }
        public static async Task<PerWebUserCache> UpdateEntry(PerWebUserCache cache)
        {
            return (await DocDBRepo.DB<PerWebUserCache>.UpdateItemAsync(cache));
        }
        public static async Task<IEnumerable<PerWebUserCache>> GetAllEntries()
        {
            return (await DocDBRepo.DB<PerWebUserCache>.GetItemsAsync());
        }
        public static async Task<IEnumerable<PerWebUserCache>> GetAllEntries(CacheUser user)
        {
            return (await DocDBRepo.DB<PerWebUserCache>.GetItemsAsync(u => u.WebUserUniqueId == user.UserObjId && u.HostName == user.HostName));
        }
        public static async Task RemoveEntry(PerWebUserCache cache)
        {
            await DocDBRepo.DB<PerWebUserCache>.DeleteItemAsync(cache);
        }
    }

    public class AdalCosmosTokenCache : TokenCache
    {
        private string _userObjId;
        private string _hostName;
        private PerWebUserCache Cache;

        public AdalCosmosTokenCache(CacheUser user): this(user.UserObjId, user.HostName)
        {
        }

        // constructor
        public AdalCosmosTokenCache(string userObjId, string hostName)
        {
            // associate the cache to the current user of the web app
            _userObjId = userObjId;
            _hostName = hostName;

            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            this.BeforeWrite = BeforeWriteNotification;

            // look up the entry in the DB
            var task = Task.Run(async () => {
                Cache = await PerWebUserCache.GetCache(new CacheUser(_userObjId, _hostName));
            });
            task.Wait();

            try
            {
                // place the entry in memory
                this.Deserialize((Cache == null) ? null : B2BPortal.Common.Utils.Utils.Decrypt(new EncryptedObj(Cache.CacheBits, Cache.Salt)));
            }
            catch(CryptographicException)
            {
                //error decrypting from token cache - clearing the cached item (encryption key may have changed)
                task = Task.Run(async () => {
                    await PerWebUserCache.RemoveEntry(Cache);
                });
                task.Wait();
                this.Deserialize(null);
            }
            catch (Exception ex)
            {
                var newEx = new Exception("Error decrypting the cached token. ", ex);
                throw newEx;
            }
        }

        // clean up the DB
        public override void Clear()
        {
            base.Clear();
            IEnumerable<PerWebUserCache> entries = null;
            var task = Task.Run(async () => {
                entries = await PerWebUserCache.GetAllEntries();
            });
            task.Wait();

            foreach (var cacheEntry in entries)
                PerWebUserCache.RemoveEntry(cacheEntry).Wait();
        }

        // Notification raised before ADAL accesses the cache.
        // This is your chance to update the in-memory copy from the DB, if the in-memory version is stale
        async void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            if (Cache == null)
            {
                // first time access
                Cache = await PerWebUserCache.GetCache(new CacheUser(_userObjId, _hostName));
            }
            else
            {   
                // retrieve last write from the DB
                var dbCache = await PerWebUserCache.GetCache(new CacheUser(_userObjId, _hostName));

                // if the in-memory copy is older than the persistent copy
                if (dbCache.LastWrite > Cache.LastWrite)
                {
                    // update in-memory copy
                    Cache = dbCache;
                }
            }
            this.Deserialize((Cache == null) ? null : B2BPortal.Common.Utils.Utils.Decrypt(new EncryptedObj(Cache.CacheBits, Cache.Salt)));
        }
        // Notification raised after ADAL accessed the cache.
        // If the HasStateChanged flag is set, ADAL changed the content of the cache
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            Task task;
            // if state changed
            if (this.HasStateChanged)
            {
                var enc = B2BPortal.Common.Utils.Utils.Encrypt(this.Serialize());

                if (Cache != null)
                {
                    Cache.CacheBits = enc.EncryptedData;
                    Cache.Salt = enc.VectorData;
                    Cache.LastWrite = DateTime.Now;
                    // update the DB and the lastwrite             
                    task = Task.Run(async () => {
                        await PerWebUserCache.UpdateEntry(Cache);
                    });
                    task.Wait();
                }
                else
                {
                    Cache = new PerWebUserCache
                    {
                        WebUserUniqueId = _userObjId,
                        CacheBits = enc.EncryptedData,
                        Salt = enc.VectorData,
                        LastWrite = DateTime.Now,
                        HostName = _hostName
                    };
                    // add the entry             
                    task = Task.Run(async () => {
                        await PerWebUserCache.AddEntry(Cache);
                    });
                    task.Wait();
                }

                this.HasStateChanged = false;
            }
        }
        void BeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            // if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
        }

        public override void DeleteItem(TokenCacheItem item)
        {
            base.DeleteItem(item);
        }
    }
}