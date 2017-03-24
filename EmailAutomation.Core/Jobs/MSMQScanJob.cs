using Common.Logging;
using EmailAutomation.Core.Utils;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmailAutomation.Core.Jobs
{
    public class MSMQScanJob : IJob
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Execute(IJobExecutionContext context)
        {
            Logger.Info("Scanning Microsoft Message Queue for emails.");
            try
            {

                string host = context.MergedJobDataMap.GetString("host");
                int port = context.MergedJobDataMap.GetInt("port");
                string path = context.MergedJobDataMap.GetString("path");

                if (MessageQueue.Exists(path))
                {
                    Logger.Info($"Found path:{path}");

                    using (MessageQueue queue = new MessageQueue(path))
                    {
                        while(queue.Peek(TimeSpan.FromSeconds(5)) != null)
                        {
                            Message message = queue.Receive(TimeSpan.FromSeconds(5));
                            MailMessage email = message.Body as MailMessage;

                            IJobDetail job = JobHelper.createSendEmailJob(email, host, port);
                            ITrigger trigger = JobHelper.createSendEmailTrigger(job.Key);

                            JobHelper.scheduleJob(job, trigger);
                        }
                    }
                }
                else
                {
                    Logger.Info($"Cannot find Queue path:{path}");
                }
                
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occured in scanning for emails from Microsoft Message Queue.", ex);
                throw new JobExecutionException();
            }
        }
    }
}
