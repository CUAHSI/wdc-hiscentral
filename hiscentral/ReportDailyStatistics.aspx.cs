using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class ReportDailyStatistics : System.Web.UI.Page {
    public class StatisticModel {
        public string Date { get; set; }
        public string Count { get; set; }

        public DateTime GetActualDate() {
            DateTime d = new DateTime();
            DateTime.TryParse(Date, out d);
            return d;
        }
    }

    public class CSVModel {
        public string Date { get; set; }
        public string Stat1 { get; set; }
        public string Stat2 { get; set; }
        public string Stat3 { get; set; }
    }

    public string NetworkName = String.Empty;
    protected void LnkDownload_Click(object sender, EventArgs e) {
        var stat1 = DataAccess_Logging.GetStat1(NetworkName);
        var stat2 = DataAccess_Logging.GetStat2(NetworkName);
        var stat3 = DataAccess_Logging.GetStat3(NetworkName);

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
        var networkName = Server.UrlDecode(Request.QueryString["n"]);
        lblNetworkName.Text = networkName;
        //Testing - remove the line below when project goes live!!!!
        NetworkName = networkName = "LittleBearRiver";
        var stat1 = DataAccess_Logging.GetStat1_GroupByDay(networkName);
        var stat2 = DataAccess_Logging.GetStat2_GroupByDay(networkName);
        var stat3 = DataAccess_Logging.GetStat3_GroupByDay(networkName);

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


        var paddedList1 = PadDates(list1);
        var paddedList2 = PadDates(list2);
        var paddedList3 = PadDates(list3);


        rptStat1.DataSource = paddedList1;
        rptStat1.DataBind();
        rptStat2.DataSource = paddedList2;
        rptStat2.DataBind();
        rptStat3.DataSource = paddedList3;
        rptStat3.DataBind();

        var csvList = new List<CSVModel>();
        csvList.Add(new CSVModel { Date = "Date", Stat1 = "Clients per Day", Stat2 = "Requests per Day", Stat3 = "Values per Day" });
        paddedList1.ForEach(x => {
            csvList.Add(new CSVModel { Date = x.Date, Stat1 = x.Count });
        });
        paddedList2.ForEach(x => {
            var itemToUpdate = csvList.SingleOrDefault(y => y.Date == x.Date);
            if (itemToUpdate != null) {
                itemToUpdate.Stat2 = x.Count;
            }
        });
        paddedList3.ForEach(x => {
            var itemToUpdate = csvList.SingleOrDefault(y => y.Date == x.Date);
            if (itemToUpdate != null) {
                itemToUpdate.Stat3 = x.Count;
            }
        });


        Configuration conf = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(Request.ApplicationPath);   //"/CentralHIS2"
        if (conf.AppSettings.Settings["UsageReportFolder"] == null) throw new Exception("Unable to find UsageReportFolder in AppSettings");
        string folder = conf.AppSettings.Settings["UsageReportFolder"].Value;
        string virtualDirectory = Request.ApplicationPath == "/" ? "/" : Request.ApplicationPath + "/";
        var fileName = "usagereport-" + Guid.NewGuid().ToString() + ".csv";

        System.IO.File.WriteAllLines(folder + "/ReportStat/" + fileName, csvList.Select(x => x.Date + "," + x.Stat1 + "," + x.Stat2 + "," + x.Stat3));
        lnkDownload.NavigateUrl = "/ReportStat/" + fileName; ;


    }

    private List<StatisticModel> PadDates(List<StatisticModel> inputList) {
        var outputList = new List<StatisticModel>();
        var startingDate = inputList.Min(x => x.GetActualDate());
        var endingDate = inputList.Max(x => x.GetActualDate());
        var dateCtr = startingDate;
        while (dateCtr <= endingDate) {
            var transactionsOnDateCtr = inputList.Where(x => x.GetActualDate().Date == dateCtr);
            if (transactionsOnDateCtr.Any()) {
                outputList.AddRange(transactionsOnDateCtr);
            } else {
                outputList.Add(new StatisticModel { Date = dateCtr.ToShortDateString(), Count = "0" });
            }
            dateCtr = dateCtr.AddDays(1);
        }
        return outputList;
    }
}