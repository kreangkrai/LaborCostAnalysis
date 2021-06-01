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
    public class ManPowerController : Controller
    {
        IManpower Manpower;

        public ManPowerController()
        {
            this.Manpower = new ManpowerService();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetData()
        {
            List<List<ManpowerModel>> lmphs = Manpower.GetMPHModels();
            return Json(lmphs);
        }
    }
}
