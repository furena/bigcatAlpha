using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace B2BPortal.Common.Enums
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
        BulkInviteSubmission,
        BulkInviteResults
    }

    /// <summary>
    /// List of possible guest request approval choices:
    ///     Approved     (manually approved),
    ///     AutoApproved (approved via preauth match OR batch processing),
    ///     Denied       (manually denied),
    ///     Pending      (queued for manual review),
    ///     QueuePending (uploaded by an admin/inviter for batch processing)
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