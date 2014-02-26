using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Configuration;

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

using BackupODM_1_1.Properties;
using BackupODM_1_1.WaterML11;
using BackupODM_1_1.OD_1_1_1DataSetTableAdapters;

namespace BackupODM_1_1
{
    class Network
    {
        public List<string> networknameList { get; set; }
        public List<string> urlList { get; set; }
        public List<int> networkidList { get; set; }

        public Network(string strComm, SqlConnection sqlConn)
        {
            networknameList = new List<string>();
            urlList = new List<string>();
            networkidList = new List<int>();

            SqlCommand sqlComm = new SqlCommand(strComm, sqlConn);
            SqlDataReader dr;
            try
            {
                using (dr = sqlComm.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        networkidList.Add(Convert.ToInt32(dr["NetworkID"].ToString()));
                        networknameList.Add(dr["NetworkName"].ToString());
                        urlList.Add(dr["ServiceWSDL"].ToString());
                    }
                }
            dr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception@Network: {0}", e.Message);
            }

        }
    }

    class Program
    {
        public static int DbUpdateBatchSize = 30; 
        static string LogFile;
        static int LogCount = 0;
        static DateTime LastDbConnTime;
        static string AppPath;
        static SqlConnection SqlConnOD;
        static OD_SeriesCatalog HisSC;
        static string strConnOD = "BackupODM_1_1.Properties.Settings.OD_1_1_1ConnectionString";
        static string strConnHis = "BackupODM_1_1.Properties.Settings.hiscentralConnectionString";

        static void Main(string[] args)
        {
            AppPath = AppDomain.CurrentDomain.BaseDirectory;

            // Refer to http://stackoverflow.com/questions/4470700/c-sharp-how-to-save-console-writeline-outputs-to-text-file
            LogFile = string.Format("{0}\\log-{1:MMddyy-HHmmss}-{2}.txt", AppPath, DateTime.Now, LogCount++);

            FileStream fileStream = new FileStream(LogFile, FileMode.Create);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            streamWriter.AutoFlush = true;

            TextWriter savedCons = Console.Out;
            Console.SetOut(streamWriter);
            Console.SetError(streamWriter);

            // Connection to OD
            SqlConnOD = GetDatabaseConnection(strConnOD);

            // Connection to hiscentral
            SqlConnection sqlConnHis = GetDatabaseConnection(strConnHis);
            HisSC = new OD_SeriesCatalog(sqlConnHis);

            LastDbConnTime = DateTime.Now;

            // Read HISNetworks Table and get List of networkID,NetworkName,ServiceWSDL
            string strCommHISNetworks =
                           @"SELECT NetworkID, NetworkName, ServiceWSDL 
                           FROM HISNetworks 
                           WHERE IsPublic=1 and IsApproved=1 
                                 and ServiceWSDL like '%cuahsi_1_1_%'
                                 and ServiceWSDL not like '%sdsc%' 
                                 and ServiceWSDL not like '%cuahsi.org%'";

            Network network = new Network(strCommHISNetworks, sqlConnHis);

            int networkid;
            string networkname, url;

            Console.WriteLine("Found {0} Web Services from HISNetworks.", network.networknameList.Count);

            // Loop over each ServiceWSDL
            // Skip first one (http://eddy.ccny.cuny.edu/CIMS/cuahsi_1_1.asmx?WSDL) which always failed with
            // Exception@HandleSites: Cannot retrieve units or variables from database
            for (int i = 1; i < network.networknameList.Count; i++)
            {
                networkid = network.networkidList[i];
                networkname = network.networknameList[i];
                url = network.urlList[i];
                Console.WriteLine(" ");
                Console.WriteLine("..............................................................");
                Console.WriteLine("Processing web service {0} of {1}: {2},{3},{4} Time {5:HH:mm:ss}.",
                    i + 1, network.networknameList.Count, networkid, networkname, url, DateTime.Now);

                if (url.Contains("cuahsi_1_1"))
                {
                    HandleSites_1_1(url, SqlConnOD);
                }

            }

            Console.Out.Close();

            Console.SetOut(savedCons);

            Console.WriteLine("Program finished. Press any key to exit.");
            Console.ReadLine();

        }

        static SqlConnection GetDatabaseConnection(string connStr)
        {
            // Get Connection string/provider from *.config.
            ConnectionStringSettings connSet =
                 ConfigurationManager.ConnectionStrings[connStr];

            // Get the factory provider.
            DbProviderFactory dbFact = DbProviderFactories.GetFactory(connSet.ProviderName);

            // Now make connection object.
            DbConnection dbConn = dbFact.CreateConnection();
            dbConn.ConnectionString = connSet.ConnectionString;
            if (dbConn.State == System.Data.ConnectionState.Open)
            {
                dbConn.Close();
                dbConn.Dispose();
            }
            dbConn.Open();
            Console.WriteLine("Conn State: {0}", dbConn.State);
            Console.WriteLine("ConnectionTimeout: {0}", dbConn.ConnectionTimeout);

            SqlConnection sqlConn;
            if (dbConn is SqlConnection)
                sqlConn = (SqlConnection)dbConn;
            else
                throw new Exception("Expected SqlConnection type, got " + dbConn.GetType().Name);

            return sqlConn;
        }

        static void CheckAndRefreshResources()
        {
            int MB = (1 << 20);
            FileInfo finfo = new FileInfo(LogFile);
            if (finfo.Length > 4.5 * MB)
            {
                string newLog;

                // Create a new log file when it is too big.
                newLog = string.Format("{0}\\log-{1:MMddyy-HHmmss}-{2}.txt", AppPath, DateTime.Now, LogCount++);
                Console.WriteLine("************** Switch log file to {0} ***************", newLog);
                Console.Out.Close();

                FileStream fileStream = new FileStream(newLog, FileMode.Create);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                streamWriter.AutoFlush = true;

                Console.SetOut(streamWriter);
                Console.SetError(streamWriter);
                Console.WriteLine("************** Continue log from previous {0} ***************", LogFile);
                LogFile = newLog;
            }

            // Reconnect database server if needed
            TimeSpan diff = DateTime.Now - LastDbConnTime;
            if (diff.TotalMinutes < 60)
                return;

            Console.WriteLine("Reconnect to {0}.", strConnOD);
            SqlConnOD.Close();
            SqlConnOD = GetDatabaseConnection(strConnOD);

            Console.WriteLine("Reconnect to {0}.", strConnHis);
            HisSC.Adapter.Connection.Close();
            HisSC.Adapter.Connection = GetDatabaseConnection(strConnHis);

            LastDbConnTime = DateTime.Now;
        }


        static WaterML11.TimeSeriesResponseType LoadDataValueInfo(string siteCode, string varCode,
    WaterML11.WaterOneFlowClient WofClient,
    DateTime beginDateTime, DateTime endDateTime)
        {
            string errMsg = "";
            string beginDT = string.Format("{0:s}", beginDateTime);
            string endDT = string.Format("{0:s}", endDateTime);

            WaterML11.TimeSeriesResponseType tsRt = null;
            try
            {
                tsRt = WofClient.GetValuesObject(siteCode, varCode, beginDT, endDT, "");
            }
            catch (Exception e)
            {
                errMsg = "Exception@LoadData: " + e.Message;
                Console.WriteLine(errMsg);
            }

            return tsRt;
        }

        static WaterML11.seriesCatalogType[] LoadOnesiteInfo(WaterML11.WaterOneFlowClient WofClient,
    string url, string siteCode, ref string errMsg)
        {
            WaterML11.SiteInfoResponseType rt = null;
            WaterML11.seriesCatalogType[] sctAll = null;
            errMsg = "";
            try
            {
                rt = WofClient.GetSiteInfoObject(siteCode, "");
                sctAll = rt.site[0].seriesCatalog;
            }
            catch (Exception e)
            {
                errMsg = "Exception@LoadOneSite: " + e.Message;
            }

            return sctAll;
        }

        static void HandleSites_1_1(string url, SqlConnection sqlConn)
        {
            string network, siteCode, varCode;
            string queryParam = string.Empty;
            string errMsg = string.Empty;
            int count;

            WaterML11.WaterOneFlowClient WofClient = new WaterOneFlowClient("WaterOneFlow", url);

            string[] blank = new string[0];
            WaterML11.SiteInfoResponseType siteRt = null;
            try
            {
                siteRt = WofClient.GetSitesObject(blank, "");
            }
            catch (Exception e)
            {
                errMsg = "Exception@HandleSites: " + e.Message;
                Console.WriteLine(errMsg);
            }

            if (siteRt == null)
            {
                if (errMsg.Length == 0)
                    errMsg = "Info: no site was returned from the URL.";
                Console.WriteLine(errMsg);
                return;
            }

            // Write into "Sites"
            //siteRt = WofClient.GetSiteInfoObject(siteCode, "");
            if (OD_SiteInfo.HandleSiteInfo(sqlConn, siteRt) == 0)
                OD_Utils.Exit(1);

            int nsites = siteRt.site.Count();
            Console.WriteLine("Reading {0} sites from {1}..", nsites, url);

            WaterML11.seriesCatalogType[] sctAll;
            OD_SeriesCatalog odSC = new OD_SeriesCatalog(sqlConn);

            #region Loop for each site
            for (int i = 0; i < nsites; i++)
            {

                network = siteRt.site[i].siteInfo.siteCode[0].network;
                siteCode = network + ":" + siteRt.site[i].siteInfo.siteCode[0].Value;

                if (i % 10 == 0)
                {
                    // Do this check and refresh for after every 100 sites
                    CheckAndRefreshResources();
                    sqlConn = SqlConnOD;
                    odSC.Adapter.Connection = sqlConn;
                }

                sctAll = LoadOnesiteInfo(WofClient, url, siteCode, ref errMsg);

                if (sctAll == null)
                {
                    if (errMsg.Length == 0)
                        errMsg = "Error: LoadOneSiteInfo() returned NULL.";
                    Console.WriteLine(errMsg);
                    continue;
                }


                if ((i + 1) % 50 == 0)
                    Console.WriteLine("..Site[{0}] {1} with {2} catalogs from {3} Time {4:HH:mm:ss}.",
                         i, siteCode, sctAll.Count(), url, DateTime.Now);
                else
                    Console.WriteLine("..Site[{0}] {1} with {2} catalogs Time {3:HH:mm:ss}.",
                        i, siteCode, sctAll.Count(), DateTime.Now);

                string sourceOrg = null;
                int methodID = 0;
                #region Loop for each seriesCatalogType
                for (int j = 0; j < sctAll.Count(); j++)
                {
                    WaterML11.seriesCatalogType sct = sctAll[j];

                    if (sct.series == null)
                    {
                        errMsg = string.Format("Error: WaterML11.seriesCatalogType[{0}].series is NULL.", j);
                        continue;
                    }

                    #region Loop for each seriesCatalogTypeSeries
                    for (int k = 0; k < sct.series.Count(); k++)
                    {
                        WaterML11.seriesCatalogTypeSeries scts = sct.series[k];
                        WaterML11.TimeIntervalType ti = (WaterML11.TimeIntervalType)scts.variableTimeInterval;
                        DateTime beginDateTime = ti.beginDateTime;
                        DateTime endDateTime = ti.endDateTime;

                        string code = scts.variable.variableCode[0].Value;
                        varCode = network + ":" + code;
                        //valueCount = scts.valueCount.Value;

                        Console.WriteLine("");
                        Console.WriteLine("....Series[{0}] var: {1} of site[{2}]", k, code, i);
                    
                        // Use variable info from siteInfoResponseType
                        // instead of from variablesResponseType
                        // since the former includes the latter and contains more infomration
                        if (OD_VariableInfo.HandleVariableInfo(sqlConn, scts.variable) == 0)
                        {
                            Console.WriteLine("Failed to insert variable. Give up!");
                            continue;
                        }

                        // Add to SeriesCatalog table if not there
                        // Check SeriesCatalog table to get the newest data time
                        OD_1_1_1DataSet.SeriesCatalogRow scRow = odSC.GetOrCreateSeriesCatalog(
                            siteRt.site[i].siteInfo.siteCode[0].Value,
                            scts.variable.variableCode[0].Value,
                            scts);

                        if (scRow.EndDateTime >= ti.endDateTime)
                        {
                            Console.WriteLine("No further action since database has most recent ending date time.");
                            Console.WriteLine("Web service reported {0} values from <{1}> to <{2}>.",
                                scts.valueCount.Value, ti.beginDateTime.ToString(), ti.endDateTime.ToString());
                            Console.WriteLine("Database has {0} values from <{1}> to <{2}>.",
                                scRow.ValueCount, scRow.BeginDateTime.ToString(),
                                scRow.EndDateTime.ToString());
                            if (ti.beginDateTime != scRow.BeginDateTime)
                            {
                                Console.WriteLine("WARNING: Web server has older data not in database! Please double check!");
                            }
                            if (scts.valueCount.Value != scRow.ValueCount)
                            {
                                Console.WriteLine("WARNING: data value counts mismatch, maybe due to duplicate values from web service!");
                            }
                            continue;
                        }

                        // Should we use UTC time?
                        if (scRow.ValueCount > 0)
                        {
                            beginDateTime = OD_Utils.GetDateTime(
                                scRow.EndDateTime, scRow.TimeUnitsID, 1);
                        }

                        // Update DataValue table 
                        //try
                        //{
                                WaterML11.TimeSeriesResponseType tsrt = null;

                                Console.WriteLine("......Getting {0} values from <{1}> to <{2}>",
                                    scts.valueCount.Value, beginDateTime, endDateTime);      
                                tsrt = LoadDataValueInfo(siteCode, varCode, WofClient,
                                    beginDateTime, endDateTime);

                                if (tsrt == null)
                                    continue;

                                if ((sourceOrg == null) || (!string.Equals(sourceOrg, scts.source.organization)))
                                {
                                    // Insert a new source
                                    sourceOrg = scts.source.organization;
                                    OD_SourceInfo.HandleSourceInfo(sqlConn,
                                        siteRt.site[i].siteInfo, scts, tsrt.timeSeries[0]);
                                }

                                //if ((methodCode == null) || (!string.Equals(methodCode, scts.method.methodCode)))
                                if ((methodID == 0) || (methodID != scts.method.methodID))
                                {
                                    // Insert a new method
                                    methodID = scts.method.methodID;
                                    OD_Methods.HandleMethodsInfo(sqlConn, scts.method);
                                }
                        
                                count = OD_DataValues.HandleDataValueInfo(sqlConn,
                                    odSC, scRow, siteRt.site[i].siteInfo, scts, tsrt);
                                Console.WriteLine("       -------->>>>>>> Inserted {0} records. Database has {1} values from <{2}> to <{3}>.",
                                    count, scRow.ValueCount, scRow.BeginDateTime.ToString(),
                                    scRow.EndDateTime.ToString());

                        //}
                        //catch (Exception ex)
                        //{
                        //    errMsg = "Exception@HandleDataValueInfo: " + ex.Message;
                        //    Console.WriteLine(errMsg);
                        //}

                    }
                    #endregion Loop for each seriesCatalogTypeSeries

                }
                #endregion Loop for each seriesCatalogType

            }
            #endregion Loop for each site

        } // end of HandleSites_1_1()


    }
}
