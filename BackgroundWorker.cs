using System.Data.SqlClient;
using System.Data;
using System.Timers;

namespace Delta.Beverages.Web
{
    public class BackgroundWorker
    {
        static System.Timers.Timer aTimer = null;
        static IConfiguration _configuration;

        public BackgroundWorker(IConfiguration configuration)
        {
            _configuration = configuration;
            SetInitialTimer();
            OnTimedEvent(null, null);
        }

        private void SetInitialTimer()
        {
            try
            {
                DateTime now = DateTime.Now;
                DateTime nextHour = now.AddHours(1).Date.AddHours(now.Hour + 1);
                double initialInterval = (nextHour - now).TotalMilliseconds;

                aTimer = new System.Timers.Timer(initialInterval); // Time remaining until the next hour
                aTimer.Elapsed += new ElapsedEventHandler(OnInitialTimedEvent);
                aTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }

        private void OnInitialTimedEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                aTimer.Stop();
                aTimer.Elapsed -= OnInitialTimedEvent;

                // Execute your hourly task here
                OnTimedEvent(source, e);

                // Set the timer to trigger every hour
                aTimer.Interval = 60 * 60 * 1000; // one hour in milliseconds
                aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

                // Refresh pages


                aTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }

        //public BackgroundWorker()
        //{
        //    aTimer = new System.Timers.Timer(60 * 60 * 1000); //one hour in milliseconds
        //    aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        //    SetNextInterval();
        //}
        //static private void SetNextInterval()
        //{
        //    DateTime nowTime = DateTime.Now;
        //    DateTime specificTime = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, 1, 0, 0, 0);
        //    if (nowTime > specificTime)
        //        specificTime = specificTime.AddDays(1);

        //    double tickTime = (specificTime - nowTime).TotalMilliseconds;
        //    aTimer = new Timer(tickTime);
        //    aTimer.Elapsed += OnTimedEvent;
        //    aTimer.Start();
        //}
        private static void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            string cs = _configuration.GetConnectionString("DefaultConnection").ToString();
            try
            {
                // get the connection    
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();

                    DataSet dtActiveRuns = new DataSet();
                    SqlDataAdapter adaptActiveRuns = new SqlDataAdapter("SELECT * FROM [dbo].[Runs] WHERE IsActive = 1", conn);
                    adaptActiveRuns.Fill(dtActiveRuns);

                    foreach (DataRow row in dtActiveRuns.Tables[0].Rows)
                    {
                        // Access each column using the column name or index
                        int runID = (int)row["ID"]; // Assuming RunID is an integer
                        int hour = DateTime.Now.Hour;
                        insertRunDetails(runID, hour, conn);

                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }

        public static void insertRunDetails(int runID, int hour, SqlConnection con)
        {
            try
            {
                /*string query = "If NOT Exists ( SELECT RunsID FROM [RunsDetails] WHERE RunsID = @RunsID AND TimeWindowTimeStamp= @TimeWindowTimeStamp) " +
                "INSERT INTO [RunsDetails] ([RunsID], [TimeWindowTimeStamp], [Count], [ChangeTimeStamp]) " +
                              "VALUES (@RunsID, @TimeWindowTimeStamp, @Count, @ChangeTimeStamp)" +
                              "ELSE " +
                              "UPDATE  [RunsDetails] " +
                              "SET [TimeWindowTimeStamp] = @TimeWindowTimeStamp , [ChangeTimeStamp] = @ChangeTimeStamp " +
                              "WHERE RunsID = @RunsID AND TimeWindowTimeStamp= @TimeWindowTimeStamp";
                */
                string query = "IF NOT EXISTS " +
                    "( " +
                    "SELECT RunsID " +
                    "FROM [RunsDetails] " +
                    "    WHERE RunsID = @RunsID " +
                    "    AND TimeWindowTimeStamp = @TimeWindowTimeStamp" +
                    ")" +
                    "BEGIN" +
                    "    INSERT INTO [RunsDetails] ([RunsID], [TimeWindowTimeStamp], [Count], [ChangeTimeStamp]) " +
                    "    VALUES (@RunsID, @TimeWindowTimeStamp, @Count, @ChangeTimeStamp)" +
                    "END";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@RunsID", runID);
                    cmd.Parameters.AddWithValue("@TimeWindowTimeStamp", CreateDateTimeByHour(hour));
                    cmd.Parameters.AddWithValue("@Count", "0");
                    cmd.Parameters.AddWithValue("@ChangeTimeStamp", DateTime.Now);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }


        static DateTime CreateDateTimeByHour(int hour)
        {
            // Creating a DateTime object with just the hour parameter
            return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, 0, 0);
        }
    }
}
