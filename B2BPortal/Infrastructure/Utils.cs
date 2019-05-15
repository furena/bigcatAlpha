using B2BPortal.Models;
using Microsoft.Owin.Security.DataProtection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace B2BPortal.Infrastructure
{
    public static class Utils
    {
        public static IDataProtector OwinDPP { get; set; }

        public static string ApplicationName { get; set; }
        public static string CrossServerKey { get; set; }

        /// <summary>
        /// Retrieve a strongly-typed claim lookup result from a user's ClaimsIdentity
        /// </summary>
        /// <typeparam name="T">the type to return (coercing the claim string)</typeparam>
        /// <param name="claimsIdentity">the user's ClaimsIdentity object</param>
        /// <param name="claimName">string name of the claim to retrieve</param>
        /// <returns>the claim value</returns>
        public static T GetClaim<T>(ClaimsIdentity claimsIdentity, string claimName)
        {
            if (claimsIdentity == null)
                return default(T);

            try
            {
                var res = claimsIdentity.Claims.Where(cx => cx.Type == claimName).Select(c => c.Value).SingleOrDefault();
                return (res == null) ? default(T) : (T)Convert.ChangeType(res, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }
        public static T GetClaim<T>(string claimName)
        {
            ClaimsPrincipal claimsPrincipal = HttpContext.Current.User as ClaimsPrincipal;

            if (claimsPrincipal == null)
                return default(T);

            try
            {
                var claimsIdentity = (ClaimsIdentity)claimsPrincipal.Identity;
                var res = claimsIdentity.Claims.Where(cx => cx.Type == claimName).Select(c => c.Value).SingleOrDefault();
                return (T)Convert.ChangeType(res, typeof(T));
            }
            catch
            {
                return default(T);
            }

        }
        public static string GetClaim(string claimName)
        {
            return GetClaim<string>(claimName);
        }

        public static string GetDomain()
        {
            return ConfigurationManager.AppSettings["Domain"];
        }

        /// <summary>
        /// Retrieve a strongly-typed claim lookup result from a user's ClaimsPrincipal
        /// </summary>
        /// <typeparam name="T">the type to return (coercing the claim string)</typeparam>
        /// <param name="claimsPrincipal">the user's ClaimsPrincipal object</param>
        /// <param name="claimName">string name of the claim to retrieve</param>
        /// <returns>the claim value</returns>
        public static T GetClaim<T>(ClaimsPrincipal claimsPrincipal, string claimName)
        {
            if (claimsPrincipal == null)
                return default(T);

            try
            {
                var claimsIdentity = (ClaimsIdentity)claimsPrincipal.Identity;
                return GetClaim<T>(claimsIdentity, claimName);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Retrieve a claim lookup result from a user's ClaimsPrincipal
        /// </summary>
        /// <param name="claimsPrincipal">the user's ClaimsPrincipal object</param>
        /// <param name="claimName">string name of the claim to retrieve</param>
        /// <returns>the claim value string</returns>
        public static string GetClaim(ClaimsPrincipal claimsPrincipal, string claimName)
        {
            return GetClaim<string>(claimsPrincipal, claimName);
        }

        /// <summary>
        /// Retrieve a claim lookup result from a user's ClaimsIdentity
        /// </summary>
        /// <param name="claimsIdentity">the user's ClaimsIdentity</param>
        /// <param name="claimName">string name of the claim to retrieve</param>
        /// <returns>the claim value string</returns>
        public static string GetClaim(ClaimsIdentity claimsIdentity, string claimName)
        {
            return GetClaim<string>(claimsIdentity, claimName);
        }

        /// <summary>
        /// Return the collection of the user's current system roles
        /// </summary>
        /// <param name="claimsPrincipal">the user's ClaimsPrincipal object</param>
        /// <returns>collection of strings</returns>
        public static IEnumerable<String> GetRoles(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null)
                throw new NullReferenceException("claimsPrinciple is null");

            return claimsPrincipal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(n => n.Value);
        }

        /// <summary>
        /// Return the collection of the user's current assigned Group memberships
        /// </summary>
        /// <param name="claimsPrincipal">the user's ClaimsPrincipal object</param>
        /// <returns>collection of strings</returns>
        public static IEnumerable<GroupObject> GetGroups(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null)
                throw new NullReferenceException("claimsPrinciple is null");

            var groups = claimsPrincipal.Claims.Where(x => x.Type == CustomClaimTypes.MemberOfGroup).Select(n => n.Value);
            var res = groups.Select(g => JsonConvert.DeserializeObject<GroupObject>(g)).OrderBy(g => g.GroupName).ToList();
            return res;
        }

        /// <summary>
        /// Insert spaces before camel-cased words in a token, i.e., "ThisIsAString" to "This Is A String"
        /// </summary>
        /// <param name="s">the token</param>
        /// <returns>string</returns>
        public static string SplitCamelCase(string s)
        {
            return Regex.Replace(s, "([a-z](?=[A-Z0-9])|[A-Z](?=[A-Z][a-z]))", "$1 ");
        }

        public static string GetProfileUrl(Uri requestUri)
        {
            var port = (requestUri.Port == 443 || requestUri.Port == 80) ? "" : ":" + requestUri.Port.ToString();
            return string.Format("{0}://{1}{2}/profile", requestUri.Scheme, requestUri.Host, port);
        }

        public static string GetTempPassword()
        {
            var g = Guid.NewGuid().ToString();
            g = g.Replace("-", "");
            return g;
        }
        public static string Pluralize(int count, string word)
        {
            return (count == 1) ? word : string.Format("{0}s", word); 
        }

        public static string GetHtmlMessageWrapper(string title, string body)
        {
            var res = new StringBuilder("<html><head>");
            res.AppendFormat("<title>{0}</title>", title);
            res.AppendLine("<style type='text/css'>");
            res.AppendLine("    p, blockquote, div, span {font:11pt calibri, arial}");
            res.AppendLine("</style></head>");
            res.AppendLine("<body><div>");
            res.Append(body);
            res.AppendLine("</div></body></html>");
            return res.ToString();
        }

        public static bool ConfirmLocalFileUnlocked(string filePath)
        {
            try
            {
                var currAttributes = File.GetAttributes(filePath);
                if ((currAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    var newAttributes = RemoveAttribute(currAttributes, FileAttributes.ReadOnly);
                    File.SetAttributes(filePath, newAttributes);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logging.WriteToAppLog("Unable to set local file properties to read-write", EventLogEntryType.Error, ex);
                return false;
            }
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        public static string EncryptString(string stringToEncrypt)
        {
            var encoded = Encoding.UTF8.GetBytes(stringToEncrypt);
            var res = OwinDPP.Protect(encoded);
            return Encoding.UTF8.GetString(res);
        }

        public static string DecryptString(string stringToDecrypt)
        {
            var decoded = Encoding.UTF8.GetBytes(stringToDecrypt);
            var res = OwinDPP.Unprotect(decoded);
            return Encoding.UTF8.GetString(res);
        }
    }

    public static class Dynamic
    {
        public static bool HasMember(object dynObj, string memberName)
        {
            return GetMemberNames(dynObj).Contains(memberName);
        }

        public static IEnumerable<string> GetMemberNames(object dynObj)
        {
            var metaObjProvider = dynObj as IDynamicMetaObjectProvider;

            if (null == metaObjProvider) throw new InvalidOperationException(
                "The supplied object must be a dynamic object " +
                "(i.e. it must implement IDynamicMetaObjectProvider)"
            );

            var metaObj = metaObjProvider.GetMetaObject(
                Expression.Constant(metaObjProvider)
            );

            var memberNames = metaObj.GetDynamicMemberNames();

            return memberNames;
        }
    }
}