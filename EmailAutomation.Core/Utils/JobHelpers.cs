using EmailAutomation.Core.Jobs;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace EmailAutomation.Core.Utils
{
    public class JobHelper
    {
        internal static IJobDetail createSendEmailJob(MailMessage email, string host, int port)
        {
            JobDataMap dataMap = new JobDataMap();
            dataMap.Add("host", host);
            dataMap.Add("port", port);

            JobKey jobKey = new JobKey(email.To.First().Address + "-" + Guid.NewGuid());

            IJobDetail job = JobBuilder.Create<SendEmailJob>()
                .SetJobData(dataMap)
                .WithIdentity(jobKey)
                .Build();

            return job;
        }

        internal static ITrigger createSendEmailTrigger(JobKey key)
        {
            TriggerKey triggerKey = new TriggerKey("trigger-" + key.Name);

            ITrigger trigger = TriggerBuilder.Create()
                .StartNow()
                .WithIdentity(triggerKey)
                .Build();
            return trigger;
        }

        internal static void scheduleJob(IJobDetail job, ITrigger trigger)
        {
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();

            if (scheduler.IsShutdown)
            {
                scheduler.Start();
            }

            scheduler.ScheduleJob(job, trigger);
        }
    }
}
