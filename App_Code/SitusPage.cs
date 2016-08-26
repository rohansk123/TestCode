using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Data;
using System.Net;

public partial class SitusPage : System.Web.UI.Page
{

    protected void Page_PreInit(object sender, EventArgs e)
    {
        //Response.Write("<br> ca:" + Session["currentAgency"]);

        //Response.Write("<br>test:" + (string)Session["name"] + doesGroupExist((string)Session["name"]));
        if (String.IsNullOrWhiteSpace((string)Session["name"]) && this.GetType().Name != "default_aspx" && this.GetType().Name != "home_aspx")
        {
            Response.Write("GroupShort: " + Session["name"]);
            //Response.Redirect(Page.ResolveUrl("~"));
        }

        if (this.GetType().Name == "products_aspx" && String.IsNullOrWhiteSpace(Request["EID"]) && String.IsNullOrWhiteSpace((string)Session["empID"]))
        {
            Response.Redirect(Page.ResolveUrl("~/Home.aspx"));
        }

        if (this.GetType().Name == "products_aspx" && !String.IsNullOrWhiteSpace(Request["EID"]))
        {
            Session["empID"] = Request["EID"];
        }
    }

    protected bool doesGroupExist(string groupName)
    {
        if(String.IsNullOrWhiteSpace((string) Session["currentAgency"]))
            return false;
        return Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT DISTINCT name FROM " + Session["currentAgency"] + ".sys.schemas WHERE name = '" + groupName + "') \n   then 1 \n   else 0 \n end"));
    }

    protected bool doesEmployeeExist(string EEID)
    {
        //throw new Exception(" select case \n   when exists (SELECT DISTINCT empID FROM " + Session["currentAgency"] + "." + Session["name"] + ".Employees WHERE empID = '" + EEID + "') \n   then 1 \n   else 0 \n end");
        return Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT DISTINCT empID FROM " + Session["currentAgency"] + ".[" + Session["name"] + "].Employees WHERE empID = '" + EEID + "') \n   then 1 \n   else 0 \n end"));
    }

    public class CensusException : Exception
    {
        public CensusException()
        {
        }

        public CensusException(string message)
            : base(message)
        {
        }
    }

}