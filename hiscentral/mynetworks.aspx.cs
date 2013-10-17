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
using Microsoft.AspNet.Membership.OpenAuth;

public partial class mynetworks : System.Web.UI.Page {
    protected void Page_Load(object sender, EventArgs e) {
        //Membership.GetUser() == null) Response.Redirect("login.aspx?url="+Request.Url.AbsolutePath);
    }
    protected void GridView1_SelectedIndexChanged(object sender, GridViewCommandEventArgs e) {

        int index = int.Parse(e.CommandArgument.ToString());
        GridView1.SelectedIndex = index;
        if (e.CommandName == "Select") {
            Session["NetworkID"] = this.GridView1.SelectedValue;
            Session["NetworkName"] = this.GridView1.Rows[index].Cells[3].Text;
            Response.Redirect("network.aspx");
        } else {
            Session["NetworkName"] = this.GridView1.Rows[index].Cells[3].Text;
            Session["NetworkWSDL"] = this.GridView1.Rows[index].Cells[4].Text;
            Response.Redirect("testpage.aspx");
        }



    }
    protected void SqlDataSource1_Selecting(object sender, SqlDataSourceSelectingEventArgs e) {

    }
    protected void SqlDataSource1_Init(object sender, EventArgs e) {
        if (Membership.GetUser() == null) Response.Redirect("Login.aspx");
        var loggedInUser = Membership.GetUser().UserName;

        if (Request.QueryString["sa"] == "sa") {
            if (!DataAccess.SuperAdmins.Contains(loggedInUser)) {
                SqlDataSource1.SelectCommand += "Where username = '" + loggedInUser + "'";
                GridView1.Columns[3].Visible = false;
            } else {
            }

        } else {
            SqlDataSource1.SelectCommand += "Where username = '" + loggedInUser + "'";
            GridView1.Columns[3].Visible = false;
        }


        

        var ProviderName = OpenAuth.GetProviderNameFromCurrentRequest();
        var gmail = Session["gmail"] != null ? Session["gmail"].ToString() : String.Empty;


    }


    protected void GridView1_RowCommand(object sender, GridViewCommandEventArgs e) {
        int index = int.Parse(e.CommandArgument.ToString());
        GridView1.SelectedIndex = index;
        if (e.CommandName == "Select") {
            Session["NetworkID"] = this.GridView1.SelectedValue;
            Session["NetworkName"] = this.GridView1.Rows[index].Cells[3].Text;
            Response.Redirect("network.aspx");
        } else {
            Session["NetworkName"] = this.GridView1.Rows[index].Cells[3].Text;
            Session["NetworkWSDL"] = this.GridView1.Rows[index].Cells[4].Text;
            Response.Redirect("testpage.aspx");
        }

    }


    protected void GridView1_SelectedIndexChanged1(object sender, EventArgs e) {
        var test123 = 3232;
    }
    //protected void GridView1_DataBound(object sender, GridViewCommandEventArgs e) {
    //    if (e.Row.RowType == DataControlRowType.DataRow) {
    //        e.Row.Attributes["onClick"] = "location.href='view.aspx?id=" + DataBinder.Eval(e.Row.DataItem, "id") + "'";
    //    }
    //}
    protected void GridView1_RowDataBound(object sender, GridViewRowEventArgs e) {
        if (e.Row.RowType == DataControlRowType.DataRow) {
            var thirdCell = e.Row.Cells[2];
            var networkName = e.Row.Cells[3].Text;
            thirdCell.Controls.Clear();
            thirdCell.Controls.Add(new HyperLink { NavigateUrl = "~/report.aspx?n=" + Server.UrlEncode(networkName), Text = "Usage Report" });
        }
    }
}
