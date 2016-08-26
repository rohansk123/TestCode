using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Xml;

/// <summary>
/// Summary description for SitusFunctionPage
public partial class SitusFunctionPage : System.Web.UI.Page
{
    protected void Page_PreInit(object sender, EventArgs e)
    {
        if(String.IsNullOrWhiteSpace((string)Session["currentAgency"]) || Session["currentAgency"] == null)
            Session["currentAgency"] = HttpContext.Current.Profile.GetPropertyValue("Agency");

        if (Request["RefreshProductsGrid"] == "Yes")
            ClientScript.RegisterStartupScript(GetType(), "RefreshProductsGrid", "<script>self.parent.location='../Products.aspx';</script>");
    }

    public class BeneficiariesPanel : Panel
    {
        Literal head = null;
        Literal body = null;
        PlaceHolder foot = null;
        LinkButton panelBtn = null;
        static List<List<string>> listOfProducts = null;

        public BeneficiariesPanel(string state)
        {
            head = new Literal();
            body = new Literal();
            foot = new PlaceHolder();
            if(SiteMaster.MyGlobals.myConnection.State == System.Data.ConnectionState.Closed)
                SiteMaster.MyGlobals.myConnection.Open();

            if (state == "add")
            {

                string SQL = "INSERT INTO [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[beneficiaries] ([EmpID],[name],[ssn],[product_code],[relation],[percent],[type])";
                
                if(!String.IsNullOrWhiteSpace(HttpContext.Current.Request["birthdate"]))
                    SQL = SQL.Insert(SQL.LastIndexOf(')'), ",[age]");

                SQL += "VALUES ('" + HttpContext.Current.Session["empID"] + "','" + HttpContext.Current.Request["name"] + "','" + HttpContext.Current.Request["ssn"] + "','BASE','" +
                    HttpContext.Current.Request["relation"] + "','" + HttpContext.Current.Request["percent"] + "','" + HttpContext.Current.Request["type"] + "') ";

                if (!String.IsNullOrWhiteSpace(HttpContext.Current.Request["birthdate"]))
                    SQL = SQL.Insert(SQL.LastIndexOf(')'), ",'" + SitusEnrollmentPage.ageAtEffectiveDate(HttpContext.Current.Request["birthdate"], DateTime.Now.ToShortDateString()) + "'");

                //HttpContext.Current.Response.Write("state "+SQL);
                if (new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteNonQuery() == 1)
                {
                    for (int i = 0; i < listOfProducts.Count; i++)
                    {
                        SQL = "INSERT INTO [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[beneficiaries]  ([EmpID],[name],[ssn],[product_code],[relation],[percent],[type]) " +
                            "VALUES ('" + HttpContext.Current.Session["empID"] + "','" + HttpContext.Current.Request["name"] + "','" + HttpContext.Current.Request["ssn"] + "','" + listOfProducts[i][1] + "','" +
                            HttpContext.Current.Request["relation"] + "','" + HttpContext.Current.Request["percent"] + "','" + HttpContext.Current.Request["type"] + "') ";
                        //HttpContext.Current.Response.Write(SQL);
                        new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteNonQuery();

                    }
                    SiteMaster.MyGlobals.myConnection.Close();
                    HttpContext.Current.Response.Redirect("Beneficiaries.aspx");
                }
                SiteMaster.MyGlobals.myConnection.Close();
            }

            if (state == "edit")
            {
                Controls.Clear();
                head = new Literal();
                body = new Literal();
                foot = new PlaceHolder();

                panelBtn = new LinkButton();
                panelBtn.Click += editBeneficiary;
                panelBtn.CssClass = "btn btn-warning btn-xs";
                panelBtn.Text = "<i class='fa fa-check fa-white'></i> Save Changes";

                SqlDataReader myReader = new SqlCommand("SELECT * FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[beneficiaries] " +
                    "WHERE EmpID='" + HttpContext.Current.Session["empID"] + "' AND ID='" + HttpContext.Current.Request["key"] + "' ", SiteMaster.MyGlobals.myConnection).ExecuteReader();
                while (myReader.Read())
                {
                    body.Text += "     <tbody>" +
                                 "   	    <tr><td>Name: </td><td><input type='text' name='name' value=\"" + myReader["name"].ToString() + "\" /></td></tr> \n" +
                                 "   	    <tr><td>Percent: </td><td><input type='text' name='percent' value='" + myReader["percent"].ToString() + "' style='width:50px' maxlength='3'/>%</td></tr> \n" +
                                 "   	   	<tr><td>Type: </td><td><select name='type'> \n";
                    if(myReader["type"].ToString() == "Primary")
                    {
                        body.Text += "                       <option value='Primary' selected>Primary</option> \n" +
                                     "                       <option value='Contingent'>Contingent</option> \n";
                    }
                    else if (myReader["type"].ToString() == "Contingent")
                    {
                        body.Text += "                       <option value='Primary'>Primary</option> \n" +
                                     "                       <option value='Contingent' selected>Contingent</option> \n";
                    }
                    body.Text += "                   </select></td></tr> \n" +
                                 "   	   	<tr><td>ssn: </td><td style='font-size: 11px'><input type='text' name='ssn' value='" + myReader["ssn"].ToString() + "' /> </td></tr> \n" +
                                 "   	   	<tr><td>Relation: </td><td><input type='text' name='relation' value='" + myReader["relation"].ToString() + "' /></td></tr> \n" +
                                 "         <tr><td colspan='2'><input type='hidden' name='key' value='" + HttpContext.Current.Request["key"] + "' /> &nbsp; </td> </tr>" +
                                 "     </tbody>";
                }
                myReader.Close();

                foot.Controls.Add(new System.Web.UI.LiteralControl("<div style='color: gray; height: 20px; width: 100%; position: absolute; bottom:0px; margin-bottom:5px; text-align:center'>"));
                foot.Controls.Add(panelBtn);
                foot.Controls.Add(new System.Web.UI.LiteralControl("</div>"));
                //HttpContext.Current.Response.Write(HttpContext.Current.Request["state"]);

                Controls.Add(head);
                Controls.Add(body);
                Controls.Add(foot);
            }

            if (state == "editP")
            {
                Controls.Clear();
                head = new Literal();
                body = new Literal();
                foot = new PlaceHolder();

                panelBtn = new LinkButton();
                panelBtn.Click += saveBeneficiaries;
                panelBtn.CssClass = "btn btn-warning btn-xs";
                panelBtn.Text = "<i class='fa fa-check fa-white'></i> Save Changes";
                panelBtn.Style.Add("text-align", "center");
                panelBtn.Style.Add("position", "absolute");
                panelBtn.Style.Add("bottom", "0px");
                panelBtn.Style.Add("left", "40%");

                head.Text = "<thead>\n                     <tr> <th>Name</th> <th>Relation</th> <th>%</th> <th>Type</th> </tr> \n    	            </thead>  \n";

                body.Text = "";

                int count = 0;
                string SQL = "SELECT b.[ID],[name],[relation],[type],[percent],[product_code],[product_name] " +
                             "FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Beneficiaries] AS b " +
                             "LEFT JOIN [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Products] ON [product_code]='" + HttpContext.Current.Request["key"] + "' " +
                             "WHERE EmpID='" + HttpContext.Current.Session["empID"] + "' ";
                //HttpContext.Current.Response.Write(SQL);
                SqlDataReader myReader = new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteReader();
                if (myReader.HasRows)
                {
                    while (myReader.Read())
                    {

                        body.Text += "<tr> <td colspan='4'> " + myReader["product_name"].ToString() + " </td> </tr> \n";

                        body.Text += "   	    <tr><td><input type='hidden'  name='ID_" + count + "' value='" + myReader["ID"].ToString() + "'/> \n";
                        body.Text += "   	            <input type='text' style='width:120px' name='name_" + count + "' value='" + myReader["name"].ToString() + "'/> </td> \n";
                        body.Text += "   	   	<td><input type='text' style='width:60px' name='relation_" + count + "' value='" + myReader["relation"].ToString() + "'/> </td>  \n";
                        body.Text += "   	   	<td><input type='text' style='width:30px' maxlength='3' name='percent_" + count + "' value='" + myReader["percent"].ToString() + "'/> </td>  \n";
                        body.Text += "   	   	<td><input type='text' style='width:40px' name='type_" + count + "' value='" + myReader["type"].ToString() + "'/> </td>  \n";
                        body.Text += "   	    </tr>  \n";

                        count++;
                    }
                }
                else body.Text += "<tr><td colspan='6'>There are no beneficiaries listed.</td></tr>\n";
                myReader.Close();
                SiteMaster.MyGlobals.myConnection.Close();

                body.Text += "<tr><td colspan='4' style='height:20px'><input type='hidden' name='count' value='" + count + "'/></td></tr>\n";
                Controls.Add(head);
                Controls.Add(body);
                Controls.Add(foot);
                Controls.Add(panelBtn);
            }

            if (state == "delete")
            {
                string SQL = "DELETE FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[beneficiaries] " +
                    "WHERE EmpID='" + HttpContext.Current.Session["empID"] + "' AND name='" + HttpContext.Current.Request["name"] + "' ";
                HttpContext.Current.Response.Write(SQL);
                new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteNonQuery();
                SiteMaster.MyGlobals.myConnection.Close();

                HttpContext.Current.Response.Redirect("Beneficiaries.aspx");
            }
            Controls.Add(head);
            Controls.Add(body);
            Controls.Add(foot);
        }

        public BeneficiariesPanel()
        {
            Controls.Clear();
            SqlConnection currentConnection = new SqlConnection(SiteMaster.MyGlobals.connectionString);
            if (currentConnection.State == System.Data.ConnectionState.Closed)
                currentConnection.Open();

            if(HttpContext.Current.Request["updated"] == "yes")
                HttpContext.Current.Response.Write("<div class='alert alert-success'><button class='close' data-dismiss='alert'>hide</button><strong>Success!</strong> Beneficiaries have been updated.</div>");

            head = new Literal();
            body = new Literal();
            foot = new PlaceHolder();
            listOfProducts = new List<List<string>>();
            body.Text = "<thead> <tr>\n   <th> Name </th> <th> Type </th> <th> Percent </th>";

            SqlDataReader myReader = new SqlCommand("SELECT * FROM [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Products] WHERE [hasBeneficiaries]='TRUE' ", currentConnection).ExecuteReader();

            while (myReader.Read())
            {
                listOfProducts.Add(new List<string> { myReader["product_name"].ToString(), myReader["code"].ToString() });
                body.Text += " <th> " + myReader["product_name"].ToString() + " </th>";
            }
            myReader.Close();
            body.Text += "\n </th> <th> </th> <th> </tr> </thead>  \n <tr>\n    ";

            myReader = new SqlCommand("SELECT * FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Beneficiaries] " +
                                        "WHERE [EmpID]='" + HttpContext.Current.Session["empID"] + "' ORDER BY [type],[name]", currentConnection).ExecuteReader();

            //HttpContext.Current.Response.Write("BeneficiariesPanel: SELECT * FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Beneficiaries] " +
            //"WHERE [EmpID]='" + HttpContext.Current.Session["empID"] + "' ORDER BY [type],[name]");

            double primarySum = 0;
            double contingentSum = 0;
            string name = "";
            string oldName = "";
            string lastID = "";
            string[] associatedProducts = new string[listOfProducts.Count];

            if (myReader.HasRows)
            {
                while (myReader.Read())
                {
                    // *** SET UP VARIABLES FOR FIRST ITERATION ***
                    if (String.IsNullOrWhiteSpace(oldName))
                    {
                        oldName = myReader["name"].ToString();
                        name = myReader["name"].ToString();
                        body.Text += " <td> " + myReader["name"].ToString() + " </td> <td> " + myReader["type"].ToString() + " </td> <td> " + myReader["percent"].ToString() + " </td>";
                        oldName = myReader["name"].ToString();

                        if (myReader["type"].ToString() == "Primary")
                            primarySum += Convert.ToDouble(myReader["percent"]);

                        if (myReader["type"].ToString() == "Contingent")
                            contingentSum += Convert.ToDouble(myReader["percent"]);
                    }

                    name = myReader["name"].ToString();

                    // *** IF CURRENT BENEFICIARY'S NAME HAS CHANGED, ADD IN PREVIOUS BENEFICIARY'S ASSOCIATED PRODUCTS ***
                    if (oldName != name)
                    {
                        for (int i = 0; i < associatedProducts.Length; i++)
                        {
                            //HttpContext.Current.Response.Write(associatedProducts[i]);
                            if (associatedProducts[i] == "1")
                                body.Text += " <td> <input type='checkbox' name='" + listOfProducts[i][1] + "[]' value='" + lastID + "' checked /> </td>";
                            else
                                body.Text += " <td> <input type='checkbox' name='" + listOfProducts[i][1] + "[]' value='" + lastID + "' /> </td>";
                        }
                        body.Text += "<td><a class='edit_dependent' href='Beneficiaries.aspx?state=edit&key=" + lastID + "'>Edit</a></td>";
                        body.Text += "<td><a class='delete_dependent' href='Beneficiaries.aspx?state=delete&name=" + oldName + "'>Delete</a></td>";
                        body.Text += "\n </tr> \n <tr>\n    ";

                        Array.Clear(associatedProducts, 0, associatedProducts.Length);
                        body.Text += " <td> " + myReader["name"].ToString() + " </td> <td> " + myReader["type"].ToString() + " </td> <td> " + myReader["percent"].ToString() + " </td>";
                        oldName = myReader["name"].ToString();

                        if (myReader["type"].ToString() == "Primary")
                            primarySum += Convert.ToDouble(myReader["percent"]);

                        if (myReader["type"].ToString() == "Contingent")
                            contingentSum += Convert.ToDouble(myReader["percent"]);
                    }

                    // *** FIND AND LIST ASSOCIATED PRODUCTS FOR CURRENT BENEFICIARY ***
                    for (int i = 0; i < listOfProducts.Count; i++)
                    {
                        if (listOfProducts[i][1] == myReader["product_code"].ToString())
                            associatedProducts[i] = "1";
                    }

                    // *** STORE ID FOR NEXT ITERATION'S EDIT BUTTION ***
                    if (myReader["product_code"].ToString() == "BASE")
                        lastID = myReader["ID"].ToString();
                }

                // *** ADD LAST BENEFICIARY'S ASSOCIATED PRODUCTS ***
                for (int i = 0; i < associatedProducts.Length; i++)
                {
                    //HttpContext.Current.Response.Write(associatedProducts[i]);
                    if (associatedProducts[i] == "1")
                        body.Text += " <td> <input type='checkbox' name='" + listOfProducts[i][1] + "[]' value='" + lastID + "' checked /> </td>";
                    else
                        body.Text += " <td> <input type='checkbox' name='" + listOfProducts[i][1] + "[]' value='" + lastID + "' /> </td>";
                }
                body.Text += "<td><a class='edit_dependent' href='Beneficiaries.aspx?state=edit&key=" + lastID + "'>Edit</a></td>";
                body.Text += "<td><a class='delete_dependent' href='Beneficiaries.aspx?state=delete&name=" + oldName + "'>Delete</a></td>";
                body.Text += "\n </tr> <tr><td colspan='" + (listOfProducts.Count + 6) + "'> &nbsp;  </td> </tr>";
                myReader.Close();

                if (contingentSum != 0 && Math.Round(contingentSum) != 100)
                {
                    head.Text = "<div class='alert alert-danger'><strong>Error!</strong> The percentages for the Contingent Beneficiaries does not add to 100%</div>";
                }

                if (primarySum != 0 && Math.Round(primarySum) != 100)
                {
                    head.Text = "<div class='alert alert-danger'><strong>Error!</strong> The percentages for the Primary Beneficiaries does not add to 100%</div>";
                }

                //LinkButton editBen = new LinkButton();
                //editBen.Click += saveBeneficiaries;
                //editBen.CssClass = "btn btn-warning btn-xs";
                //editBen.Text = "<i class='fa fa-check fa-white'></i> Save Changes";

                LinkButton addBen = new LinkButton();
                addBen.Click += displayAddPanel;
                addBen.CssClass = "btn btn-info btn-xs";
                addBen.Text = "<i class='fa fa-plus fa-white'></i> Add Beneficiary";

                LinkButton okBen = new LinkButton();
                okBen.Click += redirectToGrid;
                okBen.CssClass = "btn btn-success btn-xs";
                okBen.Text = "<i class='fa fa-check fa-white'></i> These beneficiaries are correct";

                foot.Controls.Add(new System.Web.UI.LiteralControl("<div style='color: gray; height: 20px; width: 100%; position: absolute; bottom:0px; margin-bottom:5px; text-align:center'>"));
               // foot.Controls.Add(editBen);
                foot.Controls.Add(new Label() { Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" });
                foot.Controls.Add(addBen);
                foot.Controls.Add(new Label() { Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" });
                foot.Controls.Add(okBen);
                foot.Controls.Add(new System.Web.UI.LiteralControl("</div>"));
            }
            else
            {
                body.Text = "\n </tr> <tr><td colspan='" + (listOfProducts.Count + 6) + "'> There are no Beneficiaries Listed  </td> </tr>";
                body.Text += "\n </tr> <tr><td colspan='" + (listOfProducts.Count + 6) + "'> &nbsp;  </td> </tr>";

                LinkButton addBen = new LinkButton();
                addBen.Click += displayAddPanel;
                addBen.CssClass = "btn btn-info btn-xs";
                addBen.Text = "<i class='fa fa-plus fa-white'></i> Add Beneficiary";

                LinkButton okBen = new LinkButton();
                okBen.Click += redirectToGrid;
                okBen.CssClass = "btn btn-success btn-xs";
                okBen.Text = "<i class='fa fa-check fa-white'></i> These beneficiaries are correct";

                foot.Controls.Add(new System.Web.UI.LiteralControl("<div style='color: gray; height: 20px; width: 100%; position: absolute; bottom:0px; margin-bottom:5px; text-align:center'>"));
                foot.Controls.Add(addBen);
                foot.Controls.Add(new Label() { Text = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" });
                foot.Controls.Add(okBen);
                foot.Controls.Add(new System.Web.UI.LiteralControl("</div>"));
            }

            Controls.Add(head);
            Controls.Add(body);
            Controls.Add(foot);
            currentConnection.Close();
        }

        void displayAddPanel(object sender, EventArgs e)
        {
            Controls.Clear();
            head = new Literal();
            body = new Literal();
            foot = new PlaceHolder();

            panelBtn = new LinkButton();
            panelBtn.Click += addBeneficiary;
            panelBtn.CssClass = "btn btn-info btn-xs";
            panelBtn.Text = "<i class='fa fa-plus fa-white'></i> Add Beneficiary";
            panelBtn.Style.Add("text-align", "center");
            panelBtn.Style.Add("position", "absolute");
            panelBtn.Style.Add("bottom", "0px");
            panelBtn.ID = "addBtn";
            panelBtn.ClientIDMode = System.Web.UI.ClientIDMode.Static;
            panelBtn.Enabled = false;
            panelBtn.Style.Add("left", "40%");

            panelBtn.Attributes.CssStyle.Add("text-align", "center");
            head.Text = "  <div>         <input type='hidden' name='__EVENTTARGET' id='__EVENTTARGET' value='' /> \n" +
                     "<input type='hidden' name='__EVENTARGUMENT' id='__EVENTARGUMENT' value='' /></div> \n" +
                     "<script type='text/javascript'>" +
 "//<![CDATA[ \n" +
    " var theForm = document.forms['aspnetForm'];" +
     "if (!theForm) {" +
      "   theForm = document.aspnetForm;" +
     "}" +
     "function __doPostBack(eventTarget, eventArgument) {" +
        " if (!theForm.onsubmit || (theForm.onsubmit() != false)) {" +
           "  theForm.__EVENTTARGET.value = eventTarget;" +
           "  theForm.__EVENTARGUMENT.value = eventArgument;" +
            " theForm.submit();" +
         "}" +
     "}" +
 "//]]>" +
 "</script> \n";
            head.Text += "<thead>\n                     <tr> <th colspan='2'>Please enter the new Employee Census Data</th> </tr> \n    	            </thead>  \n";
            body.Text = "      <tbody>" +
                                    "           <tr><td>Name: </td><td><input type='text' name='name' maxlength='50' required /></td></tr> \n" +
                                    "   	   	<tr><td>SSN: </td><td><input type='text' name='ssn' onkeypress=\"FormatNumber(this, '###-##-####');\" maxlength='11' /></td></tr> \n";
            if ((string)HttpContext.Current.Session["currentAgency"] == "TaylorAndSons")
                body.Text += "   	   	<tr><td>Birthdate: </td><td><input type='text' name='birthdate' onkeypress=\"FormatNumber(this, '##/##/####');\" maxlength='11' /></td></tr> \n";
            body.Text += "   	   	<tr><td>Relation: </td><td><input type='text' name='relation' maxlength='50' /></td></tr> \n" +
                                    "   	   	<tr><td>Percent: </td><td><input type='text' name='percent' maxlength='3' style='width:50px' required/></td></tr> \n" +
                                    "   	   	<tr><td>Type: </td><td><select name='type' required> \n" +
                                    "                       <option value='Primary'>Primary</option> \n" +
                                    "                       <option value='Contingent'>Contingent</option> \n" +
                                    "                   </select></td></tr> \n" +
                                    "           <tr><td colspan='6'>&nbsp;<input type='hidden' name='state' value='add' /></td></tr>\n" +
                                    "      </tbody>";   
            //foot.Controls.Add(panelBtn);

            Controls.Add(head);
            Controls.Add(body);
            Controls.Add(foot);
            Controls.Add(panelBtn);
        }

        void displayEditPanel(object sender, EventArgs e)
        {
            Controls.Clear();
            head = new Literal();
            body = new Literal();
            foot = new PlaceHolder();

            panelBtn = new LinkButton();
            panelBtn.Click += editBeneficiary;
            panelBtn.CssClass = "btn btn-warning btn-xs";
            panelBtn.Text = "<i class='fa fa-check fa-white'></i> Save Changes";

            SqlDataReader myReader = new SqlCommand("SELECT * FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[beneficiaries] " +
                "WHERE EmpID='" + HttpContext.Current.Session["empID"] + "' AND ID='" + HttpContext.Current.Request["key"] + "' ", SiteMaster.MyGlobals.myConnection).ExecuteReader();
            while (myReader.Read())
            {
                body.Text += "     <tbody>" +
                                  "   	    <tr><td>Name: </td><td><input type='text' name='name' value='" + myReader["name"].ToString() + "' /></td></tr> \n" +
                                  "   	   	<tr><td>Type: </td><td style='font-size: 11px'>" + myReader["type"].ToString() + " </td></tr> \n" +
                                  "   	   	<tr><td>ssn: </td><td style='font-size: 11px'>" + myReader["ssn"].ToString() + " </td></tr> \n" +
                                  "   	   	<tr><td>Relation: </td><td><input type='text' name='relation' value='" + myReader["relation"].ToString() + "' /></td></tr> \n" +
                                  "         <input type='hidden' name='key' value='" + HttpContext.Current.Request["key"] + "' /></tr> \n" +
                                  "     </tbody>";
            }
            myReader.Close();

            foot.Controls.Add(new System.Web.UI.LiteralControl("<div style='color: gray; height: 20px; width: 100%; position: absolute; bottom:0px; margin-bottom:5px; text-align:center'>"));
            foot.Controls.Add(panelBtn);
            foot.Controls.Add(new System.Web.UI.LiteralControl("</div>"));

            Controls.Add(head);
            Controls.Add(body);
            Controls.Add(foot);
            //Controls.Add(panelBtn);
        }

        void displayProductPanel(object sender, EventArgs e)
        {
            Controls.Clear();
            if (SiteMaster.MyGlobals.myConnection.State == System.Data.ConnectionState.Closed)
                SiteMaster.MyGlobals.myConnection.Open();

            head = new Literal();
            body = new Literal();
            foot = new PlaceHolder();
            panelBtn = new LinkButton();
            panelBtn.Click += displayAddPanel;
            panelBtn.CssClass = "btn btn-info btn-xs";
            panelBtn.Text = "<i class='fa fa-plus fa-white'></i> Ad1d Beneficiary";
            panelBtn.Style.Add("text-align", "center");
            panelBtn.Style.Add("position", "absolute");
            panelBtn.Style.Add("bottom", "0px");
            panelBtn.Style.Add("left", "40%");

            head.Text = "<thead>\n                     <tr> <th>Name</th> <th>Relation</th> <th>%</th> <th>Type</th> </tr> \n    	            </thead>  \n";

            body.Text = "";

            string SQL = "SELECT b.[ID],[name],[relation],[type],[percent],[product_code],[product_name] " +
                         "FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Beneficiaries] AS b " +
                         "LEFT JOIN [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Products] ON [product_code]='" + HttpContext.Current.Request["key"] + "' " +
                         "WHERE EmpID='" + HttpContext.Current.Session["empID"] + "' ";
            //HttpContext.Current.Response.Write(SQL);
            SqlDataReader myReader = new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteReader();
            if (myReader.HasRows)
            {
                while (myReader.Read())
                {

                    body.Text += "<tr> <td colspan='4'> " + myReader["product_name"].ToString() + " </td> </tr> \n";

                    body.Text += "   	    <tr><td><input type='text' value='" + myReader["name"].ToString() + "'/> </td> \n";
                    body.Text += "   	   	<td><input type='text' value='" + myReader["relation"].ToString() + "'/> </td>  \n";
                    body.Text += "   	   	<td><input type='text' value='" + myReader["percent"].ToString() + "'/> </td>  \n";
                    body.Text += "   	   	<td><input type='text' value='" + myReader["type"].ToString() + "'/> </td>  \n";
                    body.Text += "   	    </tr>  \n";
                }
            }
            else body.Text += "<tr><td colspan='6'>There are no beneficiaries listed.</td></tr>\n";
            myReader.Close();
            SiteMaster.MyGlobals.myConnection.Close();
            body.Text += "<tr><td colspan='6'>&nbsp;</td></tr>\n";
            Controls.Add(head);
            Controls.Add(body);
            Controls.Add(foot);
            Controls.Add(panelBtn);
        }

        void addBeneficiary(object sender, EventArgs e)
        {
            //HttpContext.Current.Response.Write("asasaasa");
            SiteMaster.MyGlobals.myConnection.Open();

            string SQL = "INSERT INTO [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[beneficiaries] " +
                "VALUES ('" + HttpContext.Current.Session["empID"] + "','" + HttpContext.Current.Request["name"] + "','" + HttpContext.Current.Request["ssn"] + "','N/A','" +
                HttpContext.Current.Request["relation"] + "','" + HttpContext.Current.Request["percent"] + "','" + HttpContext.Current.Request["type"] + "') ";
            //HttpContext.Current.Response.Write(SQL);
            new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteNonQuery();

            SiteMaster.MyGlobals.myConnection.Close();
            //HttpContext.Current.Response.Redirect("Beneficiaries.aspx");
        }

        void saveBeneficiaries(object sender, EventArgs e)
        {
            if (SiteMaster.MyGlobals.myConnection.State == System.Data.ConnectionState.Closed)
                SiteMaster.MyGlobals.myConnection.Open();
            for (int i = 0; i < listOfProducts.Count; i++)
            {
                string currentProduct = listOfProducts[i][1];
                string[] benList = null;
                if(!String.IsNullOrWhiteSpace( HttpContext.Current.Request[currentProduct + "[]"]))
                    benList = HttpContext.Current.Request[currentProduct + "[]"].Split(',');

                // *** DELETE OLD BENEFICIARY VALUES ***
                string SQL = "DELETE FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Beneficiaries] " +
                    "WHERE [EmpID]='" + (string)HttpContext.Current.Session["empID"] + "' AND [product_code]='" + currentProduct + "' ";
                //Response.Write(SQL);
                new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteNonQuery();

                if(benList != null)
                    foreach (string benID in benList)
                    {
                        //HttpContext.Current.Response.Write(benID);
                        // *** GET BASE BENEFICIARY VALUES ***
                        SQL = "SELECT * FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Beneficiaries] " +
                            "WHERE [ID]='" + benID + "' AND [product_code]='BASE' ";
                        HttpContext.Current.Response.Write(SQL);

                        SqlDataReader myReader2 = new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteReader();
                        if (myReader2.HasRows)
                        {
                            while (myReader2.Read())
                            {
                                string name = myReader2["name"].ToString();
                                string ssn = myReader2["ssn"].ToString();
                                string relation = myReader2["relation"].ToString();
                                string percent = myReader2["percent"].ToString();
                                string type = myReader2["type"].ToString();
                                HttpContext.Current.Response.Write(name + ":" + ssn + ":" + relation);

                                foreach (char a in name)
                                    if (a == '\'')
                                        name = name.Insert(name.IndexOf(a), "'");
                                // // *** INSERT NEW BENEFICIARY VALUES ***
                                SQL = "INSERT INTO [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Beneficiaries] ([EmpID],[name],[ssn],[product_code],[relation],[percent],[type]) " +
                                    " VALUES('" + HttpContext.Current.Session["empID"] + "','" + name + "','" + ssn + "','" + currentProduct + "','" + relation + "','" + percent + "','" + type + "')";
                                HttpContext.Current.Response.Write("<br/>" + name + ": " + SQL);
                                new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteNonQuery();

                                
                               
                            }
                        }
                        myReader2.Close();
                    }
               
            }
            HttpContext.Current.Response.Redirect("Beneficiaries.aspx");
        }

        void editBeneficiary(object sender, EventArgs e)
        {
            string SQL = "UPDATE b " + 
                "SET [name]='" + Page.Request["name"] + "', [percent]='" + Page.Request["percent"] + "', [type]='" + Page.Request["type"] +
                    "', [ssn]='" + Page.Request["ssn"] + "', [relation]='" + Page.Request["relation"] + "' " +
                "FROM [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Beneficiaries] AS b " +
                "INNER JOIN [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[beneficiaries] AS s ON b.EmpID = s.EmpID AND b.name=s.name " +
                "WHERE b.[EmpID]='" + HttpContext.Current.Session["empID"] + "' AND s.product_code='BASE'  AND s.[ID]='" + Page.Request["key"] + "' ";
            HttpContext.Current.Response.Write(SQL);
            new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteNonQuery();

            HttpContext.Current.Response.Redirect("Beneficiaries.aspx");
        }

        void deleteBeneficiary(object sender, EventArgs e)
        {
            string SQL = "DELETE FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[beneficiaries] " +
                "WHERE EmpID='" + HttpContext.Current.Session["empID"] + "' AND ID='" + Page.Request["key"] + "' ";
            Page.Response.Write(SQL);
            new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteNonQuery();

            Page.Response.Redirect(Page.ResolveUrl("~") + "Census/Beneficiaries.aspx");
        }
       // void saveBeneficiaries(object sender, EventArgs e)
        void  redirectToGrid(object sender, EventArgs e) 
        {
           //  myConnection.Open();
            bool connClosed = false;
            if (SiteMaster.MyGlobals.myConnection.State == System.Data.ConnectionState.Closed)
            {
                connClosed = true;
                SiteMaster.MyGlobals.myConnection.Open();
            }

            string SQL = "SELECT count(*) FROM [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Beneficiaries] " +
                            "WHERE [empID]='" + HttpContext.Current.Session["empID"] + "' AND [product_code]='BASE' ";
            int count = Convert.ToInt32(SiteMaster.getSingleSQLData(SQL));
            if (count > 0)
            {
                SQL = "UPDATE [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Employees] SET [app_status]='pending'";
                SQL += " WHERE [empID]=" + HttpContext.Current.Session["empID"];
                // myCommand = new SqlCommand(SQL, myConnection);
                if (new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteNonQuery() == 1)
                {
                    if (HttpContext.Current.Session["appStatus"].ToString() == "notstarted")
                        HttpContext.Current.Session["appStatus"] = "pending";
                    if (connClosed)
                        SiteMaster.MyGlobals.myConnection.Close();
                    Page.Response.Redirect(ResolveUrl("~"));
                }
            }
            else
            {
                Page.Response.Redirect(Page.ResolveUrl("~") + "Census/Beneficiaries.aspx?ben=none");
            }
        }
    }

    public class CustomReportsPanel : Panel
    {
        Literal options = null;
        PlaceHolder functions = null;
        LinkButton panelBtn = null;
        CheckBoxList colList = null;
        string[] requiredCols = new string[] { "fname", "lname", "product_name", "deduction_amt", "face_value", "tier", "coverage_level" };
        ListBox savedReportsList = null;

        public string product = null;
        public string reportType = null;

        public CustomReportsPanel(string reportType, string extraData, string[] colList)
        {
            if (SiteMaster.MyGlobals.myConnection.State == System.Data.ConnectionState.Closed)
                SiteMaster.MyGlobals.myConnection.Open();

            options = new Literal();
            string tableName = "";
            int columnCount = 0;
            string colListString = "'" + String.Join("', '", colList) + "'";

            string headString = "<div class='widget-content'>";

            string savedReportsString = "<div style='display:inline-block; width:33%; vertical-align:top'>Saved Report Templates: <br/> \n" +
            "<select name='savedReportList' onclick='window.location=\"AddChangeTerminateReport.aspx?reportState=load&reportName=\"+this.value;' size='6'>\n";

            foreach (string asd in System.IO.Directory.GetFiles(SiteMaster.getFolderPath("Documents") + "/userData/" + HttpContext.Current.User.Identity.Name + @"/" + reportType))
            {
                if (asd.Contains(".xml"))
                {
                    string reportName = asd.Remove(asd.LastIndexOf('.')).Substring(asd.LastIndexOf("\\") + 1);
                    savedReportsString += "<option value='" + reportName + "'> " + reportName + " </option>";
                }
            }
            savedReportsString += "</select> </div>";

            headString += savedReportsString;

            headString += "<div style='display:inline-block; width:33%; border-right: 1px solid #ccc; border-left: 1px solid #ccc'><center><label for='ctl101'>Please enter the Report name:</label> \n" +
                "<input type='text' name='reportName' size='25' id='ctl101' required /> <br/> \n" +
                "<input type='checkbox' name='saveReport' value='yes' id='ctl102' /> <label for='ctl102'> Save for later use.</label><br/> \n" +
                "</center> </div>";

            headString += "<div style='display:inline-block; width:30%; vertical-align:top; padding-left: 5px'>" + extraData + "</div><hr /> ";

            Controls.Add(new Literal() { Text = headString });
            
            string initTablesString = "<div id='transactions'><center> \n  <table class='table table-bordered table-striped table-hover with-check' style='float:left; width: 33%'>\n";
            string endTablesString = "     </table> \n    </div> \n";
            string[] listofTables = null;
            if (colList.Length > 0)
                listofTables = new string[colList.Length];
            int count = 0;
            //throw new Exception("length:" + colList.Length);

            string SQL = "SELECT * FROM " + HttpContext.Current.Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='" + HttpContext.Current.Session["group"] + "' " +
                "AND TABLE_NAME IN  (" + colListString.Replace("CurrentDeductions", "") + ") " +
                "AND COLUMN_NAME NOT IN ('ID', 'password', 'code', 'carrier', 'selerix_group_number', 'ext_group_number', 'tax', 'rollover_tax'," +
                "'group_number', 'employee_ID', 'product_code', 'year', 'old_transaction', 'PDF', 'deduction_amt') " +
                "AND DATA_TYPE NOT IN ('bit')";

            if(colListString.Contains("Users"))
                 SQL += " UNION ALL " + 
                     "SELECT * FROM Administration.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='general' AND TABLE_NAME='Users' " +
                "AND COLUMN_NAME NOT IN ('ID', 'password', 'fname', 'sex', 'address', 'city', 'state', 'zip') " +
                "AND DATA_TYPE NOT IN ('bit')";

            SqlDataReader sql_columnsData = new SqlCommand(SQL, SiteMaster.MyGlobals.myConnection).ExecuteReader();

            while (sql_columnsData.Read())
            {
                if (String.IsNullOrWhiteSpace(tableName))
                {
                    tableName = sql_columnsData["TABLE_NAME"].ToString();
                    listofTables[count] = initTablesString + "        <thead> <tr> <th><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' name='title-table-checkbox' id='all_" + sql_columnsData["TABLE_NAME"] + "'></span></div> </th>" +
                        "        <th> " + tableName + " </th> </tr> </thead>";
                }

                if (tableName != sql_columnsData["TABLE_NAME"].ToString())
                {
                    listofTables[count] += endTablesString;
                    count++;

                    tableName = sql_columnsData["TABLE_NAME"].ToString();
                    if (tableName == "Users")
                        tableName = "Agents";

                    //HttpContext.Current.Response.Write("<br/>>length:" + listofTables.Length + ", count:" + count);

                    listofTables[count] = initTablesString + "        <thead> <tr> <th><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' name='title-table-checkbox' id='all_" + sql_columnsData["TABLE_NAME"] + "'></span></div> </th>" +
                        "        <th> " + tableName + " </th> </tr> </thead>";

                    if (tableName == "Agents")
                        tableName = "Users";
                }

                if (tableName == "Users")
                {
                    if(sql_columnsData["COLUMN_NAME"].ToString() == "lname")
                        listofTables[count] += "        <tr> <td><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' id='ctl" + columnCount + "' name='" + tableName + "Cols[]' " +
                        " value='agent_name' /> </span></div></td> <td> <label for='ctl" + columnCount++ + "'>Agent Name</label></td> \n </tr>";
                    else
                        listofTables[count] += "        <tr> <td><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' id='ctl" + columnCount + "' name='" + tableName + "Cols[]' " +
                        " value='agent_" + sql_columnsData["COLUMN_NAME"] + "' /> </span></div></td> <td> <label for='ctl" + columnCount++ + "'> " +
                        SiteMaster.UppercaseFirst(sql_columnsData["COLUMN_NAME"].ToString().Replace('_', ' ')) + "</label></td> \n </tr>";
                }
                else if(requiredCols.Contains(sql_columnsData["COLUMN_NAME"].ToString()))
                    listofTables[count] += "      <tr> <td><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='hidden' name='" + tableName + "Cols[]' value='" + sql_columnsData["COLUMN_NAME"] + "' /><input type='checkbox' checked disabled /> </span></div></td>" +
                        "<td>" + SiteMaster.UppercaseFirst(sql_columnsData["COLUMN_NAME"].ToString().Replace('_', ' ')) + "   <span class='label label-important'> Required.</span> </td> \n </tr>";
                else
                    listofTables[count] += "        <tr> <td><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' id='ctl" + columnCount + "' name='" + tableName + "Cols[]' " +
                        " value='" + sql_columnsData["COLUMN_NAME"] + "' /> </span></div></td> <td> <label for='ctl" + columnCount++ + "'> " +
                        SiteMaster.UppercaseFirst(sql_columnsData["COLUMN_NAME"].ToString().Replace('_', ' ')) + "</label></td> \n </tr>";

                if (sql_columnsData["COLUMN_NAME"].ToString() == "birthdate" || sql_columnsData["COLUMN_NAME"].ToString() == "hiredate" || sql_columnsData["COLUMN_NAME"].ToString() == "date")
                    listofTables[count] = listofTables[count].Insert(listofTables[count].LastIndexOf("</label>"), 
                        " - Set date range: <br />" +
                        "<input type='text' name='" + sql_columnsData["COLUMN_NAME"].ToString() + "_start_date' style='width:100px' maxlength='10' onkeypress=\"FormatNumber(this, '##/##/####');\" />" 
                        + "&nbsp;&nbsp;&nbsp;to&nbsp;&nbsp;&nbsp;" +
                        "<input type='text' name='" + sql_columnsData["COLUMN_NAME"].ToString() + "_end_date' style='width:100px' maxlength='10' onkeypress=\"FormatNumber(this, '##/##/####');\" />");

                //if (sql_columnsData["COLUMN_NAME"].ToString() == "face_value")
                //    options.Text = options.Text.Insert(options.Text.LastIndexOf("</li>"), "<b>(Please ensure this is checked when including Life policies in Report.)</b>");
                //colList.Controls.Add(new CheckBox { ID = tableName + columnCount++, 
                //                                    Text = sql_columnsData["COLUMN_NAME"].ToString()
                //                                    });
            }
            listofTables[count] += endTablesString;

            if(colListString.Contains("CurrentDeductions"))
            {
                count++;

                listofTables[count] = initTablesString + "        <thead> <tr> <th><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' name='title-table-checkbox' " + 
                    " id='all_CurrentDeductions'></span></div> </th>" +
                    "        <th> Current Deductions </th> </tr> </thead>";

                listofTables[count] += "        <tr> <td><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' id='ctl" + columnCount + "' name='CurrentDeductionsCols[]' " +
                    " value='cd_tier' /> </span></div></td> <td> <label for='ctl" + columnCount++ + "'> Tier </label></td> \n </tr>";

                listofTables[count] += "        <tr> <td><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' id='ctl" + columnCount + "' name='CurrentDeductionsCols[]' " +
                    " value='cd_coverage_level' /> </span></div></td> <td> <label for='ctl" + columnCount++ + "'> Coverage Level </label></td> \n </tr>";

                listofTables[count] += "        <tr> <td><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' id='ctl" + columnCount + "' name='CurrentDeductionsCols[]' " +
                    " value='cd_face_value' /> </span></div></td> <td> <label for='ctl" + columnCount++ + "'> Face Value </label></td> \n </tr>";

                listofTables[count] += endTablesString;
            }

            for (int i = 0; i < listofTables.Length; i ++)
                if (!String.IsNullOrWhiteSpace(listofTables[i]) && listofTables[i].Contains("all_Transactions"))
                {
                    string ts = listofTables[i];
                    ts = ts.Insert(ts.LastIndexOf("</table>") - 1, "        <tr> <td><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='checkbox' id='ctl" + columnCount + "' name='Amount' " +
                        " value='amount' checked />  </span></div></td> <td> <label for='ctl" + columnCount++ + "'> Old Deductions </label> </td> \n </tr>");

                    ts = ts.Insert(ts.LastIndexOf("</table>"), "        <tr><td><div class='checker' id='uniform-title-table-checkbox'><span class=''><input type='hidden' name='TransactionsCols[]' value='deduction_amt' /> " +
                                    "<input type='checkbox' checked disabled /> </span></div></td>" +
                        "<td> New Deductions <span class='label label-important'> Required.</span> </td> \n </tr>");

                    ts = ts.Insert(ts.LastIndexOf("</table>"), "        <tr><td><div class='checker' id='uniform-title-table-checkbox'><span class=''> <input type='checkbox' id='ctl" + columnCount + "' name='combineEESP' value='Yes' />  </span></div></td> <td> <label for='ctl" + columnCount++ + "'>  <strong>Combine EE/Spouse Policies. </strong></label> </td> \n </tr>");
                    listofTables[i] = ts;
                }

            Controls.Add(new Literal { Text = String.Join(" \n", listofTables) });

            string footString = "\n <hr style='width:98%; margin-bottom:0px'/></div> \n <div class='form-actions'> ";
            footString += "\n <input type='hidden' name='createReport' value='create' /> ";
            footString += "\n <button type='submit' id='submitCustRptBtn' name='submitBtn' class='btn btn-success btn-xs'><i class='icon-ok icon-white'></i> Generate Excel </button> \n ";
            footString += "\n</div> ";

            Controls.Add(new Literal { Text = footString });
        }

        public CustomReportsPanel(string reportName)
        {
            Controls.Clear();

            options = new Literal();
            string tableName = "";
            int columnCount = 0;

            XmlDocument report = new XmlDocument();
            report.Load(SiteMaster.getFolderPath("Documents") + @"\userData\" + HttpContext.Current.User.Identity.Name + @"\" + reportName + ".xml");
            
            Controls.Add(new Literal()
            {
                Text = "<center><label for='ctl101'>Please enter the Report name:</label> \n" +
                "<input type='text' name='reportName' size='25' id='ctl101' value='" + reportName + "' required /> <br/> \n" +
                "<input type='checkbox' name='saveReport' value='yes' id='ctl102' /> <label for='ctl102'> Save for later use.</label><br/> \n" +
                "<hr /> </center>"
            });

            foreach (XmlElement test in report)
            {
                product = test.GetAttribute("product");
                reportType = test.GetAttribute("type");
            }

            Controls.Add(new Literal()
            {
                Text = "<div style='position:absolute; top: 40px; left:5px; width: 170px; border-bottom: 1px solid black'> " +
                "For Product: <b>" +
                (string)SiteMaster.getSingleSQLData("SELECT DISTINCT [product_name] FROM [" +  HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].Products WHERE [code]='" + product + "'") + "</b><br/> Report Type: <b>" + reportType + "</b> </div> "
            });

            string [] fileList = System.IO.Directory.GetFiles(SiteMaster.getFolderPath("Documents") + "/userData/" + HttpContext.Current.User.Identity.Name);

            options.Text += "<select name='savedReportList' onclick='window.location=\"AddChangeTerminateReport.aspx?reportState=load&reportName=\"+this.value;' style='position:absolute; top:150px; right:5px;' size='" + (fileList.Length + 1) + "'>\n";

            foreach (string file in fileList)
            {
                string rn = file.Remove(file.LastIndexOf('.')).Substring(file.LastIndexOf("\\") + 1);
                options.Text += "<option value='" + rn + "'> " + rn + " </option>";
            }
            options.Text += "</select>";

            options.Text += "<div id='transactions'>\n   <ul style='list-style-type: none;'>\n";

            SqlDataReader sql_columnsData = new SqlCommand("SELECT * FROM " + HttpContext.Current.Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='" + HttpContext.Current.Session["group"] + "' " +
                "AND TABLE_NAME IN  ('Transactions', 'Employees', 'Products') " +
                "AND COLUMN_NAME NOT IN ('ID', 'empID', 'RN', 'code', 'carrier', 'selerix_group_number', 'ext_group_number', 'tax', 'rollover_tax'," +
                "'group_number', 'employee_ID', 'product_code', 'year', 'old_transaction', 'PDF', 'deduction_amt') " +
                "AND DATA_TYPE NOT IN ('bit')", SiteMaster.MyGlobals.myConnection).ExecuteReader();

            while (sql_columnsData.Read())
            {
                string checkedYes = "";
                if (String.IsNullOrWhiteSpace(tableName))
                {
                    tableName = sql_columnsData["TABLE_NAME"].ToString();
                    options.Text += "        <li><h3> " + tableName.Remove(tableName.Length - 1) + " Data</h3> </li>";
                }

                if (tableName != sql_columnsData["TABLE_NAME"].ToString())
                {
                    tableName = sql_columnsData["TABLE_NAME"].ToString();
                    options.Text += "        <li><h3> " + tableName.Remove(tableName.Length - 1) + " Data</h3> </li>";
                }

                foreach (XmlElement test in report)
                {
                    if (test.InnerXml.Contains(sql_columnsData["COLUMN_NAME"].ToString()))
                        checkedYes = "checked";
                }

                if (requiredCols.Contains(sql_columnsData["COLUMN_NAME"].ToString()))
                    options.Text += "        <li><input type='hidden' name='" + tableName + "Cols[]' value='" + sql_columnsData["COLUMN_NAME"] + "' /> <input type='checkbox' " + checkedYes + " disabled /> "
                        + SiteMaster.UppercaseFirst(sql_columnsData["COLUMN_NAME"].ToString().Replace('_', ' ')) +
                        "   <span class='label label-important'> Required.</span> </li> \n";
                else
                    options.Text += "        <li><input type='checkbox' id='ctl" + columnCount + "' name='" + tableName + "Cols[]' " +
                        " value='" + sql_columnsData["COLUMN_NAME"] + "' " + checkedYes + " /> <label for='ctl" + columnCount++ + "'> " +
                        SiteMaster.UppercaseFirst(sql_columnsData["COLUMN_NAME"].ToString().Replace('_', ' ')) + "</label> </li> \n";

                if (sql_columnsData["COLUMN_NAME"].ToString() == "face_value")
                    options.Text = options.Text.Insert(options.Text.LastIndexOf("</li>"), "(Please ensure this is checked when including Life policies in Report.)");
            }

            options.Text += "        <li><input type='checkbox' id='ctl" + columnCount + "' name='Amount' " +
                " value='amount' checked /> <label for='ctl" + columnCount++ + "'> Old Deductions </label> </li> \n";

            options.Text += "        <li><input type='hidden' name='TransactionsCols[]' value='deduction_amt' /> " +
                            "<input type='checkbox' checked disabled /> New Deductions " +
                            "<span class='label label-important'> Required.</span> </li> \n";

            options.Text += "   </ul>";
            options.Text += "\n <input type='hidden' name='createReport' value='create' /> ";
            options.Text += "\n <button type='submit' name='submitBtn' class='btn btn-success btn-xs'><i class='icon-ok icon-white'></i> Generate Excel </button>";
            options.Text += "\n</div>";

            Controls.Add(options);
        }

        public void refreshReportsList()
        {
            savedReportsList.Items.Clear();
            foreach (string asd in System.IO.Directory.GetFiles(SiteMaster.getFolderPath("Documents") + @"\userData\" + HttpContext.Current.User.Identity.Name))
            {
                savedReportsList.Items.Add(new ListItem(asd.Remove(asd.LastIndexOf('.')).Substring(asd.LastIndexOf("\\") + 1), asd));
            }
        }
    }

    public static Dictionary<string, Dictionary<string, string[]>> getProductsDetails()
    {
        Dictionary<string, Dictionary<string, string[]>> results = new Dictionary<string, Dictionary<string, string[]>>();
        SqlConnection myConnection = new SqlConnection(SiteMaster.MyGlobals.connectionString);
        myConnection.Open();

        string agency = (string)HttpContext.Current.Session["currentAgency"];
        string group = (string)HttpContext.Current.Session["group"];

        string SQL = "SELECT DISTINCT [COLUMN_NAME], [code] FROM [" + agency + "].[" + group + "].[Products] " +
            "JOIN [" + agency + "].INFORMATION_SCHEMA.COLUMNS ON [TABLE_NAME] = code + '_RT' AND [TABLE_SCHEMA] = '" + group + "' " +
            "WHERE [COLUMN_NAME] in ('coverage_level', 'tier', 'face_value') " +
            "ORDER BY [code], [COLUMN_NAME]";

        SqlDataReader sql_ProductsData = new SqlCommand(SQL, myConnection).ExecuteReader();

        if (sql_ProductsData.HasRows)
        {
            SQL = "";
            string currentProduct = "";
            Dictionary<string, string[]> details = null;

            while (sql_ProductsData.Read())
            {
                List<string> reportData = new List<string>();
                string columnName = sql_ProductsData["COLUMN_NAME"].ToString();
                if (currentProduct != sql_ProductsData["code"].ToString())
                {
                    if (details != null && details.Count > 0)
                        results[currentProduct] = details;

                    currentProduct = sql_ProductsData["code"].ToString();
                    details = new Dictionary<string, string[]>();
                }

                SQL = "SELECT DISTINCT [" + columnName + "] FROM [" + agency + "].[" + group + "].[" + currentProduct + "_RT]";

                SqlDataReader sql_ColumnData = new SqlCommand(SQL, myConnection).ExecuteReader();
                                
                while (sql_ColumnData.Read())
                {
                    reportData.Add(sql_ColumnData[columnName].ToString());
                }
                sql_ColumnData.Close();

                details[columnName] = reportData.ToArray<string>();
            }
            if (details != null && details.Count > 0)
                results[currentProduct] = details;

            sql_ProductsData.Close();
        }
        return results;
    }

    protected static string getDependentTypes()
    {
        return "                   <option value=''> Select... </option> \n" +
               "                   <option value='Spouse'>Spouse</option> \n" +
               "                   <option value='Child'>Child</option> \n" +
               "                   <option value='Mother'>Mother</option> \n" +
               "                   <option value='Father'>Father</option> \n" +
               "                   <option value='Other'>Other</option> \n";
    }

    protected static string getDependentTypes(string currentType)
    {
        string types = "                   <option value=''> Select... </option> \n";

        if (currentType == "Spouse")
            types += "                   <option value='Spouse' selected>Spouse</option> \n";
        else
            types += "                   <option value='Spouse'>Spouse</option> \n";

        if (currentType == "Child")
            types += "                   <option value='Child' selected>Child</option> \n";
        else
            types += "                   <option value='Child'>Child</option> \n";

        if (currentType == "Mother")
            types += "                   <option value='Mother' selected>Mother</option> \n";
        else
            types += "                   <option value='Mother'>Mother</option> \n";

        if (currentType == "Father")
            types += "                   <option value='Father' selected>Father</option> \n";
        else
            types += "                   <option value='Father'>Father</option> \n";

        if (currentType == "Other")
            types += "                   <option value='Other' selected>Other</option> \n";
        else
            types += "                   <option value='Other'>Other</option> \n";

        if (currentType == "Spouse")
            types += "                   <option value='Spouse' selected>Spouse</option> \n";
        else
            types += "                   <option value='Spouse'>Spouse</option> \n";
        return types;
    }

    protected static string getGenderTypes()
    {
        return "                   <option value=''> Select... </option> \n" +
               "                   <option value='Male'>Male</option> \n" +
               "                   <option value='Female'>Female</option> \n";
    }

    protected static string getGenderTypes(string currentType)
    {
        string types = "                   <option value=''> Select... </option> \n";

        if (currentType == "Male")
            types += "                   <option value='Male' selected>Male</option> \n";
        else
            types += "                   <option value='Male'>Male</option> \n";

        if (currentType == "Female")
            types += "                   <option value='Female' selected>Female</option> \n";
        else
            types += "                   <option value='Female'>Female</option> \n";

        return types;
    }
   
}