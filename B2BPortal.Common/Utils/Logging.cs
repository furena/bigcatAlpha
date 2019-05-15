using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
//using System.Web.Http.Filters;

namespace B2BPortal.Common.Utils
{
    public static class Logging
    {
        public static bool AlertsEnabled { get; set; }
        public static string AlertRecipients { get; set; }
        public static string AppServer { get; set; }
        
        /// <summary>
        /// Writes an error entry to the Application log, Application Source. This is a fallback error writing mechanism.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorType">Type of error.</param>
        /// <param name="ex">Original exception (optional)</param>
        public static string WriteToAppLog(string message, EventLogEntryType errorType, Exception ex = null)
        {
            message = GetErrorMessageString(message, ex);
            EventLog.WriteEntry("Application", message, errorType, 0);
            return message;
        }

        public static string GetErrorMessageString(string message, Exception ex = null)
        {
            if (ex != null)
            {
                message += message + " (original error: " + ex.Source + "/" + ex.Message + "\r\nStack Trace: " +
                                ex.StackTrace + ")";
                if (ex.InnerException != null)
                {
                    message += "\r\nInner Exception: " + ex.GetBaseException();
                }
            }
            return message;
        }
        #region helpers

        private static string FormatAlertMessage(string message, string source)
        {
            var res = new StringBuilder();
            res.AppendFormat("<p>The following error occured at {0} (server time):</p>", DateTime.Now);
            res.AppendLine("    <blockquote>");
            res.AppendFormat("      Message: {0}<br>", message);
            res.AppendFormat("      Source: {0}<br>", source);
            res.AppendLine("    </blockquote>");
            res.AppendFormat("<p>View error details at {0}/ErrorLog</p>", AppServer);
            return Utils.GetHtmlMessageWrapper("Application Alert", res.ToString());
        }
        #endregion
    }
}
