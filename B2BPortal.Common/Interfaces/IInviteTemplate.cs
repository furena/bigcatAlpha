using System;

namespace B2BPortal.Common.Interfaces
{
    public interface IInviteTemplate: IDocModelBase
    {
        string TemplateName { get; set; }
        string TemplateContent { get; set; }
        string SubjectTemplate { get; set; }
        DateTime LastUpdated { get; set; }
        int TemplateVersion { get; set; }
        string TemplateAuthor { get; set; }
    }
}