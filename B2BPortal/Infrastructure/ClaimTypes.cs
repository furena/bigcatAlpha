using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace B2BPortal.Infrastructure
{
    public static class CustomClaimTypes
    {
        public const string UserId = "UserId";
        public const string IdentityProvider = "http://schemas.microsoft.com/identity/claims/identityprovider";
        public const string ExtClaims = "ExtClaims";
        public const string AuthType = "AuthType";
        public const string MemberOfGroup = "MemberOfGroup";
        public const string FullName = "FullName";
        public const string ObjectIdentifier = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        public const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";
    }
    public static class AuthTypes
    {
        public const string Local = "OpenIdConnect-Local";
        public const string B2EMulti = "OpenIdConnect-Multi";
        public const string B2C = "OpenIdConnect-B2C";
        public const string Api = "ApiAuth";
    }
    public static class Roles
    {
        public const string B2BAdmins = "B2BAdmins";
        public const string CompanyAdministrator = "Company Administrator";
    }
}