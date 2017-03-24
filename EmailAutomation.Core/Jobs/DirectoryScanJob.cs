using Common.Logging;
using EmailAutomation.Core.Utils;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmailAutomation.Core.Jobs
{
    public class DirectoryScanJob : IJob
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                Logger.Info("Scanning directory for emails");
                // Step 1. Get parameters from context. 
                // Step 1(a). Get path
                string path = context.MergedJobDataMap.GetString("path");

                // Step 1(b). Get subject
                string subject = context.MergedJobDataMap.GetString("subject");

                // Step 1(c). Get from
                string from = context.MergedJobDataMap.GetString("from");

                // Step 1(d). Get the SMTP host
                string host = context.MergedJobDataMap.GetString("host");

                // Step 1(e). Get the SMTP port
                int port = context.MergedJobDataMap.GetInt("port");
                if(Logger.IsDebugEnabled)
                {
                    Logger.Debug($"Path:{path}, Subject:{subject}, From:{from}");
                }

                // Scan for files in directory.
                foreach(string file in Directory.EnumerateFiles(path))
                {
                    // Step 2. Create the email object. 
                    string body = File.ReadAllText(Path.Combine(path, file));
                    string to = Path.GetFileNameWithoutExtension(file);
                    MailMessage email = new MailMessage(from, to, subject, body);

                    if(Logger.IsDebugEnabled)
                    {
                        Logger.Debug($"Body:{body}, To: {to}");
                    }

                    // Step 3. Create the send mail job.
                    IJobDetail job = JobHelper.createSendEmailJob(email, host, port);
                    ITrigger trigger = JobHelper.createSendEmailTrigger(job.Key);

                    // Step 4. schedule the job to fire. 
                    JobHelper.scheduleJob(job, trigger);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occured in scanning for emails from directory", ex);
                throw new JobExecutionException();
            }
        }
    }
}
