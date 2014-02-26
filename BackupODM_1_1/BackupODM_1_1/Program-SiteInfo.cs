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

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;

using BackupODM_1_1.WaterML11;
using BackupODM_1_1.OD_1_1_1DataSetTableAdapters;


namespace BackupODM_1_1
{
    class OD_SiteInfo
    {
        // Return number of records inserted or found
        public static int HandleSiteInfo(SqlConnection sqlConn, SiteInfoResponseType rt)
        {
            SitesTableAdapter stAdapter = new SitesTableAdapter();
            stAdapter.Connection = sqlConn;
            SiteInfoType stinfo;
       
            OD_1_1_1DataSet.SitesDataTable tblSites = new OD_1_1_1DataSet.SitesDataTable();
            
            Console.WriteLine(">>>Parsing and inserting SITES");
            for (int i = 0; i < rt.site.Count(); i++)
            {
                stinfo = rt.site[i].siteInfo;
                try
                {
                    if (InsertOneSite(tblSites, stinfo, sqlConn))
                        stAdapter.Update(tblSites);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to insert site {0}, completed {1} of {2}: {3}.",
                        stinfo.siteCode, i, rt.site.Count(), e.Message);
                    return i;
                }
            }

            // PrintTable(stAdapter, tblSites);            

            return rt.site.Count();
        }
                
        static bool InsertOneSite( OD_1_1_1DataSet.SitesDataTable sitesTable, 
            SiteInfoType stinfo, SqlConnection sqlConn)
        {
            OD_1_1_1DataSet.SitesRow row = sitesTable.NewSitesRow();

            row.SiteCode = stinfo.siteCode[0].network + "|" + stinfo.siteCode[0].Value;
            string cond = "SiteCode = '" + row.SiteCode + "'";
            if (OD_Utils.Exists(row.Table.TableName, cond, sqlConn))
                return false;
                        
            row.SiteName = stinfo.siteName;
            LatLonPointType glt = (LatLonPointType)stinfo.geoLocation.geogLocation;
            row.Latitude = glt.latitude;
            row.Longitude = glt.longitude;
            row.LatLongDatumID = 0;
            row.Elevation_m = stinfo.elevation_m;
            if (stinfo.verticalDatum != null)
            {
                row.VerticalDatum = stinfo.verticalDatum;
            }

            row.LocalProjectionID = 0;
            
            //row.LocalX = 0;
            //row.LocalY = 0;
            //row.LocalProjectionID = 0;
            //row.PosAccuracy_m = 0;
            if (stinfo.note != null)
            {
                for (int i = 0; i < stinfo.note.Count(); i++)
                {
                    NoteType note = stinfo.note[i];
                    switch (note.title)
                    {
                        case "State":
                            row.State = note.Value;
                            break;
                        case "County":
                            row.County = note.Value;
                            break;
                        case "agency":
                            row.Comments = note.Value;
                            break;
                    }
                }
            }

            sitesTable.AddSitesRow(row);

            return true;
        }
     
        static void PrintTable(SitesTableAdapter stAdapter, OD_1_1_1DataSet.SitesDataTable dt)
        {
            stAdapter.Fill(dt);

            // Print out the column names.
            for (int curCol = 0; curCol < dt.Columns.Count; curCol++)
            {
                Console.Write(dt.Columns[curCol].ColumnName + "\t");
            }
            Console.WriteLine("\n----------------------------------");

            // Print the DataTable.
            for (int curRow = 0; curRow < dt.Rows.Count; curRow++)
            {
                for (int curCol = 0; curCol < dt.Columns.Count; curCol++)
                {
                    Console.Write(dt.Rows[curRow][curCol].ToString() + "\t");
                }
                Console.WriteLine();
            }
        }
    }

}
