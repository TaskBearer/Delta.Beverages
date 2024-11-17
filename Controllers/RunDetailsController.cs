using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using Delta.Beverages.Web.Models;
using System.Reflection;

namespace Delta.Beverages.Web.Controllers
{
    public class RunDetailsController : Controller
    {
        private readonly string _connectionString;
        public RunDetailsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public IActionResult Index(string flag, string RunsId)
        {
            HomeViewModel model = new HomeViewModel();
            model = BindData(RunsId, model);
            return View(model);
        }

        public HomeViewModel BindData(string selectedIndex, HomeViewModel model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

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

                    // Retrieve data for GridView1
                    DataTable dt = new DataTable();
                    SqlDataAdapter adapt1 = new SqlDataAdapter("SELECT R.ID AS ID, 'Line ' + CONVERT(VARCHAR, R.SelectedLine) + ' - SKU ' + CONVERT(VARCHAR, S.SKU) " +
                        "+ ' \n ' + FORMAT(R.ChangeTimeStamp, 'MM-dd-yyyy') AS RunDescription " +
                        $"FROM Runs AS R INNER JOIN SKUs AS S ON R.SKUID = S.ID WHERE R.ID = {selectedIndex}"
                       //                     +   " WHERE R.ChangeTimeStamp > DATEADD(hour, -24, GETDATE())" 
                       , con);
                    adapt1.Fill(dt);

                    model.RunDetails = new RunDetails();
                    model.RunDetails.SelectedLine = dt.Rows[0]["ID"].ToString();
                    model.RunDetails.Description = dt.Rows[0]["RunDescription"].ToString();


                    dt = new DataTable();
                    adapt1 = new SqlDataAdapter($"SELECT CONCAT(FORMAT(TimeWindowTimeStamp, 'hh:mm tt'), ' - ', FORMAT(DATEADD(HOUR, 1, TimeWindowTimeStamp), 'hh:mm tt')) AS TimeWindowTimeStamp, " +
                        $"ID, RunsID, Count  FROM [RunsDetails] WHERE RunsID = {selectedIndex} "
                        //+ $" AND ChangeTimeStamp > DATEADD(hour, -24, GETDATE())"
                        , con);
                    adapt1.Fill(dt);

                    var runsTimeWindowDetails = new List<RunsTimeWindowDetails>();
                    if (dt != null && dt.Rows.Count > 0)
                    {

                        foreach (DataRow row in dt.Rows)
                        {
                            runsTimeWindowDetails.Add(new RunsTimeWindowDetails
                            {
                                TimeWindowTimeStamp = row["TimeWindowTimeStamp"].ToString(),
                                ID = row["ID"].ToString(),
                                RunsID = row["RunsID"].ToString(),
                                Count = row["Count"].ToString(),
                            });

                        }

                        model = GetData(dt.Rows[0]["RunsID"].ToString(), model);
                    }
                    model.RunsTimeWindowDetails = runsTimeWindowDetails;

                    //DataTable dtSKUs = new DataTable();
                    //using (var con1 = new SqlConnection(cs))
                    //{
                    //    con1.Open();
                    //    //// Retrieve data for GridView2
                    //    SqlDataAdapter adapt2 = new SqlDataAdapter("SELECT ID, SKU, Description FROM SKUs", con1);
                    //    adapt2.Fill(dtSKUs);
                    //}
                    //foreach (GridViewRow row in GridView2.Rows)
                    //{
                    //    if (row.RowType == DataControlRowType.DataRow)
                    //    {
                    //        GetUIEliments();

                    //        if (row.RowType == DataControlRowType.DataRow)
                    //        {
                    //            var textboxCount = (TextBox)row.FindControl("textboxCount");
                    //            if (textboxCount.Text == "0")
                    //            {
                    //                textboxCount.BackColor = Color.Red;
                    //            }
                    //        }

                    //        //  row.FindControl

                    //        if (DropDownListSelectSKU != null)
                    //        {
                    //            dtSKUs = new DataTable();
                    //            SqlDataAdapter adaptSKUs = new SqlDataAdapter("SELECT ID, SKU, Description FROM SKUs", con);
                    //            adaptSKUs.Fill(dtSKUs);

                    //            // Add a default "Select User" item at the beginning
                    //            DataRow newRow = dtSKUs.NewRow();
                    //            newRow["ID"] = 0; // Assuming 0 is not a valid user ID
                    //            newRow["SKU"] = "Select SKU";
                    //            dtSKUs.Rows.InsertAt(newRow, 0);


                    //            // Bind the DropDownList to the list
                    //            DropDownListSelectSKU.DataTextField = "SKU"; // Display SKU
                    //            DropDownListSelectSKU.DataValueField = "ID"; // SKU ID
                    //            DropDownListSelectSKU.DataSource = dtSKUs;
                    //            DropDownListSelectSKU.DataBind();
                    //        }

                    //        if (DropDownListSelectUser != null)
                    //        {
                    //            DataTable dtUsers = new DataTable();
                    //            SqlDataAdapter adaptUsers = new SqlDataAdapter("SELECT ID, UserName FROM UserRoles WHERE [Function] = 'Production Reporting'", con);
                    //            adaptUsers.Fill(dtUsers);

                    //            // Add a default "Select User" item at the beginning
                    //            DataRow newRow = dtUsers.NewRow();
                    //            newRow["ID"] = 0; // Assuming 0 is not a valid user ID
                    //            newRow["UserName"] = "Select User";
                    //            dtUsers.Rows.InsertAt(newRow, 0);

                    //            DropDownListSelectUser.DataTextField = "UserName";
                    //            DropDownListSelectUser.DataValueField = "ID";
                    //            DropDownListSelectUser.DataSource = dtUsers;
                    //            DropDownListSelectUser.DataBind();
                    //        }
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (log or display error message)
            }
            return model;
        }

        private HomeViewModel GetData(string runID, HomeViewModel model)
        {
            DataTable table = new DataTable();
            DataTable tableAsset = new DataTable();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = $"SELECT [RunDetailId], [Downtime], A.AssetNumberID as AssetId, A.AssetName as AssetInvolved, [Notes], [ChangeTimeStamp] " +
                    $"FROM DownTime AS D INNER JOIN AssetList AS A ON D.AssetInvolved = A.AssetNumberID WHERE RunDetailId = {runID}";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                    {
                        ad.Fill(table);
                    }
                }
                conn.Close();
            }
            var assetList = new List<AssetList> { };

            //foreach (DataRow row in table.Rows)
            //{
            //    assetList.Add(new AssetList
            //    {
            //        RunDetailId = row["RunDetailId"].ToString(),
            //        AssetId = row["AssetId"].ToString(),
            //        AssetInvolved = row["AssetInvolved"].ToString(),
            //        DownTime = row["DownTime"].ToString(),
            //        Notes = row["Notes"].ToString(),
            //    });
            //}
            model.AssetList = assetList;

            var allAssets = new List<Assets> { };
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string sql = $"SELECT * FROM AssetList";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                    {
                        ad.Fill(tableAsset);

                        foreach (DataRow row in tableAsset.Rows)
                        {
                            allAssets.Add(new Assets
                            {
                                AssetNumberID = row["AssetNumberID"].ToString(),
                                AssetName = row["AssetName"].ToString(),
                            });
                        }
                    }
                }
                con.Close();
            }
            model.AllAssets = allAssets;

            DataTable dtAllUsers = new DataTable();
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                SqlDataAdapter adAllUsers = new SqlDataAdapter("SELECT ID, UserName FROM UserRoles WHERE [Function] = 'Production Reporting'", con);
                adAllUsers.Fill(dtAllUsers);
            }

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
            return model;
        }

        private string GetTotalRunCounts(string runDetailsID)
        {
            string totalRun = string.Empty;
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                DataSet dsSKUs = new DataSet();
                SqlDataAdapter adaptTotalCount = new SqlDataAdapter($"SELECT SUM(CAST([Count] AS INT)) as TotalCount FROM [dbo].[RunsDetails] WHERE RunsID = (SELECT TOP 1 RunsId FROM [dbo].[RunsDetails] WHERE ID = {runDetailsID})", con);
                adaptTotalCount.Fill(dsSKUs);
                totalRun = dsSKUs.Tables[0].Rows[0]["TotalCount"].ToString();
                con.Close();
            }
            return totalRun;
        }

        public IActionResult Submit(string runDetailsID, string count)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string equery =
                    $"UPDATE [dbo].[RunsDetails] SET Count = {count} WHERE ID = {runDetailsID}";

                using (SqlCommand cmd = new SqlCommand(equery, con))
                {
                    cmd.ExecuteNonQuery();
                }

                con.Close();
            }
            return new JsonResult(GetTotalRunCounts(runDetailsID));
        }
        public void SetUser(string UserId)
        {
            HttpContext.Session.SetString("user", UserId);
        }

        public void EndRun(string runsId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string equery = $"UPDATE  [dbo].[Runs] SET IsActive = 0, EndRunTimeStamp = getdate() WHERE EndRunTimeStamp IS NULL AND ID =  {runsId}";
                using (SqlCommand cmd = new SqlCommand(equery, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void AddNewAssetDownTime(string runsDetailsId, string assetInvolved, string downTime, string notes)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string equery = "INSERT INTO DownTime (RunDetailId, Downtime, AssetInvolved, Notes, ChangeTimeStamp) " +
                    "VALUES(@RunDetailId, @Downtime, @AssetInvolved, @Notes, @ChangeTimeStamp)";
                //  string equery = "INSERT INTO DownTime (RunDetailId, Downtime, AssetInvolved, Notes, )" +
                //      " VALUES(@RunDetailId, Downtime, @AssetInvolved, @Notes)";
                using (SqlCommand cmd = new SqlCommand(equery, conn))
                {
                    cmd.Parameters.AddWithValue("@RunDetailId", runsDetailsId);
                    cmd.Parameters.AddWithValue("@Downtime", downTime);
                    cmd.Parameters.AddWithValue("@AssetInvolved", assetInvolved);
                    cmd.Parameters.AddWithValue("@Notes", notes);
                    cmd.Parameters.AddWithValue("@ChangeTimeStamp", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public IActionResult GetDownTimeByRunsDetailsId(string runDetailsID)
        {
            DataTable table = new DataTable();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = $"SELECT D.ID as [DownTimeID], [RunDetailId], [Downtime], A.AssetNumberID as AssetId, A.AssetName as AssetInvolved, [Notes], [ChangeTimeStamp] " +
                    $"FROM DownTime AS D INNER JOIN AssetList AS A ON D.AssetInvolved = A.AssetNumberID WHERE RunDetailId = {runDetailsID}";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataAdapter ad = new SqlDataAdapter(cmd))
                    {
                        ad.Fill(table);
                    }
                }
                conn.Close();
            }
            var assetList = new List<AssetList> { };

            foreach (DataRow row in table.Rows)
            {
                assetList.Add(new AssetList
                {
                    DownTimeId = row["DownTimeId"].ToString(),
                    RunDetailId = row["RunDetailId"].ToString(),
                    AssetId = row["AssetId"].ToString(),
                    AssetInvolved = row["AssetInvolved"].ToString(),
                    DownTime = row["DownTime"].ToString(),
                    Notes = row["Notes"].ToString(),
                });
            }

            return new JsonResult(assetList);
        }
        public void UpdateDownTime(string downTimeID, string runId, string asset, string downTime, string notes)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string equery = "UPDATE DownTime SET RunDetailId=@RunId,AssetInvolved= @Asset, DownTime= @DownTime,Notes= @Notes where ID=@stor_id";
                using (SqlCommand cmd = new SqlCommand(equery, conn))
                {
                    cmd.Parameters.AddWithValue("@RunId", runId);
                    cmd.Parameters.AddWithValue("@Asset", asset);
                    cmd.Parameters.AddWithValue("@DownTime", downTime);
                    cmd.Parameters.AddWithValue("@Notes", notes);
                    cmd.Parameters.AddWithValue("@stor_id", downTimeID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteDownTime(string downTimeID)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string equery = "DELETE DownTime where ID=@stor_id";
                using (SqlCommand cmd = new SqlCommand(equery, conn))
                {
                    cmd.Parameters.AddWithValue("@stor_id", downTimeID);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
