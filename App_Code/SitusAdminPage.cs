using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Data;
using System.Net;

/// <summary>
/// Summary description for SitusPage
/// </summary>
public partial class SitusAdminPage : System.Web.UI.Page
{
    public static bool isPostBack;

    protected void Page_PreInit(object sender, EventArgs e)
    {
        if (Session["currentAgency"] == null)
        {
            isPostBack = this.IsPostBack;
            //ProfileCommon person = new ProfileCommon().GetProfile(User.Identity.Name);
            //Session["Agency"] = person.GetPropertyValue("Agency");
            //Session["currentAgency"] = (string)person.GetPropertyValue("Agency");
        }

        //Response.Write("WTF::" + Session["currentAgency"] + "::");
        //Response.Write(this.GetType().Name);
        if (String.IsNullOrWhiteSpace((string)Session["name"]) && GetType().Name != "adminapp_default_aspx")
            Response.Redirect(Page.ResolveUrl("~/AdminApp"));
    }

}