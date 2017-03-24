using Common.Logging;
using EmailAutomation.Core.Utils;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmailAutomation.Core.Jobs
{
    public class SqlServerScanJob : IJob
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public void Execute(IJobExecutionContext context)
        {
            Logger.Info("Scanning SqlServer for emails");
            try
            {
                // Step 1. parse the values from the job data map. 
                string connectionString = context.MergedJobDataMap.GetString("connectionString");
                string host = context.MergedJobDataMap.GetString("host");
                int port = context.MergedJobDataMap.GetInt("port");
                int maxRead = context.MergedJobDataMap.GetInt("maxRead");

                // Step 2. Connect to database, and query.
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand();
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
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    int rows = adapter.Fill(queryResult);

                    Logger.Debug($"Retrieved:{rows} Rows from Sql Server");

                    // go through the results.
                    foreach(DataRow row in queryResult.Rows)
                    {
                        string from = row["from"].ToString();
                        string to = row["to"].ToString();
                        string subject = row["subject"].ToString();
                        string body = row["body"].ToString();

                        MailMessage email = new MailMessage(from, to, subject, body);

                        // Create a job-trigger based off the row of data
                        IJobDetail job = JobHelper.createSendEmailJob(email, host, port);
                        ITrigger trigger = JobHelper.createSendEmailTrigger(job.Key);
                        
                        // fire off the job to send the email.
                        JobHelper.scheduleJob(job, trigger);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occured in scanning for emails from SqlServer", ex);
                throw new JobExecutionException();
            }
        }
    }
}
