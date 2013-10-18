using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class AssociateLogin : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var token = Request.QueryString["s"] ?? String.Empty;
        if (!String.IsNullOrEmpty(token)) {
            if (User.Identity.IsAuthenticated) {
                lblMessage.Text = "You are already logged into the application as " + User.Identity.Name + ". Please log out first!";
            } else {
                Response.Redirect("Login.aspx?s=" + token);
            }
        } else {
            lblMessage.Text = "Invalid token supplied in query string!";
        }
    }
}