using LaborCostAnalysis.Interfaces;
using LaborCostAnalysis.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaborCostAnalysis.Controllers
{
    public class NormalOvertimeController : Controller
    {
        INormalOvertime NormalOvertime;

        public NormalOvertimeController()
        {
            this.NormalOvertime = new NormalOvertimeService();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetData()
        {
            return Json(NormalOvertime.NormalPerOvertime());
        }
    }
}
