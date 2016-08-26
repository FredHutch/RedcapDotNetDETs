using System.Net.Mail;
using log4net;
using System.Web.Configuration;
using System.ComponentModel;

namespace DotNetDETs.Infrastructure
{
    public class Messaging
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string mailServer = WebConfigurationManager.AppSettings["MailServer"].ToString();

        public void SendEmail(string from, string to, string subject, string body, string cc, string bcc, string recordId)
        {
            SmtpClient smtp = new SmtpClient(mailServer);
            MailMessage msg = new MailMessage();
            smtp.SendCompleted += new
            SendCompletedEventHandler(SendCompletedCallback);
            msg.From = new MailAddress(from);

            foreach (string address in to.Split(new char[] { ',', ';' }))
            {
                msg.To.Add(new MailAddress(address));
            }

            if (!string.IsNullOrEmpty(cc))
            {
                foreach (string address in cc.Split(new char[] { ',', ';' }))
                {
                    msg.CC.Add(new MailAddress(address));
                }
            }

            if (!string.IsNullOrEmpty(bcc))
            {
                foreach (string address in bcc.Split(new char[] { ',', ';' }))
                {
                    msg.Bcc.Add(new MailAddress(address));
                }
            }
            msg.Subject = subject;
            msg.IsBodyHtml = true;
            msg.Body = body;

            string messageSummary = $"Email from: '{from}', to: '{to}', recordId {recordId}";

            Log.InfoFormat("Attempting to send email. Details: {0}", messageSummary);
            smtp.SendAsync(msg, messageSummary);
        }

        private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
            string token = (string)e.UserState;

            if (e.Cancelled)
            {
                Log.ErrorFormat("Messaging.SendMail for {0} was cancelled.", token);
            }
            if (e.Error != null)
            {
                Log.ErrorFormat("Messaging.SendMail for {0}. Details: {1}", token, e.Error.ToString());
            }
        }
    }
}