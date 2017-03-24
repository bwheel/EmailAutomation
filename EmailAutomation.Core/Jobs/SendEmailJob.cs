using Common.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmailAutomation.Core.Jobs
{
    public class SendEmailJob : IJob
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                MailMessage email = context.MergedJobDataMap.Get("email") as MailMessage;
                int port = context.MergedJobDataMap.GetInt("port");
                string host = context.MergedJobDataMap.GetString("host");

                Logger.Info($"Sending email to:{email.To}");
                if(Logger.IsDebugEnabled)
                {
                    Logger.Debug($"Host:{host}-Port:{port}-Email:{email.ToString()}");
                }

                using (SmtpClient client = new SmtpClient(host, port))
                {
                    client.UseDefaultCredentials = true;
                    client.Send(email);
                }
            }
            catch(Exception ex)
            {
                Logger.Error("An exception occured in sending email", ex);
                throw new JobExecutionException();
            }
        }
    }
}
