using LaborCostAnalysis.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaborCostAnalysis.Services
{
    public class ConnectToDB
    {
       
        IConnectDB connect;
        public ConnectToDB()
        {
            this.connect = new ConnectDB();
        }
    }
}
