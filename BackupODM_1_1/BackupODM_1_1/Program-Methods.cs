using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;

using BackupODM_1_1.WaterML11;
using BackupODM_1_1.OD_1_1_1DataSetTableAdapters;

namespace BackupODM_1_1
{
    class OD_Methods
    {
        // Return number of records inserted or found.
        public static int HandleMethodsInfo(SqlConnection sqlConn, MethodType m)
        {
            Console.WriteLine(">>>Parsing and inserting METHODS");

            // Check if MethodID is already in the table or not
            string cond = "MethodID = " + m.methodID;
            if (OD_Utils.Exists("Methods", cond, sqlConn))
            {
                return 1;
            }

            string sql = string.Format(@"SET IDENTITY_INSERT [Methods] ON;
INSERT INTO Methods (MethodID, MethodDescription, MethodLink)
VALUES ({0}, '{1}', '{2}');
SET IDENTITY_INSERT [Methods] OFF",
                            m.methodID,
                            m.methodDescription,
                            m.methodLink);
            return OD_Utils.RunNonQuery(sql, sqlConn);       
        }
    }
}

