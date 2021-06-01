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
using System.Text;
using System.Threading.Tasks;

namespace LaborCostAnalysis.Controllers
{
    public class ImportLaborCostController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        static List<LaborCostModel> jobs;
        IConnectDB DB;

        public ImportLaborCostController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public JsonResult ImportSpent()
        {
            IFormFile file = Request.Form.Files[0];
            string folderName = "files";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            StringBuilder sb = new StringBuilder();
            jobs = new List<LaborCostModel>();
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
                    sheet = hssfwb.GetSheetAt(1);
                }
                else
                {
                    XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format
                    sheet = hssfwb.GetSheetAt(1);
                }
                IRow headerRow = sheet.GetRow(2);
                int cellCount = headerRow.LastCellNum;
                IRow row;
                for (int i = 3; i <= sheet.LastRowNum; i++)
                {
                    row = sheet.GetRow(i);
                    if (row.GetCell(8).StringCellValue == "")
                        break;
                    if (row == null)
                        break;
                    if (row.Cells.All(d => d.CellType == CellType.Blank))
                        break;
                    if (row.Cells.All(c => c.NumericCellValue == 0))
                        break;

                    string str_job_id = row.GetCell(8).StringCellValue.Trim();
                    str_job_id = str_job_id.Replace("-", String.Empty).Replace(" ", String.Empty);

                    LaborCostModel job = new LaborCostModel()
                    {
                        job_id = str_job_id,
                        job_name = row.GetCell(9).StringCellValue,
                        week = Convert.ToInt32(row.GetCell(5).NumericCellValue),
                        month = Convert.ToInt32(row.GetCell(6).NumericCellValue),
                        year = Convert.ToInt32(row.GetCell(7).NumericCellValue),
                        week_time = row.GetCell(10).StringCellValue,
                        labor_cost = Convert.ToInt32(row.GetCell(11).NumericCellValue),
                        ot_cost = Convert.ToInt32(row.GetCell(12).NumericCellValue),
                        accomodate = Convert.ToInt32(row.GetCell(13).NumericCellValue),
                        compensate = Convert.ToInt32(row.GetCell(14).NumericCellValue),
                        social_security = Convert.ToInt32(row.GetCell(15).NumericCellValue),
                        number_of_labor = Convert.ToInt32(row.GetCell(16).NumericCellValue)
                    };
                    jobs.Add(job);
                }
            }
            return Json(jobs);
        }

        [HttpPost]
        public JsonResult ConfirmUpload()
        {

            this.DB = new ConnectDB();
            SqlConnection con = DB.Connect();
            con.Open();
            List<LaborCostModel> Job_ID = new List<LaborCostModel>();
            string Job_cmd = "select Job_ID from Job";
            SqlCommand cmd_Job = new SqlCommand(Job_cmd, con);
            SqlDataReader dr_Job = cmd_Job.ExecuteReader();
            if (dr_Job.HasRows)
            {
                while (dr_Job.Read())
                {
                    LaborCostModel j = new LaborCostModel()
                    {
                        job_id = dr_Job["Job_ID"].ToString().Trim()
                    };
                    Job_ID.Add(j);
                }
                dr_Job.Close();
            }
            con.Close();

            List<string> v1 = Job_ID.Select(s => s.job_id).ToList();
            List<string> v2 = jobs.Select(s => s.job_id).Distinct().ToList();
            var diff_Job = v2.Except(v1).ToList();
            if (diff_Job.Count <= 0)
            {
                string str_cmd = "select Job_ID, " +
                                        "Week, " +
                                        "Month, " +
                                        "Year, " +
                                        "Week_time, " +
                                        "isnull(NULLIF(Labor_Cost,''),0)as Labor_Cost, " +
                                        "isnull(NULLIF(OT_Labor_Cost,''),0) as OT_Labor_Cost , " +
                                        "isnull(NULLIF(Accommodation_Cost,''),0) as Accommodation_Cost, " +
                                        "isnull(NULLIF(Compensation_Cost,''),0) as Compensation_Cost, " +
                                        "Social_Security, " +
                                        "isnull(NULLIF(No_Of_Labor_Week,''),0) as No_Of_Labor_Week " +
                                        "from Labor_Costs";

                con.Open();
                SqlCommand cmd = new SqlCommand(str_cmd, con);
                SqlDataReader dr = cmd.ExecuteReader();

                List<LaborCostModel> uploaded_jobs = new List<LaborCostModel>();
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        LaborCostModel job = new LaborCostModel()
                        {
                            job_id = dr["Job_ID"].ToString(),
                            week = Convert.ToInt32(dr["Week"]),
                            month = Convert.ToInt32(dr["Month"]),
                            year = Convert.ToInt32(dr["Year"]),
                            week_time = Convert.ToString(dr["Week_Time"]),
                            labor_cost = Convert.ToInt32(dr["Labor_Cost"]),
                            ot_cost = Convert.ToInt32(dr["OT_Labor_Cost"]),
                            accomodate = Convert.ToInt32(dr["Accommodation_Cost"]),
                            compensate = Convert.ToInt32(dr["Compensation_Cost"]),
                            social_security = Convert.ToInt32(dr["Social_Security"]),
                            number_of_labor = Convert.ToInt32(dr["No_Of_Labor_Week"])
                        };
                        uploaded_jobs.Add(job);
                    }
                    dr.Close();
                }
                con.Close();
                var dif = jobs.Where(w => !uploaded_jobs.Any(y => y.job_id == w.job_id && y.week == w.week && y.month == w.month && y.year == w.year)).ToList();

                using (SqlCommand cmd3 = new SqlCommand("INSERT INTO Labor_Costs(" +
                                                                "Job_ID, " +
                                                                "Week, " +
                                                                "Month, " +
                                                                "Year, " +
                                                                "Week_time, " +
                                                                "Labor_Cost, " +
                                                                "OT_Labor_Cost, " +
                                                                "Accommodation_Cost, " +
                                                                "Compensation_Cost, " +
                                                                "Social_Security, " +
                                                                "No_Of_Labor_Week) " +
                                                     "VALUES(@Job_ID," +
                                                            "@Week, " +
                                                            "@Month, " +
                                                            "@Year, " +
                                                            "@Week_time, " +
                                                            "@Labor_Cost, " +
                                                            "@OT_Labor_Cost, " +
                                                            "@Accommodation_Cost, " +
                                                            "@Compensation_Cost, " +
                                                            "@Social_Security, " +
                                                            "@No_Of_Labor_Week)", con))
                {
                    con.Open();
                    cmd3.CommandType = CommandType.Text;
                    cmd3.Connection = con;
                    cmd3.Parameters.Add("@Job_ID", SqlDbType.NVarChar);
                    cmd3.Parameters.Add("@Week", SqlDbType.Int);
                    cmd3.Parameters.Add("@Month", SqlDbType.Int);
                    cmd3.Parameters.Add("@Year", SqlDbType.Int);
                    cmd3.Parameters.Add("@Week_time", SqlDbType.NVarChar);
                    cmd3.Parameters.Add("@Labor_Cost", SqlDbType.NVarChar);
                    cmd3.Parameters.Add("@OT_Labor_Cost", SqlDbType.NVarChar);
                    cmd3.Parameters.Add("@Accommodation_Cost", SqlDbType.NVarChar);
                    cmd3.Parameters.Add("@Compensation_Cost", SqlDbType.NVarChar);
                    cmd3.Parameters.Add("@Social_Security", SqlDbType.Int);
                    cmd3.Parameters.Add("@No_Of_Labor_Week", SqlDbType.NVarChar);

                    for (int i = 0; i < dif.Count; i++)
                    {
                        cmd3.Parameters[0].Value = dif[i].job_id;
                        cmd3.Parameters[1].Value = dif[i].week;
                        cmd3.Parameters[2].Value = dif[i].month;
                        cmd3.Parameters[3].Value = dif[i].year;
                        cmd3.Parameters[4].Value = dif[i].week_time;
                        cmd3.Parameters[5].Value = dif[i].labor_cost;
                        cmd3.Parameters[6].Value = dif[i].ot_cost;
                        cmd3.Parameters[7].Value = dif[i].accomodate;
                        cmd3.Parameters[8].Value = dif[i].compensate;
                        cmd3.Parameters[9].Value = dif[i].social_security;
                        cmd3.Parameters[10].Value = dif[i].number_of_labor;
                        cmd3.ExecuteNonQuery();
                    }
                }
                con.Close();
                return Json("Done");
            }
            else
            {
                return Json(diff_Job);
            }
        }
    }
}
