using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class CreateTokens : System.Web.UI.Page {
    protected void Page_Load(object sender, EventArgs e) {
        try {
            DataAccess.GatherUniqueLoginsAndInitializeTokens();
            lblSuccess.Text = "Security tokens successfully created for existing logins. Please lookup the 'ExistingTokens' table in the database manually!";
        } catch (Exception ex) {
            lblSuccess.Text = ex.Message;
        }
        lblSuccess.Visible = true;

    }
}