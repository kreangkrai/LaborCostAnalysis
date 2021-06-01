using LaborCostAnalysis.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaborCostAnalysis.Interfaces
{
    interface ISpentPerWeek
    {
        List<SpentPerWeekModel> GetSpentCostPerWeeks();
        List<SpentPerWeekModel> GetSpentPerWeeksByJob(string job_id);
    }
}
ddd
