using LaborCostAnalysis.Interfaces;
using LaborCostAnalysis.Models;
using LaborCostAnalysis.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LaborCostAnalysis.Controllers
{
    public class ImportProgressController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        IConnectDB DB;
        static int year;
        static string month;
        static List<ProgressModel> ipgs;

        public ImportProgressController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetData()
        {
            List<ProgressModel> pg = GetProgressModels();
            List<List<ProgressModel>> pgs = new List<List<ProgressModel>>();
            string[] job_id = pg.Select(s => s.job_id).Distinct().ToArray();
            for (int i = 0; i < job_id.Count(); i++)
            {
                pgs.Add(pg.Where(w => w.job_id == job_id[i]).Select(s => s).OrderBy(y => y.year).ThenBy(m => m.month).ToList());
            }
            return Json(pgs);
        }

        public List<ProgressModel> GetProgressModels()
        {
            List<ProgressModel> pgs = new List<ProgressModel>();
            this.DB = new ConnectDB();
            SqlConnection con = DB.Connect();
            con.Open();

            string str_cmd = "select Progress.Job_ID, " +
                                    "job.Job_Number, " +
                                    "job.Job_Name, " +
                                    "job.Estimated_Budget, " +
                                    "Progress.Job_Progress, " +
                                    "Progress.Month, " +
                                    "Progress.Year, " +
                                    "sum((cast(s1.Labor_Cost as int) + cast(s1.OT_Labor_Cost as int) + cast(s1.Accommodation_Cost as int) + cast(s1.Compensation_Cost as int) + isnull(s1.Social_Security,0))) OVER(PARTITION BY s1.job_ID ORDER BY s1.job_ID ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as acc_spent_cost, " +
                                    "(job.Estimated_Budget - sum((cast(s1.Labor_Cost as int) + cast(s1.OT_Labor_Cost as int) + cast(s1.Accommodation_Cost as int) + cast(s1.Compensation_Cost as int) + isnull(s1.Social_Security,0))) OVER(PARTITION BY s1.job_ID ORDER BY s1.job_ID ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)) as remaining_cost " +
                                    "from Progress " +
                                    "left join job on job.Job_ID = Progress.Job_ID " +
                                    "left join ( select Job_ID,Month,Year,sum(cast(Labor_Cost as int)) as Labor_Cost,sum(cast(OT_Labor_Cost as int)) as OT_Labor_Cost,sum(cast(Accommodation_Cost as int)) as Accommodation_Cost,sum(cast(Compensation_Cost as int)) as Compensation_Cost,sum(isnull(Social_Security,0)) as Social_Security " +
                                    "from Labor_Costs group by Job_ID,Year,Month) as s1 ON s1.Job_ID = Progress.Job_ID and s1.Year = Progress.Year and s1.Month = Progress.Month";

            SqlCommand cmd = new SqlCommand(str_cmd, con);
            SqlDataReader dr = cmd.ExecuteReader();

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    ProgressModel pg = new ProgressModel()
                    {
                        job_id = dr["Job_ID"] != DBNull.Value ? dr["Job_ID"].ToString() : "",
                        job_number = dr["Job_Number"] != DBNull.Value ? dr["Job_Number"].ToString() : "",
                        job_name = dr["Job_Name"] != DBNull.Value ? dr["Job_Name"].ToString() : "",
                        estimated_budget = dr["Estimated_Budget"] != DBNull.Value ? Convert.ToInt32(dr["Estimated_Budget"]) : 0,
                        job_progress = dr["Job_Progress"] != DBNull.Value ? Convert.ToInt32(dr["Job_Progress"]) : 0,
                        month = dr["Month"] != DBNull.Value ? Convert.ToInt32(dr["Month"]) : 0,
                        year = dr["Year"] != DBNull.Value ? Convert.ToInt32(dr["Year"]) : 0,
                        spent_cost = dr["acc_spent_cost"] != DBNull.Value ? Convert.ToInt32(dr["acc_spent_cost"]) : 0,
                        remainning_cost = dr["remaining_cost"] != DBNull.Value ? Convert.ToInt32(dr["remaining_cost"]) : 0
                    };
                    pgs.Add(pg);
                }
                dr.Close();
            }
            con.Close();
            return pgs;
        }

        [HttpPost]
        public void SetJobDetails(int y, string m)
        {
            year = y;
            month = m;
        }

        public JsonResult Import()
        {
            IFormFile file = Request.Form.Files[0];
            string folderName = "files";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            ipgs = new List<ProgressModel>();
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                ISheet sheet;
                string fullPath = Path.Combine(newPath, file.FileName);
                var stream = new FileStream(fullPath, FileMode.Create);
                file.CopyTo(stream);
                stream.Position = 0;
                if (sFileExtension == ".xls")
                {
                    HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats
                    sheet = hssfwb.GetSheetAt(0);
                }
                else
                {
                    XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format
                    sheet = hssfwb.GetSheetAt(0);
                }

                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;
                IRow row;
                for (int i = 1; i <= sheet.LastRowNum; i++)
                {
                    row = sheet.GetRow(i);
                    if (row == null)
                        break;
                    if (row.Cells.All(d => d.CellType == CellType.Blank))
                        break;
                    if (row.GetCell(0).StringCellValue.Trim() == "")
                        break;

                    ProgressModel ipg = new ProgressModel();
                    DateTime update_time = DateTime.Now;

                    ipg.job_id = row.GetCell(0).StringCellValue.Replace("-", String.Empty).Replace(" ", String.Empty);
                    ipg.estimated_budget = Convert.ToInt32(row.GetCell(1).NumericCellValue);
                    ipg.job_progress = Convert.ToInt32(row.GetCell(2).NumericCellValue);
                    ipg.year = year;
                    string[] months = new string[] { "", "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
                    ipg.month = Array.IndexOf(months, month);
                    ipgs.Add(ipg);
                }
            }
            return Json(ipgs);
        }

        [HttpPost]
        public JsonResult ConfirmImport()
        {
            this.DB = new ConnectDB();
            SqlConnection con = DB.Connect();
            using (SqlCommand cmd = new SqlCommand("INSERT INTO Progress(" +
                                                                "Job_ID, " +
                                                                "Job_Progress, " +
                                                                "Month, " +
                                                                "Year) " +
                                                     "VALUES(@Job_ID," +
                                                            "@Job_Progress, " +
                                                            "@Month, " +
                                                            "@Year)", con))
            {
                con.Open();
                cmd.CommandType = CommandType.Text;
                cmd.Connection = con;
                cmd.Parameters.Add("@Job_ID", SqlDbType.NVarChar);
                cmd.Parameters.Add("@Job_Progress", SqlDbType.NVarChar);
                cmd.Parameters.Add("@Month", SqlDbType.Int);
                cmd.Parameters.Add("@Year", SqlDbType.Int);

                for (int i = 0; i < ipgs.Count; i++)
                {
                    cmd.Parameters[0].Value = ipgs[i].job_id;
                    cmd.Parameters[1].Value = ipgs[i].job_progress;
                    cmd.Parameters[2].Value = ipgs[i].month;
                    cmd.Parameters[3].Value = ipgs[i].year;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }

            using (SqlCommand cmd = new SqlCommand("UPDATE Job SET Estimated_Budget = @Estimated_Budget WHERE Job_ID = @Job_ID", con))
            {
                con.Open();
                cmd.CommandType = CommandType.Text;
                cmd.Connection = con;
                cmd.Parameters.Add("@Estimated_Budget", SqlDbType.Int);
                cmd.Parameters.Add("@Job_ID", SqlDbType.NVarChar);

                for (int i = 0; i < ipgs.Count; i++)
                {
                    cmd.Parameters[0].Value = ipgs[i].estimated_budget;
                    cmd.Parameters[1].Value = ipgs[i].job_id;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
            return Json("Done");
        }
    }
}
