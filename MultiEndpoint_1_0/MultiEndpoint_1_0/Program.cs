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

using MultiEndpoint_1_0.Properties;
using MultiEndpoint_1_0.WaterML10;
using MultiEndpoint_1_0.HealthQueryDataSetTableAdapters;


namespace MultiEndpoint_1_0
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
            sqlConn.Close();

        }
    }

    [Serializable]
    public sealed class CheckPoint
    {
        public int urlIndex;
        public int networkId;
        public int siteIndex;
        public string siteCode;

        public CheckPoint()
        {
            urlIndex = networkId = siteIndex = 0;
            siteCode = null;
        }
    }

    class Program
    {
        // Parameters to update DataValue table
        public static bool ReadFromCache = false;
        public static int CompareRecordCount = 30;
        public static int DbUpdateBatchSize = CompareRecordCount * 3;

        // Save last run and resume from last run after restart
        public static bool CPEnabled = true;
        public static CheckPoint CP;
        static string CPFile = "CheckPoint.BIN";

        // MinValueCount: only record query results for those with valueCount > MinValueCount
        static int MinValueCount = 0;

        // NoCheckHours: the minimum interval of making query 
        static int NoCheckHours = 24;

        // 
        static int LogCount = 1;
        static DateTime LastDbConnTime;
        static string LogFile;

        static HealthQueryTimeseries HQT;
        public static HealthDataValue HDV;
        static HealthSeriesCatalog HSC;
        static hiscentralSeriesCatalog HisSC;

        static void Main(string[] args)
        {
            // Refer to http://stackoverflow.com/questions/4470700/c-sharp-how-to-save-console-writeline-outputs-to-text-file
            //
            LogFile = string.Format("log-{0:MMddyy-HHmmss}.txt", DateTime.Now);
            FileStream fileStream = new FileStream(LogFile, FileMode.Create);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            streamWriter.AutoFlush = true;

            TextWriter savedCons = Console.Out;
            Console.SetOut(streamWriter);
            Console.SetError(streamWriter);

            // Checkpoint
            if (CPEnabled)
            {
                LoadCheckPoint();
                if (CP == null)
                    CP = new CheckPoint();
            }

            // Connection to hiscentral/
            string strConnHis = "MultiEndpoint_1_0.Properties.Settings.hiscentralConnectionString";
            SqlConnection sqlConnHis = GetDatabaseConnection(strConnHis);
            HisSC = new hiscentralSeriesCatalog(sqlConnHis);

            string strConnQuery = "MultiEndpoint_1_0.Properties.Settings.HealthQueryConnectionString";
            SqlConnection sqlConnHealth = GetDatabaseConnection(strConnQuery);
            HQT = new HealthQueryTimeseries(sqlConnHealth);
            HDV = new HealthDataValue(sqlConnHealth);
            HSC = new HealthSeriesCatalog(sqlConnHealth);

            // Used to calculate elapsed time before reconnecting local database
            LastDbConnTime = DateTime.Now;

            // Read HISNetworks Table and get List of networkID,NetworkName,ServiceWSDL
            string strCommHISNetworks =
                           @"SELECT NetworkID, NetworkName, ServiceWSDL 
                           FROM HISNetworks 
                           WHERE IsPublic=1 and IsApproved=1 
                                 and ServiceWSDL like '%cuahsi_1_0_%'
                                 and ServiceWSDL not like '%sdsc%' 
                                 and ServiceWSDL not like '%cuahsi.org%'";

            Network network = new Network(strCommHISNetworks, sqlConnHis);

            Int32 networkid;
            string networkname, url;

            Console.WriteLine("Found {0} Web Services from HISNetworks.", network.networknameList.Count);

            // Loop over each ServiceWSDL
            for (int i = 0; i < network.networknameList.Count; i++)
            {
                if (CPEnabled)
                {
                    if (CP.urlIndex > i)
                        continue;
                }

                networkid = network.networkidList[i];
                networkname = network.networknameList[i];
                url = network.urlList[i];
                Console.WriteLine("..............................................................");
                Console.WriteLine("Processing web service {0} of {1}: {2},{3},{4} Time {5:HH:mm:ss}.",
                    i + 1, network.networknameList.Count, networkid, networkname, url, DateTime.Now);

                if (CPEnabled)
                {
                    CP.urlIndex = i;
                    CP.networkId = networkid;
                }

                if (url.Contains("cuahsi_1_0"))
                {
                    HandleSites_1_0(url);
                }
                //else
                //{
                //    HandleSites_1_1(url);
                //}
                if (CPEnabled)
                {
                    // Remember to set siteIndex to 0 to allow to process all
                    // sites for next networks/URLs
                    CP.siteIndex = 0;
                    CP.siteCode = null;
                }

            }

//            streamWriter.Close();
            Console.WriteLine("Program finished. Switch to standard output.");
            Console.Out.Close();

            Console.SetOut(savedCons);
            Console.WriteLine("Program finished. Press any key to exit.");
            Console.ReadLine();

            File.Delete(CPFile);
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
            dbConn.Open();

            SqlConnection sqlConn;
            if (dbConn is SqlConnection)
                sqlConn = (SqlConnection)dbConn;
            else
                throw new Exception("Expected SqlConnection type, got " + dbConn.GetType().Name);

            return sqlConn;


        }

        public static void SaveCheckPoint()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream s = File.Create(CPFile))
            {
                formatter.Serialize(s, CP);
            }
        }

        public static void LoadCheckPoint()
        {
            if (!File.Exists(CPFile))
                return;
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream s = File.OpenRead(CPFile))
            {
                CP = (CheckPoint)formatter.Deserialize(s);
            }
        }

        static void CheckAndRefreshResources()
        {
            int MB = (1 << 20);
            FileInfo finfo = new FileInfo(LogFile);
            if (finfo.Length > 4.5 * MB)
            {
                string newLog;

                // Create a new log file when it is too big.
                newLog = string.Format("log-{0:MMddyy-HHmmss}-{1}.txt", DateTime.Now, LogCount++);
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

            string strConnHis = "MultiEndpoint_1_0.Properties.Settings.hiscentralConnectionString";
            Console.WriteLine("Reconnect to {0}.", strConnHis);
            HisSC.Adapter.Connection.Close();
            HisSC.Adapter.Connection = GetDatabaseConnection(strConnHis);

            string strConnQuery = "MultiEndpoint_1_0.Properties.Settings.HealthQueryConnectionString";
            Console.WriteLine("Reconnect to {0}.", strConnQuery);
            HDV.Adapter.Connection.Close();
            HDV.Adapter.Connection = GetDatabaseConnection(strConnQuery);
            HQT.Adapter.Connection = HSC.Adapter.Connection = HDV.Adapter.Connection;

            LastDbConnTime = DateTime.Now;
        }


        static WaterML10.TimeSeriesResponseType LoadDataValueInfo(string siteCode, string varCode,
            WaterML10.WaterOneFlowClient WofClient,
            DateTime beginDateTime, DateTime endDateTime,
            ref string queryParam, ref string errMsg)
        {
            string beginDT = string.Format("{0:s}", beginDateTime);
            string endDT = string.Format("{0:s}", endDateTime);

            errMsg = "";
            queryParam = String.Format(" <{0}--{1}> ", beginDT, endDT);
            Console.WriteLine("......Getting values from <{0}> to <{1}>.", beginDT, endDT);

            WaterML10.TimeSeriesResponseType tsRt = null;
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

        // Get DataValueInfo for 3 time periods: begin, middle, end.
        // As corresponding to the first, mid, and last 30 data values
        static WaterML10.TimeSeriesResponseType[] LoadDataValueInfoSplit(string siteCode, string varCode,
            WaterML10.WaterOneFlowClient WofClient,
            int valuecount, DateTime beginDateTime, DateTime endDateTime,
            ref string queryParam, ref string errMsg)
        {
            queryParam = "";
            errMsg = "";

            WaterML10.TimeSeriesResponseType[] tsRtAll = new WaterML10.TimeSeriesResponseType[3];

            string beginDT = string.Format("{0:s}", beginDateTime);
            string endDT = string.Format("{0:s}", endDateTime);
            Console.WriteLine("......Getting values from <{0}> to <{1}>.", beginDT, endDT);
            DateTime[] fromDateTime = new DateTime[3];
            DateTime[] toDateTime = new DateTime[3];
            string fromDT, toDT;
            int[] count = new int[3] { 0, 0, 0 };

            // Timespan between beginDT and endDT 
            TimeSpan delta = endDateTime - beginDateTime;
            double step0 = delta.TotalSeconds * Program.CompareRecordCount * 2 / valuecount;

            // The first 30 records
            fromDateTime[0] = beginDateTime;
            toDateTime[0] = beginDateTime.AddSeconds(step0);

            // The middle 30 records
            fromDateTime[1] = beginDateTime.AddSeconds(delta.TotalSeconds / 2);
            toDateTime[1] = fromDateTime[1].AddSeconds(step0);

            // The last 30 records
            fromDateTime[2] = endDateTime.AddSeconds(-step0);
            toDateTime[2] = endDateTime;

            for (int i = 0; i < 3; i++)
            {
                bool lastTry = false;
                tsRtAll[i] = null;
                double step = step0;
                while (!lastTry)
                {
                    if ((i < 2) && (DateTime.Compare(toDateTime[i], endDateTime) >= 0))
                    {
                        toDateTime[i] = endDateTime;
                        lastTry = true;
                    }
                    else if ((i == 2) && (DateTime.Compare(fromDateTime[i], beginDateTime) <= 0))
                    {
                        fromDateTime[i] = beginDateTime;
                        lastTry = true;
                    }

                    fromDT = string.Format("{0:s}", fromDateTime[i]);
                    toDT = string.Format("{0:s}", toDateTime[i]);

                    Console.WriteLine("......Getting values, Segment <{0}>: from <{1}> to <{2}>.",
                        i + 1, fromDT, toDT);
                    try
                    {
                        tsRtAll[i] = WofClient.GetValuesObject(siteCode, varCode, fromDT, toDT, "");
                    }
                    catch (Exception e)
                    {
                        errMsg = "Exception@LoadDataSplit: " + e.Message;
                        Console.WriteLine(errMsg);
                        return null;
                    }

                    // Where WaterML1.0 and WaterML1.1 differ
                    if (tsRtAll[i].timeSeries.values != null)
                    {
                        count[i] = Convert.ToInt32(tsRtAll[i].timeSeries.values.count);
                    }
                    if (count[i] >= Program.CompareRecordCount)
                    {
                        queryParam += String.Format(" <{0}--{1}> ", fromDT, toDT);
                        break;
                    }
                    else
                    {
                        step *= 2;
                        if (i < 2)
                            toDateTime[i] = toDateTime[i].AddSeconds(step);
                        else
                            fromDateTime[i] = fromDateTime[i].AddSeconds(-step);
                    }
                }

            }

            return tsRtAll;
        }

        static WaterML10.seriesCatalogType[] LoadOnesiteInfo(WaterML10.WaterOneFlowClient WofClient, 
            string url, string siteCode, ref string errMsg)
        {
            WaterML10.SiteInfoResponseType rt = null;
            WaterML10.seriesCatalogType[] sctAll = null;
            try
            {
                rt = WofClient.GetSiteInfoObject(siteCode, "");
                sctAll = rt.site[0].seriesCatalog;
            }
            catch (Exception e)
            {
                errMsg = "Exception@LoadOneSite: " + e.Message;
                Console.WriteLine(errMsg);
            }

            return sctAll;
        }

        static void HandleSites_1_0(string url)
        {
            string network, siteCode, varCode;
            int seriesCatalogID, newSCID;
            string queryParam = string.Empty;
            string errMsg = string.Empty;

            WaterML10.WaterOneFlowClient WofClient = new WaterOneFlowClient("WaterOneFlow", url);

            string[] blank = new string[0];
            WaterML10.SiteInfoResponseType sitesAll = null;
            try
            {
                // WaterML1.0
                sitesAll = WofClient.GetSites(blank, "");
                // WaterML1.1
                //sitesAll = WofClient.GetSitesObject(blank, "");
            }
            catch (Exception e)
            {
                errMsg = "Exception@HandleSites: " + e.Message;
                Console.WriteLine(errMsg);
            }

            if (sitesAll == null)
            {
                if (errMsg.Length == 0)
                    errMsg = "Info: no site was returned from the URL.";
                Console.WriteLine(errMsg);
                HQT.HandleQueryTimeseries("", "", -1, -1, -1, null, url, errMsg);
                return;
            }

            WaterML10.SiteInfoResponseTypeSite[] sitesRt = sitesAll.site;
            int nsites = sitesRt.Count();
            
            Console.WriteLine(".........................................");
            Console.WriteLine("Reading {0} sites from {1}", nsites, url);

            WaterML10.seriesCatalogType[] sctAll;

            #region Loop for each site
            for (int i = 0; i < nsites; i++)
            {
                if (CPEnabled)
                {
                    if (CP.siteIndex > i)
                        continue;
                }

                if (i % 100 == 0)
                {
                    // Do this check and refresh for after every 100 sites
                    CheckAndRefreshResources();
                }

                network = sitesRt[i].siteInfo.siteCode[0].network;
                siteCode = network + ":" + sitesRt[i].siteInfo.siteCode[0].Value;

                if (CPEnabled)
                {
                    CP.siteIndex = i;
                    CP.siteCode = siteCode;
                    SaveCheckPoint();
                }

                //queryParam = string.Format("QueryID = (SELECT TOP 1 QueryID FROM dbo.QueryTimeseries where SiteCode = '{0}'" +
                //    " ORDER BY QueryID DESC) and DATEDIFF(HOUR, QueryDateTime, GETDATE()) <= {1}",
                //    siteCode, NoCheckHours);
                //if (OD_Utils.Exists("dbo.QueryTimeseries", queryParam, HQT.Adapter.Connection))
                //{
                //    Console.WriteLine("..Skip Site {0} which was already checked in last {1} hours.",
                //        siteCode, NoCheckHours);
                //    continue;
                //}
                if (sitesRt[i].seriesCatalog != null)
                {
                    sctAll = sitesRt[i].seriesCatalog;
                }
                else
                {
                    sctAll = LoadOnesiteInfo(WofClient, url, siteCode, ref errMsg);
                }

                if (sctAll == null)
                {
                    if (errMsg.Length == 0)
                        errMsg = "Error: LoadOneSiteInfo() returned NULL.";
                    Console.WriteLine(errMsg);
                    HQT.HandleQueryTimeseries(siteCode, "", -1, -1, -1, null, url, errMsg);
                    continue;
                }

                if ((i + 1) % 100 == 0)
                    Console.WriteLine("..Site[{0}] {1} with {2} catalogs from {3} Time {4:HH:mm:ss}.",
                         i, siteCode, sctAll.Count(), url, DateTime.Now);
                else
                    Console.WriteLine("..Site[{0}] {1} with {2} catalogs Time {3:HH:mm:ss}.",
                        i, siteCode, sctAll.Count(), DateTime.Now);

                // network = SRBHOS, sites[i].seriesCatalog=null
                // My assumption is that 
                // Under that situation, need to get SiteInfoResponseType one site by one site
                // Here is the one example siteCode for testing
                //WaterML11.SiteInfoResponseType rt = new WaterML11.SiteInfoResponseType();
                //rt = WofClient.GetSiteInfoObject("SRBHOS:RTHNet", "");

                #region Loop for each seriesCatalogType
                for (int j = 0; j < sctAll.Count(); j++)
                {
                    int valueCount;
                    WaterML10.seriesCatalogType sct = sctAll[j];

                    if (sct.series == null)
                    {
                        errMsg = string.Format("Error: WaterML10.seriesCatalogType[{0}].series is NULL.", j);
                        HQT.HandleQueryTimeseries(siteCode, "", -1, -1, -1, null, url, errMsg);
                        continue;
                    }

                    #region Loop for each seriesCatalogTypeSeries
                    for (int k = 0; k < sct.series.Count(); k++)
                    {
                        WaterML10.seriesCatalogTypeSeries scts = sct.series[k];
                        WaterML10.TimeIntervalType ti = (WaterML10.TimeIntervalType)scts.variableTimeInterval;
                        DateTime beginDateTime = ti.beginDateTime;
                        DateTime endDateTime = ti.endDateTime;

                        string code = scts.variable.variableCode[0].Value;
                        varCode = network + ":" + code;
                        valueCount = scts.valueCount.Value;
                        if (valueCount <= MinValueCount)
                        {
                            Console.WriteLine(
                                "....Series[{0}] varCode: {1} valCount: {2}, too small, record in Query table, no further action.",
                                k, code, valueCount);
                            queryParam = String.Format(" <{0}--{1}> ", beginDateTime, endDateTime);
                            errMsg = "Info: value count is too small, no further action.";
                            HQT.HandleQueryTimeseries(siteCode, varCode, -1, -1, valueCount, null,
                                queryParam, errMsg);
                            continue;
                        }

                        // Check hiscentral/SeriesCatalog table to get seriesCatalogID.
                        // Note: beginDateTime and endDateTime are obtained from web service.
                        seriesCatalogID = newSCID = 0;
                        hiscentralDataSet.SeriesCatalogRow hscRow = HisSC.GetRow(siteCode, varCode, scts);
                        if (hscRow != null)
                        {
                            seriesCatalogID = hscRow.SeriesID;
                            if (hscRow.SeriesCode != null)
                            {
                                // Don't want to repeatedly print siteCode and network 
                                int idx = hscRow.SeriesCode.IndexOf(code);
                                code = hscRow.SeriesCode.Substring(idx);
                            }
                            Console.WriteLine("....Series[{0}] var: {1}, ID: {2}, valCount: {3}",
                                k, code, seriesCatalogID, valueCount);
                        }
                        else
                        {
                            newSCID = HSC.GetOrCreateSeriesID(siteCode, varCode, scts);
                            Console.WriteLine("....Series[{0}] var: {1}||{2}||{3}||{4}, newID: {5}, valCount: {6}",
                                k, code,
                                // Where WaterML1.1 and WaterML1.0 differ
                                scts.Method.methodID,
                                scts.Source.sourceID,
                                scts.QualityControlLevel.QualityControlLevelID,
                                newSCID, valueCount);
                        }

                        // Update HealthQuery/DataValue table if datavalue changed
                        int[] hasChanged = null;
                        try
                        {
                            if (valueCount > 5000)
                            {
                                WaterML10.TimeSeriesResponseType[] tsRtAll = null;
                                tsRtAll = LoadDataValueInfoSplit(siteCode, varCode, WofClient,
                                    valueCount, beginDateTime, endDateTime, ref queryParam, ref errMsg);

                                if (tsRtAll != null)
                                    hasChanged = HDV.HandleDataValueInfoSplit(seriesCatalogID, newSCID, tsRtAll);
                            }
                            else
                            {
                                WaterML10.TimeSeriesResponseType tsRt = null;
                                tsRt = LoadDataValueInfo(siteCode, varCode, WofClient,
                                    beginDateTime, endDateTime, ref queryParam, ref errMsg);

                                if (tsRt != null)
                                    hasChanged = HDV.HandleDataValueInfo(seriesCatalogID, newSCID, tsRt);
                            }
                        }
                        catch (Exception ex)
                        {
                            errMsg = "Exception@HandleSites1: " + ex.Message;
                            Console.WriteLine(errMsg);
                        }

                        // Add to HealthQuery/QueryTimeseries table.
                        HQT.HandleQueryTimeseries(siteCode, varCode, seriesCatalogID, newSCID,
                            valueCount, hasChanged, queryParam, errMsg);
                    }
                    #endregion Loop for each seriesCatalogTypeSeries

                    // We delay inserting records to Query table to improve performance, check and do it now
                    if (HQT.Table.Count > Program.DbUpdateBatchSize / 2)
                    {
                        HQT.Adapter.Update(HQT.Table);
                        HQT.Table.Clear();
                    }
                }
                #endregion Loop for each seriesCatalogType

            }
            #endregion Loop for each site

            if (HQT.Table.Count > 0)
            {
                HQT.Adapter.Update(HQT.Table);
                HQT.Table.Clear();
            }
        } // end of HandleSites_1_0()

    }
}
