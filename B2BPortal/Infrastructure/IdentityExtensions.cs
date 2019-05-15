﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Claims;
using System.Security.Principal;
using System.Diagnostics;
using B2BPortal.Common.Utils;

namespace B2BPortal.Infrastructure
{
    public static class IdentityExtensions
    {
        public static string[] GetClaims(this IIdentity identity, string claimName)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");
            var identity1 = identity as ClaimsIdentity;
            return (identity1 != null && identity1.IsAuthenticated) ? identity1.Claims.Where(c => c.Type == claimName).Select(c => c.Value.ToString()).ToArray() : null;
        }

        public static string GetClaim(this IIdentity identity, string claimName)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");
            var identity1 = identity as ClaimsIdentity;
            return (identity1 != null && identity1.IsAuthenticated) ? identity1.FindFirst(claimName).Value : null;
        }

        public static string GetEmail(this IIdentity identity)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");
            var identity1 = identity as ClaimsIdentity;
            return (identity1 != null && identity1.IsAuthenticated) ? identity1.FindFirst(ClaimTypes.Email).Value : null;
        }

        public static T GetClaim<T>(this IIdentity identity, string claimName)
        {
            if (identity == null)
                return default(T);

            try
            {
                var claimsIdentity = (ClaimsIdentity)identity;

                var res = claimsIdentity.Claims.Where(cx => cx.Type == claimName).Select(c => c.Value).SingleOrDefault();
                return (res == null) ? default(T) : (T)Convert.ChangeType(res, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public static bool HasClaim(this IIdentity identity, string claimName)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");

            return ((ClaimsIdentity)identity).HasClaim(c => c.Type == claimName);
        }

        /// <summary>
        /// Update an existing claim or add a new claim
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="claimName"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static bool SetClaim(this IIdentity identity, string claimName, string newValue)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");

            try
            {
                var identity1 = identity as ClaimsIdentity;
                if (identity1 == null) return false;

                var claim = identity1.FindFirst(claimName);

                if (claim == null)
                {
                    identity1.AddClaim(new Claim(claimName, newValue));
                    return true;
                }

                var claimType = claim.Type;
                var valueType = claim.ValueType;

                identity1.RemoveClaim(claim);
                identity1.AddClaim(new Claim(claimType, newValue, valueType));

                return true;
            }
            catch (Exception ex)
            {
                Logging.WriteToAppLog("error setting replacement claim", EventLogEntryType.Error, ex);
                return false;
            }
        }

        public static bool RemoveClaims(this IIdentity identity, string claimName)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");

            try
            {
                var identity1 = identity as ClaimsIdentity;
                if (identity1 == null) return false;

                identity1.Claims.Where(c => c.Type == claimName).ToList().ForEach(identity1.RemoveClaim);
                return true;
            }
            catch (Exception ex)
            {
                Logging.WriteToAppLog("error removing claim", EventLogEntryType.Error, ex);
                return false;
            }
        }

        /// <summary>
        /// Return the collection of the user's current system roles
        /// </summary>
        /// <param name="identity">the user's IIdentity object</param>
        /// <returns>collection of strings</returns>
        public static IEnumerable<String> GetRoles(this IIdentity identity)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");

            var identity1 = (identity as ClaimsIdentity);
            return identity1 != null ? identity1.Claims.Where(x => x.Type == ClaimTypes.Role).Select(n => n.Value) : null;
        }

        /// <summary>
        /// Verifies whether the user has been assigned a given role
        /// </summary>
        /// <param name="identity">the user's IIdentity object</param>
        /// <param name="roleName">the role to verify</param>
        /// <returns>collection of strings</returns>
        public static bool IsInRole(this IIdentity identity, string roleName)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");

            var identity1 = (identity as ClaimsIdentity);
            return (identity1 != null) && identity1.Claims.Any(x => x.Type == ClaimTypes.Role && x.Value == roleName);
        }

        /// <summary>
        /// Verifies whether the user has any of a string collection of roles assigned
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public static bool IsInAnyRole(this IIdentity identity, IList<string> roles)
        {
            if (identity == null)
                throw new ArgumentNullException("identity");

            var identity1 = (identity as ClaimsIdentity);
            return (identity1 != null) && identity1.Claims.Any(x => x.Type == ClaimTypes.Role && roles.Any(r => r == x.Value));
        }
    }
}