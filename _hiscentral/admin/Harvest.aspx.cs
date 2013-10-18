using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

public partial class admin_Harvest : System.Web.UI.Page {
    protected void Page_Load(object sender, EventArgs e) {

    }
    protected void Button1_Click(object sender, EventArgs e) {
        string sourceid = Session["NetworkID"].ToString();
        //Harvester h = new Harvester(sourceid);
        //h.HarvestNetwork();

        //Response.Redirect("../network.aspx      string datestring = begdate.Year.ToString();
        DateTime begdate = DateTime.Now;
        string datestring = begdate.Year.ToString();
        datestring += (begdate.Month < 10 ? "0" + begdate.Month.ToString() : begdate.Month.ToString());
        datestring += (begdate.Day < 10 ? "0" + begdate.Day.ToString() : begdate.Day.ToString());




        string logfilename = sourceid + "_" + datestring + ".txt";

        //string harvesterPath = "C:/Documents and Settings/whitenac/My Documents/Visual Studio 2005/Projects/HISCentralHarvester/HISCentralHarvester/bin/Debug/HISCentralHarvester.exe";
        //string harvesterPath = "F:/hiscentral_home/harvester/HISCentralHarvester2.exe";


        System.Diagnostics.Process pr = new System.Diagnostics.Process();
        pr.StartInfo.FileName = Server.MapPath("harvester/HisCentralHarvester2.exe");
        //pr.StartInfo.FileName = @"C:\inetpub\wwwroot\hiscentral\admin\harvester\HisCentralHarvester2.exe";
        pr.StartInfo.Arguments = sourceid;
        pr.Start();
        while (pr.HasExited == false)
            if ((DateTime.Now.Second % 5) == 0) {
                System.Threading.Thread.Sleep(1000);
            }
        //string harvesterPath = Server.MapPath("harvester/HisCentralHarvester2.exe");
        //System.Diagnostics.Process.Start(harvesterPath, sourceid);

        Configuration conf = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(Request.ApplicationPath);   //"/CentralHIS2"
        if (conf == null) throw new Exception("Unable to read web config");
        if(conf.AppSettings.Settings["HarvesterLogFolder"] == null) throw new Exception("Unable to find HarvestorLogFolder in AppSettings");
        string folder = conf.AppSettings.Settings["HarvesterLogFolder"].Value;
        string virtualDirectory = Request.ApplicationPath == "/" ? "/" : Request.ApplicationPath + "/";
        this.HyperLink1.NavigateUrl = virtualDirectory + "admin/" + folder + "/" + logfilename;
        this.HyperLink1.Visible = true;
        pnlWait.Visible = false;
    }
}
