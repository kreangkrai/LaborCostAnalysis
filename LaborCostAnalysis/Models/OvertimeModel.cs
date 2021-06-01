using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaborCostAnalysis.Models
{
    public class OvertimeModel
    {
        public string job_id { get; set; }

        public string employee_id { get; set; }

        public int ot_1_5 { get; set; }

        public int ot_3 { get; set; }

        public int ot_sum { get; set; }

        public int week { get; set; }

        public int month { get; set; }

        public DateTime recording_time { get; set; }
    }
}
