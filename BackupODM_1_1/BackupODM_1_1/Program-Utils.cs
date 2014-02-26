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
using System.Runtime.Serialization.Formatters.Binary;

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;

using BackupODM_1_1.WaterML11;

namespace BackupODM_1_1
{
    class OD_Utils
    {

        public static bool Exists(string table, string cond,
            SqlConnection sqlConn, SqlTransaction sqlTrans = null)
        {
            bool ret = false;

            int count = GetPrimaryKey(table, "COUNT(*)", cond, sqlConn, sqlTrans);

            if (count > 0)
                ret = true;

            return ret;
        }

        public static int GetPrimaryKey(string table, string col, string cond,
            SqlConnection sqlConn, SqlTransaction sqlTrans = null)
        {
            int ret = -1;

            SqlCommand cmd = new SqlCommand("SELECT " + col + " FROM " + table + " WHERE " + cond, sqlConn, sqlTrans);
            Object obj = cmd.ExecuteScalar();

            if (obj != null)
            {
                ret = int.Parse(obj.ToString());
            }

            return ret;
        }

        public static int RunNonQuery(string sql, SqlConnection sqlConn, SqlTransaction sqlTrans = null)
        {
            SqlCommand cmd = new SqlCommand(sql, sqlConn, sqlTrans);
            return cmd.ExecuteNonQuery();
        }

        public static SiteInfoResponseType ReadSiteInfoObject(string fileName)
        {
            if (!File.Exists(fileName))
                return null;

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream s = File.OpenRead(fileName))
            {
                SiteInfoResponseType rt = (SiteInfoResponseType)formatter.Deserialize(s);
                return rt;
            }
        }

        public static void WriteObject<T>(string fileName, T rt)
        {
            if (File.Exists(fileName))
                return;

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream s = File.Create(fileName))
            {
                formatter.Serialize(s, rt);
            }
        }


        public static void SaveXml<T>(string fileName, T rt)
        {
            if (File.Exists(fileName))
                return;

            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (FileStream s = File.Create(fileName))
            {
                xs.Serialize(s, rt);
            }
        }

        public static T LoadXml<T>(string fileName)
        {
            T rt = default(T);

            if (!File.Exists(fileName))
                return rt;

            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (FileStream s = File.OpenRead(fileName))
            {
                rt = (T) xs.Deserialize(s);
            }

            return rt;
        }

        // Copied from http://www.wackylabs.net/2006/06/getting-the-xmlenumattribute-value-for-an-enum-field/
        public static string ConvertToString(Enum e)
        {
            // Get the Type of the enum
            Type t = e.GetType();
            string str = e.ToString("G");

            // Get the FieldInfo for the member field with the enums name
            FieldInfo info = t.GetField(str);

            // Check to see if the XmlEnumAttribute is defined on this field
            if (!info.IsDefined(typeof(XmlEnumAttribute), false))
            {
                // If no XmlEnumAttribute then return the string version of the enum.
                return str;
            }

            // Get the XmlEnumAttribute
            object[] o = info.GetCustomAttributes(typeof(XmlEnumAttribute), false);
            XmlEnumAttribute att = (XmlEnumAttribute)o[0];
            return att.Name;
        }

        public static void Exit(int code)
        {                     
            Console.WriteLine("\nPress any key to finish.\n");
            Console.ReadLine();
            Environment.Exit(code);
        }


        /* From Units table
UnitsID	UnitsName	UnitsType	UnitsAbbreviation
100	second	Time	s
101	millisecond	Time	millisec
102	minute	Time	min
103	hour	Time	hr
104	day	Time	d
105	week	Time	week
106	month	Time	month
107	common year (365 days)	Time	yr
108	leap year (366 days)	Time	leap yr
109	Julian year (365.25 days)	Time	jul yr
110	Gregorian year (365.2425 days)	Time	greg yr
181	hour minute	Time	hhmm
182	year month day	Time	yymmdd
183	year day (Julian)	Time	yyddd
300	month year	Time	mmyy
         */

        public static DateTime GetDateTime(DateTime dateTime, int unitID, int intervals)
        {
            // Assume the time unit ID does not change in Units table.
            // Ideally, we should get the interpretion of unitID from Units table
            switch (unitID)
            {
                case 100:
                    return dateTime.AddSeconds(intervals);
                case 101:
                    return dateTime.AddMilliseconds(intervals);
                case 102:
                    return dateTime.AddMinutes(intervals);
                case 103:
                    return dateTime.AddHours(intervals);
                case 104:
                    return dateTime.AddDays(intervals);
                case 105:
                    return dateTime.AddDays(7 * intervals);
                case 106:
                    return dateTime.AddMonths(intervals);
                case 107:
                    return dateTime.AddDays(365 * intervals);
                case 108:
                    return dateTime.AddDays(366 * intervals);
                case 109:
                    return dateTime.AddDays(365.25 * intervals);
                case 110:
                    return dateTime.AddDays(365.2425 * intervals);
                default:
                    Console.WriteLine("GetDateTime: unsupported time unit ID {0}.", unitID);
                    return dateTime;
            }
        }

        // End of class
    }
}
