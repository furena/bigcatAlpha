using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace B2BPortal.Interfaces
{
    /// <summary>
    /// Filter for DocumentDB doc types - any records stored in DocumentDB need their type names added here
    /// </summary>
    public enum DocTypes
    {
        PreAuthDomain,
        GuestRequest,
        SiteConfig,
        InviteTemplate,
        PerWebUserCache,
        BulkInviteSubmission
    }

    /// <summary>
    /// List of possible guest request approval choices:
    ///     Approved     - request has been manually approved
    ///     AutoApproved - request approved via preauth match OR batch processing
    ///     Denied       - request manually denied
    ///     Pending      - request has been queued for manual review
    ///     QueuePending - request(s) were uploaded by a guest inviter in the admin portal for batch processing
    /// </summary>
    public enum Disposition
    {
        Approved,
        AutoApproved,
        Denied,
        Pending,
        QueuePending
    }
    public enum MemberType
    {
        Guest,
        Member
    }
}