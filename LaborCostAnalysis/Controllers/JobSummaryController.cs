using LaborCostAnalysis.Interfaces;
using LaborCostAnalysis.Models;
using LaborCostAnalysis.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaborCostAnalysis.Controllers
{
    public class JobSummaryController : Controller
    {
        IJobSummary JobSummary;

        public JobSummaryController()
        {
            this.JobSummary = new JobSummaryService();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetJobsSummary()
        {
            List<JobSummaryModel> jobs = JobSummary.GetJobsSummary();
            return Json(jobs);
        }
    }
}
