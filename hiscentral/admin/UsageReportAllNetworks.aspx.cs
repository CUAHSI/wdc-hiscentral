﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class admin_UsageReportAllNetworks : System.Web.UI.Page {
    public class StatisticModel {
        public string Date { get; set; }
        public string Count { get; set; }
    }

    public class CSVModel {
        public string Date { get; set; }
        public string Stat1 { get; set; }
        public string Stat2 { get; set; }
        public string Stat3 { get; set; }
    }

    public string NetworkName = String.Empty;
    protected void LnkDownload_Click(object sender, EventArgs e) {
        var stat1 = DataAccess_Logging.GetStat1(NetworkName, String.Empty);
        var stat2 = DataAccess_Logging.GetStat2(NetworkName, String.Empty);
        var stat3 = DataAccess_Logging.GetStat3(NetworkName, String.Empty);

        var list1 = new List<StatisticModel>();
        stat1.ForEach(x => {
            var splitString = x.Split(',');
            var date = splitString[0].Split(' ');
            list1.Add(new StatisticModel { Count = splitString[1], Date = date[0] });
        });

        var list2 = new List<StatisticModel>();
        stat1.ForEach(x => {
            var splitString = x.Split(',');
            var date = splitString[0].Split(' ');
            list2.Add(new StatisticModel { Count = splitString[1], Date = date[0] });
        });

        var list3 = new List<StatisticModel>();
        stat1.ForEach(x => {
            var splitString = x.Split(',');
            var date = splitString[0].Split(' ');
            list3.Add(new StatisticModel { Count = splitString[1], Date = date[0] });
        });


    }
    protected void Page_Load(object sender, EventArgs e) {
        var stat1 = DataAccess_Logging.GetStat1(String.Empty);
        var stat2 = DataAccess_Logging.GetStat2(String.Empty);
        var stat3 = DataAccess_Logging.GetStat3(String.Empty);

        var list1 = new List<StatisticModel>();
        stat1.ForEach(x => {
            var splitString = x.Split(',');
            var date = splitString[0].Split(' ');
            list1.Add(new StatisticModel { Count = splitString[1], Date = date[0] });
        });

        var list2 = new List<StatisticModel>();
        stat2.ForEach(x => {
            var splitString = x.Split(',');
            var date = splitString[0].Split(' ');
            list2.Add(new StatisticModel { Count = splitString[1], Date = date[0] });
        });

        var list3 = new List<StatisticModel>();
        stat3.ForEach(x => {
            var splitString = x.Split(',');
            var date = splitString[0].Split(' ');
            list3.Add(new StatisticModel { Count = splitString[1], Date = date[0] });
        });

        rptStat1.DataSource = list1;
        rptStat1.DataBind();
        rptStat2.DataSource = list2;
        rptStat2.DataBind();
        rptStat3.DataSource = list3;
        rptStat3.DataBind();

        var csvList = new List<CSVModel>();
        csvList.Add(new CSVModel { Date = "Date", Stat1 = "Clients per Month", Stat2 = "Requests per Month", Stat3 = "Values per Month" });
        list1.ForEach(x => {
            csvList.Add(new CSVModel { Date = x.Date, Stat1 = x.Count });
        });
        list2.ForEach(x => {
            var itemToUpdate = csvList.SingleOrDefault(y => y.Date == x.Date);
            if (itemToUpdate != null) {
                itemToUpdate.Stat2 = x.Count;
            }
        });
        list3.ForEach(x => {
            var itemToUpdate = csvList.SingleOrDefault(y => y.Date == x.Date);
            if (itemToUpdate != null) {
                itemToUpdate.Stat3 = x.Count;
            }
        });


        Configuration conf = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(Request.ApplicationPath);   //"/CentralHIS2"
        if (conf.AppSettings.Settings["UsageReportFolder"] == null) throw new Exception("Unable to find UsageReportFolder in AppSettings");
        string folder = conf.AppSettings.Settings["UsageReportFolder"].Value;
        string virtualDirectory = Request.ApplicationPath == "/" ? "/" : Request.ApplicationPath + "/";
        var fileName = "usagereportallnetworks-" + Guid.NewGuid().ToString() + ".csv";
        
        System.IO.File.WriteAllLines(folder + "/ReportStat/" + fileName, csvList.Select(x => x.Date + "," + x.Stat1 + "," + x.Stat2 + "," + x.Stat3));
        lnkDownload.NavigateUrl = "/ReportStat/" + fileName; ;

        
    }
}