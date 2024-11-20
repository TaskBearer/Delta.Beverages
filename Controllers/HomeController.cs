using Delta.Beverages.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;

namespace Delta.Beverages.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        public HomeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            HomeViewModel model = new HomeViewModel();
            model = BindData(model);
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("user")))
            {
                HttpContext.Session.SetString("user", model.AllUsers.FirstOrDefault().ID);
            }
            return View(model);
        }

        public IActionResult Privacy()
        {
            HomeViewModel model = new HomeViewModel();
            model = BindData(model);
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        protected HomeViewModel BindData(HomeViewModel model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    DataTable dtSKURuns = new DataTable();

                    SqlDataAdapter adSUKRuns = new SqlDataAdapter("SELECT R.ID AS ID, R.SelectedLine, S.SKU ,R.ChangeTimeStamp FROM Runs AS R INNER JOIN SKUs AS S ON R.SKUID = S.ID" +
                        " WHERE R.ChangeTimeStamp > DATEADD(hour, -24, GETDATE())", con);
                    adSUKRuns.Fill(dtSKURuns);

                    List<SKURun> SKURuns = new List<SKURun>();
                    int count = 0;
                    foreach (DataRow row in dtSKURuns.Rows)
                    {
                        SKURuns.Add(new SKURun
                        {
                            ID = row["ID"].ToString(),
                            SelectedLine = row["SelectedLine"].ToString(),
                            SKU = row["SKU"].ToString(),
                            ChangeTimeStamp = DateTime.Parse(row["ChangeTimeStamp"].ToString()),
                            Selected = count == 0 ? true : false,
                        });
                        count++;
                    }

                    model.SKURuns = SKURuns;

                    DataTable dtAllSKUs = new DataTable();
                    SqlDataAdapter adAllSKUs = new SqlDataAdapter("SELECT ID, SKU, Description FROM SKUs", con);
                    adAllSKUs.Fill(dtAllSKUs);

                    List<SKU> AllSKUs = new List<SKU>();
                    foreach (DataRow row in dtAllSKUs.Rows)
                    {
                        AllSKUs.Add(new SKU
                        {
                            ID = row["ID"].ToString(),
                            Name = row["SKU"].ToString(),
                            Description = row["Description"].ToString()
                        });
                    }

                    model.AllSKUs = AllSKUs;

                    DataTable dtAllUsers = new DataTable();
                    SqlDataAdapter adAllUsers = new SqlDataAdapter("SELECT ID, UserName FROM UserRoles WHERE [Function] = 'Production Reporting'", con);
                    adAllUsers.Fill(dtAllUsers);

                    List<User> AllUsers = new List<User>();
                    foreach (DataRow row in dtAllUsers.Rows)
                    {
                        AllUsers.Add(new User
                        {
                            ID = row["ID"].ToString(),
                            Name = row["UserName"].ToString()
                        });
                    }

                    model.AllUsers = AllUsers;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            return model;
        }

        public IActionResult GetRunDetails(string selectedLine)
        {
            int selectedSkuId = Convert.ToInt32(selectedLine);
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                DataSet dtSKUs = new DataSet();
                SqlDataAdapter adaptSKUs = new SqlDataAdapter($" SELECT R.ID, R.SKUID, R.SelectedLine, R.SelectedShift, R.SelectedUserID, R.IsActive, R.EndRunTimeStamp, S.Description," +
                    $" (SELECT SUM(CAST([Count] AS INT)) FROM [dbo].[RunsDetails] WHERE RunsID =  {selectedLine}) as TotalCount " +
                    $"FROM Runs AS R INNER JOIN SKUs AS S ON R.SKUID = S.ID  WHERE " +
                    //                        $"R.ChangeTimeStamp > DATEADD(hour, -72, GETDATE()) AND " +
                    $"R.ID =  {selectedLine}", con);
                adaptSKUs.Fill(dtSKUs);

                RunDetails runDetails = new RunDetails
                {
                    SKUId = dtSKUs.Tables[0].Rows[0]["SKUID"].ToString(),
                    SelectedLine = dtSKUs.Tables[0].Rows[0]["SelectedLine"].ToString(),
                    SelectedShift = dtSKUs.Tables[0].Rows[0]["SelectedShift"].ToString(),
                    SelectedUserID = dtSKUs.Tables[0].Rows[0]["SelectedUserID"].ToString(),
                    IsActive = dtSKUs.Tables[0].Rows[0]["IsActive"].ToString(),
                    EndRunTimeStamp = dtSKUs.Tables[0].Rows[0]["EndRunTimeStamp"].ToString(),
                    Description = dtSKUs.Tables[0].Rows[0]["Description"].ToString(),
                    TotalCount = dtSKUs.Tables[0].Rows[0]["TotalCount"].ToString()
                };

                return new JsonResult(runDetails);
            }
        }

        public IActionResult GetSKURunDetails(string selectedLine)
        {
            int selectedSkuId = Convert.ToInt32(selectedLine);
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();

                DataSet dtSKUs = new DataSet();
                SqlDataAdapter adaptSKUs = new SqlDataAdapter($" SELECT R.ID AS ID, R.SelectedLine, S.SKU ,R.ChangeTimeStamp FROM Runs AS R INNER JOIN SKUs AS S ON R.SKUID = S.ID where R.ID =  {selectedLine}", con);
                adaptSKUs.Fill(dtSKUs);

                SKURun runDetails = new SKURun
                {
                    ID = dtSKUs.Tables[0].Rows[0]["ID"].ToString(),
                    SelectedLine = dtSKUs.Tables[0].Rows[0]["SelectedLine"].ToString(),
                    SKU = dtSKUs.Tables[0].Rows[0]["SKU"].ToString(),
                    ChangeTimeStamp = DateTime.Parse(dtSKUs.Tables[0].Rows[0]["ChangeTimeStamp"].ToString()),
                };

                return new JsonResult(runDetails.ToString());
            }
        }

        public IActionResult AddRun(string SKUID, string SelectedLine, string SelectedShift, string SelectedUserID)
        {
            string query = "INSERT INTO [Runs] ([SKUID], [SelectedLine], [SelectedShift], [SelectedUserID], [ChangeTimeStamp], [CreateTimeStamp], [IsActive]) " +
                               "VALUES (@SKUID, @SelectedLine, @SelectedShift, @SelectedUserID, @ChangeTimeStamp, @CreateTimeStamp, @IsActive)";
            // Establish connection and execute query
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@SKUID", SKUID);
                    cmd.Parameters.AddWithValue("@SelectedLine", SelectedLine);
                    cmd.Parameters.AddWithValue("@SelectedShift", SelectedShift);
                    cmd.Parameters.AddWithValue("@SelectedUserID", SelectedUserID);
                    cmd.Parameters.AddWithValue("@ChangeTimeStamp", DateTime.Now);
                    cmd.Parameters.AddWithValue("@CreateTimeStamp", DateTime.Now);
                    cmd.Parameters.AddWithValue("@IsActive", "0");

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        //  startRunDetails(con);
                    }
                    con.Close();
                }
            }

            return RedirectToAction("Index");
        }

        public IActionResult EditRun(string selectedRunID, string SKUID, string SelectedLine, string SelectedShift, string SelectedUserID)
        {
            try
            {
                //if (!IsEditMode)
                //{       
                // SQL query to insert record

                string query = $"UPDATE Runs " +
                    $"SET SKUID = @SKUID, SelectedLine = @SelectedLine, SelectedShift = @SelectedShift, " +
                    $"SelectedUserID = @SelectedUserID, ChangeTimeStamp = @ChangeTimeStamp " +
                    $"WHERE ID = {selectedRunID}";


                //string query = "INSERT INTO [Runs] ([SKUID], [SelectedLine], [SelectedShift], [SelectedUserID], [ChangeTimeStamp]) " +
                //              "VALUES (@SKUID, @SelectedLine, @SelectedShift, @SelectedUserID, @ChangeTimeStamp)";
                // Establish connection and execute query
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {

                        // Add parameters to the query to prevent SQL injection
                        //  cmd.Parameters.AddWithValue("@ID", ID);
                        cmd.Parameters.AddWithValue("@SKUID", SKUID);
                        cmd.Parameters.AddWithValue("@SelectedLine", SelectedLine);
                        cmd.Parameters.AddWithValue("@SelectedShift", SelectedShift);
                        cmd.Parameters.AddWithValue("@SelectedUserID", SelectedUserID);
                        cmd.Parameters.AddWithValue("@ChangeTimeStamp", DateTime.Now);

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();


                        con.Close();


                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            return GetSKURunDetails(selectedRunID);
        }
        public IActionResult EndProduction()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string equery = $"UPDATE [dbo].[Runs] SET IsActive = 0, " +
                    $"EndRunTimeStamp = CASE  " +
                    $"WHEN EndRunTimeStamp IS NULL  " +
                    $"THEN '{DateTime.Now}'  " +
                    $"ELSE EndRunTimeStamp END, " +
                    $"EndProductionTimeStamp =  '{DateTime.Now}'";
                using (SqlCommand cmd = new SqlCommand(equery, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        public IActionResult StartRun(string selectedRunID)
        {
            try
            {
                startRunDetails(selectedRunID);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            return Json("1");
        }

        public IActionResult SKUChanged(string SKUId)
        {
            var sku = new SKU();
            try
            {
                // Get the selected SKU ID
                int selectedSkuId = Convert.ToInt32(SKUId);
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    DataSet dtSKUs = new DataSet();
                    SqlDataAdapter adaptSKUs = new SqlDataAdapter($"SELECT ID, SKU, Description, Line FROM SKUs WHERE ID = {selectedSkuId}", con);
                    adaptSKUs.Fill(dtSKUs);                    

                    if (dtSKUs.Tables[0].Rows.Count > 0)
                    {
                        sku.Description = dtSKUs.Tables[0].Rows[0]["Description"].ToString();
                        sku.Line = dtSKUs.Tables[0].Rows[0]["Line"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            return new JsonResult(sku);
        }

        public void SetUser(string UserId)
        {
            HttpContext.Session.SetString("user", UserId);
        }
        private void startRunDetails(string selectedRunID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    string equery = $"UPDATE  [dbo].[Runs] SET IsActive = 1 WHERE ID =  {selectedRunID}";
                    using (SqlCommand cmd = new SqlCommand(equery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }


                    ////  TODO: this code, move to Start Run
                    //DataSet dtActiveRuns = new DataSet();
                    //SqlDataAdapter adaptActiveRuns = new SqlDataAdapter("SELECT ID as ID FROM [OperatorDB].[dbo].[Runs] where id =selrcted", con);
                    //adaptActiveRuns.Fill(dtActiveRuns);

                    //foreach (DataRow row in dtActiveRuns.Tables[0].Rows)
                    //{
                    // Access each column using the column name or index
                    int runID = Convert.ToInt32(selectedRunID); // Assuming RunID is an integer
                    int hour = DateTime.Now.Hour;
                    BackgroundWorker.insertRunDetails(runID, hour, con);
                    //    break;
                    //}
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }
    }
}
