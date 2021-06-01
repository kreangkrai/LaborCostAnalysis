using LaborCostAnalysis.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LaborCostAnalysis.Services
{
    public class ConnectDB : IConnectDB
    {
        public SqlConnection Connect()
        {
            string connetionString;
            SqlConnection cnn;
            connetionString = @"Data Source=192.168.15.202;Initial Catalog=Manhour;User ID=sa;Password=p@ssw0rd";
            cnn = new SqlConnection(connetionString);
            return cnn;
        }
    }
}