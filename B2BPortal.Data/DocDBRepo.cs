using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Net;
using B2BPortal.Common.Interfaces;
using B2BPortal.Common.Enums;

namespace B2BPortal.Data
{
    public static class DocDBRepo
    {
        private static DocumentClient client;
        private static Uri baseDocCollectionUri;

        public static class Settings
        {
            public static string DocDBUri;
            public static string DocDBAuthKey;
            public static string DocDBName;
            public static string DocDBCollection;
        }

        public static class DB<T> where T : class, IDocModelBase
        {
            public static async Task<T> CreateItemAsync(T item)
            {
                item.DocType = (DocTypes)Enum.Parse(typeof(DocTypes), typeof(T).Name);
                item.Id = Guid.NewGuid().ToString();
                await client.CreateDocumentAsync(baseDocCollectionUri, item);
                return item;
            }

            public static async Task<T> UpdateItemAsync(T item)
            {
                item.DocType = (DocTypes)Enum.Parse(typeof(DocTypes), typeof(T).Name);
                var id = item.Id;
                await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(Settings.DocDBName, Settings.DocDBCollection, id), item);
                return item;
            }

            public static async Task<dynamic> DeleteItemAsync(T item)
            {
                var id = item.Id;
                return await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(Settings.DocDBName, Settings.DocDBCollection, id));
            }

            public static async Task<T> GetItemAsync(string id)
            {
                try
                {
                    var docType = (DocTypes)Enum.Parse(typeof(DocTypes), typeof(T).Name);

                    Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(Settings.DocDBName, Settings.DocDBCollection, id));
                    return (T)(dynamic)document;
                }
                catch (DocumentClientException e)
                {
                    if (e.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            private static async Task<IEnumerable<T>> _getItemsAsync(IDocumentQuery<T> query)
            {
                List<T> results = new List<T>();
                try
                {
                    while (query.HasMoreResults)
                    {
                        results.AddRange(await query.ExecuteNextAsync<T>());
                    }

                    return results;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            /// <summary>
            /// Returns all documents of type T where (predicate)
            /// </summary>
            /// <param name="predicate"></param>
            /// <returns></returns>
            public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
            {
                if (predicate == null) return await GetItemsAsync();

                var docType = (DocTypes)Enum.Parse(typeof(DocTypes), typeof(T).Name);

                var cli = client.CreateDocumentQuery<T>(baseDocCollectionUri);
                var cli2 = cli.Where(predicate);

                Expression<Func<T, bool>> docTypeFilter = (q => q.DocType == docType);
                docTypeFilter.Compile();

                var query = cli2.Where(docTypeFilter)
                    .AsDocumentQuery();
                return await _getItemsAsync(query);
            }

            /// <summary>
            /// returns all documents of type T
            /// </summary>
            /// <returns></returns>
            public static async Task<IEnumerable<T>> GetItemsAsync()
            {
                var docType = (DocTypes)Enum.Parse(typeof(DocTypes), typeof(T).Name);
                Expression<Func<T, bool>> docTypeFilter = (q => q.DocType == docType);

                var query = client.CreateDocumentQuery<T>(baseDocCollectionUri)
                    .Where(docTypeFilter)
                    .AsDocumentQuery();
                return await _getItemsAsync(query);
            }
            public static async Task<IEnumerable<dynamic>> GetAllItemsGenericAsync()
            {
                var docType = (DocTypes)Enum.Parse(typeof(DocTypes), typeof(T).Name);

                var query = client.CreateDocumentQuery<IDocModelBase>(baseDocCollectionUri)
                    .Where(d => d.DocType == docType)
                    .AsDocumentQuery();

                List<dynamic> results = new List<dynamic>();
                try
                {
                    while (query.HasMoreResults)
                    {
                        results.AddRange(await query.ExecuteNextAsync<dynamic>());
                    }

                    return results;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static async Task<DocumentClient> Initialize()
        {
            baseDocCollectionUri = UriFactory.CreateDocumentCollectionUri(Settings.DocDBName, Settings.DocDBCollection);
            client = new DocumentClient(new Uri(Settings.DocDBUri), Settings.DocDBAuthKey);
            await CreateDatabaseIfNotExistsAsync();
            await CreateCollectionIfNotExistsAsync();
            return client;
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(Settings.DocDBName));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = Settings.DocDBName });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(baseDocCollectionUri);
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(Settings.DocDBName),
                        new DocumentCollection { Id = Settings.DocDBCollection },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
