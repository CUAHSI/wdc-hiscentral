using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using Microsoft.AspNet.Membership.OpenAuth;

public partial class login : System.Web.UI.Page {
    public string ReturnUrl { get; set; }

    protected void Page_Load(object sender, EventArgs e) {
        try {
            if (User.Identity.IsAuthenticated) {
                lblLogin.Text = "You are signed in as " + User.Identity.Name;
            } else {
                lblLogin.Text = "You are not signed in!";
            }
            if (IsPostBack) {
                var provider = Request.Form["provider"];
                if (provider == null) {
                    return;
                }
                var token = Request.QueryString["s"] ?? String.Empty;   //If token found, we want to associate an existing user's gmail address to his login

                var redirectUrl = String.IsNullOrEmpty(token) ? "~/ExternalLoginLandingPage.aspx" : "~/ExternalLoginLandingPage.aspx?token=" + token;
                if (!String.IsNullOrEmpty(ReturnUrl)) {
                    var resolvedReturnUrl = ResolveUrl(ReturnUrl);
                    redirectUrl += "?ReturnUrl=" + HttpUtility.UrlEncode(resolvedReturnUrl);
                }

                //if (!String.IsNullOrEmpty(Request.QueryString["ReturnUrl"])) {
                //    ReturnUrl = Request.QueryString["ReturnUrl"];
                //} else {
                //    ReturnUrl = "~/ExternalLoginLandingPage.aspx";
                //}

                //var resolvedReturnUrl = ResolveUrl(ReturnUrl);
                //var redirectUrl = HttpUtility.UrlEncode(resolvedReturnUrl);

                OpenAuth.RequestAuthentication(provider, redirectUrl);
            }
        } catch (Exception ex) {
            //throw new Exception(ex.Message);
        }

    }

    //protected void Login1_LoginError(object sender, EventArgs e)
    //{

    //    //There was a problem logging in the user

    //    //'See if this user exists in the database
    //    MembershipUser userInfo = Membership.GetUser(Login1.UserName);

    //    if (userInfo == null)
    //    {
    //        //'The user entered an invalid username...
    //        LoginErrorDetails.Text = "There is no user in the database with the username " + Login1.UserName;
    //    }
    //    else
    //    {
    //        //'See if the user is locked out or not approved
    //        if (!userInfo.IsApproved)
    //        {
    //            LoginErrorDetails.Text = "Your account has not yet been approved by the site's administrators. Please try again later...";
    //        }
    //        else if (userInfo.IsLockedOut)
    //        {
    //            LoginErrorDetails.Text = "Your account has been locked out because of a maximum number of incorrect login attempts. You will NOT be able to login until you contact a site administrator and have your account unlocked.";
    //        }
    //        else
    //        {
    //            //'The password was incorrect (don't show anything, the Login control already describes the problem)
    //            LoginErrorDetails.Text = String.Empty;
    //        }
    //    }

    //}

    protected void Login1_Authenticate(object sender, AuthenticateEventArgs e) {

    }

    public IEnumerable<ProviderDetails> GetProviderNames() {
        return OpenAuth.AuthenticationClients.GetAll();
    }
}

