﻿using Common.Logging;
using EmailAutomation.Core.Utils;
using MySql.Data.MySqlClient;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmailAutomation.Core.Jobs
{
    public class MySqlScanJob : IJob
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public void Execute(IJobExecutionContext context)
        {
            Logger.Info("Scanning MySql Database for emails");
            try
            {
                string connectionString = context.MergedJobDataMap.GetString("connectionString");
                string host = context.MergedJobDataMap.GetString("host");
                int port = context.MergedJobDataMap.GetInt("port");
                int maxRead = context.MergedJobDataMap.GetInt("maxRead;")

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    MySqlCommand command = new MySqlCommand();
                    command.Connection = conn;
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandTimeout = 5000;
                    command.CommandText = "DELETE FROM Outbound_Emails WITH (READPAST) OUTPUT DELETED.* " +
                            "WHERE outbound_emails.Id IN " +
                            "(" +
                                "SELECT TOP(@MaxRead) Id " +
                                "FROM outbound_emails " +
                                "ORDER BY Id ASC " +
                            ")";
                    command.Parameters.AddWithValue("@MaxRead", maxRead);
                    command.Connection.Open();

                    DataTable queryResult = new DataTable();

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    int rows = adapter.Fill(queryResult);

                    foreach(DataRow row in queryResult.Rows)
                    {
                        string from = row["from"].ToString();
                        string to = row["to"].ToString();
                        string subject = row["subject"].ToString();
                        string body = row["body"].ToString();

                        MailMessage email = new MailMessage(from, to, subject, body);

                        IJobDetail job = JobHelper.createSendEmailJob(email, host, port);
                        ITrigger trigger = JobHelper.createSendEmailTrigger(job.Key);

                        JobHelper.scheduleJob(job, trigger);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occured in scanning for emails from MySql", ex);
                throw new JobExecutionException();
            }
        }
    }
}
