using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;

namespace AzureB2BInvite
{
    public static class MailSender
    {
        public static string MailServer { get; set; }
        public static int MailServerPort { get; set; }
        public static string MailFrom { get; set; }
        public static byte[] Logo { get; set; }
        public static string MailTemplate { get; set; }
        public static bool MailEnabled { get; set; }
        public static string SMTPLogin { get; set; }
        public static string SMTPPassword { get; set; }
        public static X509Certificate2 MailCert { get; set; }

        public static string GetTemplateContents(string templateName)
        {
            var filePath = Path.Combine(Settings.AppRootPath, @"Templates\" + templateName);
            return File.ReadAllText(filePath);
        }

        public static void SendMessage(MailMessage msg)
        {
            if (!MailEnabled) return;

            using (var smtpclient = new SmtpClient(MailServer, MailServerPort)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = (MailServerPort == 465 || MailServerPort == 587)
            })
            {
                if (SMTPLogin != "")
                {
                    smtpclient.Credentials = new NetworkCredential(SMTPLogin, SMTPPassword);
                }
                //let it fail up and let the callers log it
                smtpclient.Send(msg);
            }
        }

        public static void SendMessage(string sendTo, string subject, string body, string replyTo = "", string sendCc = "", bool sendAsHtml = true)
        {
            var staticbody = body;
            string formattedBody = "";
            if (sendAsHtml)
            {
                body = body.Replace("  ", "&nbsp;&nbsp;");
                body = body.Replace(Environment.NewLine, "<br>");
                formattedBody = MailTemplate.Replace("{{subject}}", subject);
                formattedBody = formattedBody.Replace("{{body}}", body);
            }

            try
            {
                var msg = new MailMessage
                {
                    From = new MailAddress(MailFrom),
                    Body = ((sendAsHtml) ? formattedBody : staticbody),
                    IsBodyHtml = sendAsHtml,
                    Subject = subject
                };

                bool isAddressed = false;
                if (sendTo.Length>0)
                {
                    isAddressed = true;
                    msg.To.Add(sendTo);
                }
                if (sendCc.Length > 0)
                {
                    isAddressed = true;
                    msg.CC.Add(sendCc);
                }
                if (!isAddressed)
                {
                    return;
                }

                if (replyTo.Length > 0)
                {
                    msg.ReplyToList.Add(replyTo);
                }
                if (sendAsHtml)
                {
                    if (Logo !=null && Logo.Length > 0 && formattedBody.IndexOf("cid:") > 0)
                    {
                        var oRes = new LinkedResource(new MemoryStream(Logo), "image/png")
                        {
                            ContentId = "logo.png"
                        };
                        formattedBody = formattedBody.Replace("cid:", "cid:logo.png");
                        var oView = AlternateView.CreateAlternateViewFromString(formattedBody, new System.Net.Mime.ContentType("text/html"));
                        oView.LinkedResources.Add(oRes);
                        msg.AlternateViews.Add(oView);
                    }
                }
                SendMessage(msg);
            }
            catch (Exception ex)
            {
                throw new Exception("SendMessage failed: " + ex.Message, ex);
            }
        }
    }
}
