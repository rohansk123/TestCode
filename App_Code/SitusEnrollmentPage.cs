using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Web.Security;
using System.Globalization;
using System.Xml;
using System.Data;
using System.IO;

/// <summary>
/// Summary description for SitusEnrollmentPage
/// </summary>
public class SitusEnrollmentPage : System.Web.UI.Page
{
    //*** GLOBAL VARIABLES ***
    protected string birthdate = "";
    protected string birthdateS = "";
    protected string empID = "";
    protected string effectiveDate = "";
    protected string productCode = "";
    protected string productName = "";
    protected string extGroupNumber = "";
    protected string billingMode = "";
    protected string gender = "";
    protected string rulesXML = "";

    protected int numOfBeneficiaries = 0;
    protected int numOfPanelsInQuestionnaire = 0;
    protected double chr = 0;
    protected double guaranteedIssue = -1;
    protected double guaranteedFaceValue = 0;
    protected double guaranteedCost = 0;
    protected double guaranteedIssueSpouse = -1;
    protected double guaranteedFaceValueSpouse = 0;
    protected double guaranteedCostSpouse = 0;
    protected double minimumElection = 0;

    protected bool hasDependents = false;
    protected bool showSelectDependentsBox = false;
    protected bool hasSBHQ = false;
    protected bool hasActiveHQ = false;
    protected bool usingEEAge = false;
    protected bool showBeneficiariesList = false;
    protected bool spouseRatesDifferent = false;
    protected bool hasERTable = false;
    protected bool hasEOI = false;
    protected bool hasGender = false;
    protected bool hasSmoker = false;
    protected bool hasSpouse = false;
    protected bool hasChild = false;
    protected bool hasAgeBand = false;
    protected bool hasUpperLimitGI = false;
    protected bool hasAge = false;
    protected bool hasSalaryBand = false;
    protected bool hasJobClass = false;
    protected bool hasLocation = false;
    protected bool hasState = false;
    protected bool hasBillingMode = false;
    protected bool hasCoverageLevel = false;
    protected bool hasFaceValue = false;
    protected bool hasTier = false;
    protected bool appendExistingPDF = false;
    protected bool isCombinedLife;

    protected List<Dictionary<string, string>> CensusData = null;
    protected SqlDataReader myReader = null;
    protected Panel signaturePanel = null;

    //*** CREATE THE SQL CONNECTION ***

    protected SqlConnection myConnection = new SqlConnection(SiteMaster.MyGlobals.connectionString);

    protected void Page_PreInit(object sender, EventArgs e)
    {
        try
        {
            SiteMaster.MyGlobals.myConnection.Close();
            //*** GET DEPENDENTS DATA ***
            //Response.Write("Session empid: " + (string)Session["empID"]);
            if (String.IsNullOrWhiteSpace((string)Session["group"]) || String.IsNullOrWhiteSpace((string)Session["empID"]))
                throw new RedirectException();
            else
            {
                if (Session["EEData"] != null)
                    CensusData = (List<Dictionary<string, string>>)Session["EEData"];
                else
                {
                    CensusData = SiteMaster.getEEData(true, (string)Session["empID"]);
                    //Response.Write((string)Session["empID"]);
                }

                if (CensusData == null) Response.Redirect(Page.ResolveUrl("~") + "Default.aspx");
                else if (CensusData.Count > 1) hasDependents = true;

                birthdate = CensusData[0]["birthdate"];

                foreach (Dictionary<string, string> data in CensusData)
                {
                    if (data["type"] == "Spouse")
                    {
                        hasSpouse = true;
                        birthdateS = data["birthdate"];
                    }
                    if (data["type"] == "Child")
                        hasChild = true;
                }
            }
            myConnection.Close();
        }
        catch (NullReferenceException error)
        {
            foreach (string key in Session.Keys)
            {
                Response.Write("<br /> \n" + key + " - " + Session[key]);
            }
            Response.Write("<br /> \n" + (string)Session["group"] + (string)Session["empID"]);
            Response.Write("<br /> \n" + error);
            //ClientScript.RegisterClientScriptBlock(this.GetType(), "RedirectToHome", "self.parent.location='" + Page.ResolveUrl("~") + "productsGrid.aspx?error=1';", true);
        }
        catch (RedirectException error)
        {
            //Response.Write("<br /> \n" + error);
            ClientScript.RegisterClientScriptBlock(this.GetType(), "RedirectToHome", "self.parent.location='" + Page.ResolveUrl("~") + "productsGrid.aspx?error=1';", true);
        }
    }







    protected string createRatesTable(string productCode)
    {
        string rateTable = null;
        Session["erPaid"] = false;
        Session.Remove("hiddenVars");
        // SqlDataReader sql_TableInfo = new SqlCommand("SELECT [COLUMN_NAME] FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + productCode +
        //   "_RT' AND TABLE_SCHEMA = '" + Session["group"] + "'", myConnection).ExecuteReader();

        SqlDataReader sql_TableInfo = new SqlCommand("SELECT [COLUMN_NAME] FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + productCode +
           "_FT' AND TABLE_SCHEMA = '" + Session["group"] + "'", myConnection).ExecuteReader();

        if (sql_TableInfo.HasRows)
        {

            while (sql_TableInfo.Read())
            {
                if (!hasCoverageLevel)
                    hasCoverageLevel = sql_TableInfo["COLUMN_NAME"].ToString() == "coverage_level";

                if (!hasFaceValue)
                    hasFaceValue = sql_TableInfo["COLUMN_NAME"].ToString() == "face_value";
                if (!hasTier)
                    hasTier = sql_TableInfo["COLUMN_NAME"].ToString() == "tier";

                if (!hasSalaryBand)
                    hasSalaryBand = sql_TableInfo["COLUMN_NAME"].ToString() == "salary";

                if (!hasAgeBand)
                    hasAgeBand = (sql_TableInfo["COLUMN_NAME"].ToString() == "age_low" || sql_TableInfo["COLUMN_NAME"].ToString() == "age_high");

                if (!hasAge)
                    hasAge = sql_TableInfo["COLUMN_NAME"].ToString() == "age";

                if (!hasGender)
                    hasGender = sql_TableInfo["COLUMN_NAME"].ToString() == "gender";

                if (!spouseRatesDifferent)
                {
                    spouseRatesDifferent = sql_TableInfo["COLUMN_NAME"].ToString() == "ee_or_spouse";

                }

                if (!hasJobClass)
                    hasJobClass = sql_TableInfo["COLUMN_NAME"].ToString() == "jobclass";
                if (!hasLocation)
                    hasLocation = sql_TableInfo["COLUMN_NAME"].ToString() == "location";

                if (!hasState)
                    hasState = sql_TableInfo["COLUMN_NAME"].ToString() == "state";

                if (!hasBillingMode)
                    hasBillingMode = sql_TableInfo["COLUMN_NAME"].ToString() == "billing_mode";

                if (!(bool)Session["erPaid"])
                    Session["erPaid"] = sql_TableInfo["COLUMN_NAME"].ToString() == "er_cost";

                if (!(bool)Session["erPaid"])
                    Session["erPaid"] = sql_TableInfo["COLUMN_NAME"].ToString() == "er_factor";

            }
            sql_TableInfo.Close();

            if (hasJobClass && String.IsNullOrWhiteSpace((string)Session["jobClass"]))
                throw new Exception("This person's <b>Job Class</b> has not been set.");


            rateTable = createFactoredRateTable(productCode);


            //   return rateTable;

        }

        else
        {
            sql_TableInfo = new SqlCommand("SELECT [COLUMN_NAME] FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + productCode +
          "_RT' AND TABLE_SCHEMA = '" + Session["group"] + "'", myConnection).ExecuteReader();

            if (sql_TableInfo.HasRows)
            {
                while (sql_TableInfo.Read())
                {
                    if (!hasCoverageLevel)
                        hasCoverageLevel = sql_TableInfo["COLUMN_NAME"].ToString() == "coverage_level";

                    if (!hasFaceValue)
                        hasFaceValue = sql_TableInfo["COLUMN_NAME"].ToString() == "face_value";
                    if (!hasTier)
                        hasTier = sql_TableInfo["COLUMN_NAME"].ToString() == "tier";

                    if (!hasSalaryBand)
                        hasSalaryBand = sql_TableInfo["COLUMN_NAME"].ToString() == "salary";

                    if (!hasAgeBand)
                        hasAgeBand = (sql_TableInfo["COLUMN_NAME"].ToString() == "age_low" || sql_TableInfo["COLUMN_NAME"].ToString() == "age_high");

                    if (!hasAge)
                        hasAge = sql_TableInfo["COLUMN_NAME"].ToString() == "age";

                    if (!hasGender)
                        hasGender = sql_TableInfo["COLUMN_NAME"].ToString() == "gender";
                    if (!hasJobClass)
                        hasJobClass = sql_TableInfo["COLUMN_NAME"].ToString() == "jobclass";

                    if (!hasLocation)
                        hasLocation = sql_TableInfo["COLUMN_NAME"].ToString() == "location";

                    if (!hasState)
                        hasState = sql_TableInfo["COLUMN_NAME"].ToString() == "state";

                    if (!spouseRatesDifferent)
                    {
                        spouseRatesDifferent = sql_TableInfo["COLUMN_NAME"].ToString() == "ee_or_spouse";
                        //isCombinedLife = true;
                    }

                    if (!(bool)Session["erPaid"])
                        Session["erPaid"] = sql_TableInfo["COLUMN_NAME"].ToString() == "er_cost";

                    if (!(bool)Session["erPaid"])
                        Session["erPaid"] = sql_TableInfo["COLUMN_NAME"].ToString() == "er_factor";
                }
                sql_TableInfo.Close();
            }
            hasSmoker = Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + productCode +
                "S_RT' AND TABLE_SCHEMA = '" + Session["group"] + "') then 1 \n   else 0 \n end"));

            hasERTable = Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + productCode +
                "_ER_RT' AND TABLE_SCHEMA = '" + Session["group"] + "') then 1 \n   else 0 \n end"));

            if (hasJobClass && String.IsNullOrWhiteSpace((string)Session["jobClass"]))
                throw new Exception("This person's <b>Job Class</b> has not been set.");

            // Response.Write("I reach here");
            //Response.Write(Session["hasHQ"].ToString());
            //Response.Write("hassalaryband:"+hasSalaryBand);
            //Response.Write("hasageband:" + hasAgeBand);

            //Response.Write("hasTier:" + hasTier);
            //Response.Write(" hasCoverageLevel:" + hasCoverageLevel);

            if ((bool)Session["hasHQ"])
                rateTable = createRateTableWithHQ(productCode);

            else if (hasSalaryBand)
                rateTable = createSBRateTable(productCode);
            else if ((hasAgeBand || hasAge) && !hasCoverageLevel)
                rateTable = createABRateTable(productCode);
            else if (hasTier)
            {

                if (hasCoverageLevel)
                    rateTable = createMTRateTable(productCode);
                else
                    rateTable = createSTRateTable(productCode);
                //  if (CensusData.Count > 1) showSelectDependentsBox = true;
            }
        }
        return rateTable;
    }

    public static Dictionary<int, List<string>> getPanelMessages(string productCode)
    {
        Dictionary<int, List<string>> panelMessages = new Dictionary<int, List<string>>();

        string filePath = SiteMaster.getFolderPath("Documents");
        //Response.Write(filePath);
        XmlDocument messagesXml = new XmlDocument();

        if (File.Exists(filePath + @"\" + HttpContext.Current.Session["group"] + @"\Messages\" + productCode + "_MSGS.xml"))
        {
            messagesXml.Load(filePath + @"\" + HttpContext.Current.Session["group"] + @"\Messages\" + productCode + "_MSGS.xml");
            //HttpContext.Current.Response.Write(filePath + @"\" + HttpContext.Current.Session["group"] + @"\Messages\" + productCode + "_MSGS.xml");
            XmlNodeList popupList = messagesXml.SelectNodes("/popups/popup[@before != 'default']");

            foreach (XmlNode xn in popupList)
            {
                if (xn["active"].InnerText == "1")
                {
                    string panelIdentifier = xn.Attributes["before"].Value;
                    char panelNumber = panelIdentifier[panelIdentifier.Length - 1];
                    //Response.Write("Key " + panelNumber + " value: " + xn["message"].InnerText + "\n");
                    int panelNo = (int)Char.GetNumericValue(panelNumber);
                    List<string> nodeDetails = new List<string> { xn["size"].InnerText, xn["message"].InnerText };
                    panelMessages.Add(panelNo, nodeDetails);
                }

            }

            return panelMessages;
        }
        else
            return null;
    }



    public static string getDefaultPopupText(string productCode)
    {
        string dialogContent = string.Empty;

        string filePath = SiteMaster.getFolderPath("Documents");
        //Response.Write(filePath);
        XmlDocument messagesXml = new XmlDocument();

        if (File.Exists(filePath + @"\" + HttpContext.Current.Session["group"] + @"\Messages\" + productCode + "_MSGS.xml"))
        {
            messagesXml.Load(filePath + @"\" + HttpContext.Current.Session["group"] + @"\Messages\" + productCode + "_MSGS.xml");
            if (messagesXml.SelectSingleNode("/popups/popup[@before='default']") != null)
                dialogContent = messagesXml.SelectSingleNode("/popups/popup[@before='default']/message").InnerText;

        }
        return dialogContent;
    }

    protected bool panelsExist()
    {
        if (numOfPanelsInQuestionnaire > 0)
            return true;
        else
            return false;
    }

    protected bool combinedLife(string productCode)
    {
        SqlDataReader sql_TableInfo = new SqlCommand("SELECT [COLUMN_NAME] FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + productCode +
            "_RT' AND TABLE_SCHEMA = '" + Session["group"] + "'", myConnection).ExecuteReader();
        while (sql_TableInfo.Read())
        {
            //   if (!hasCoverageLevel)
            if (sql_TableInfo["COLUMN_NAME"].ToString() == "ee_or_spouse")
                return true;
        }

        return false;
    }

    //private string createRateTableWithHQ(string productCode)
    //{
    //    string filePath = SiteMaster.getFolderPath("Documents");
    //    //Response.Write(filePath);
    //    XmlDocument healthQuestions = new XmlDocument();

    //   // healthQuestions.Load(filePath + @"\" + Session["group"] + @"\Questionnaires\" + productCode + "_HQ.htm");
    //    if (File.Exists(filePath + @"\" + Session["group"] + @"\Questionnaires\" + productCode + "_HQ.htm"))
    //        healthQuestions.Load(filePath + @"\" + Session["group"] + @"\Questionnaires\" + productCode + "_HQ.htm");

    //    if (!healthQuestions.InnerXml.Contains("data-role=\"panel\""))
    //    {
    //        numOfPanelsInQuestionnaire = 0;
    //    }
    //    else
    //        numOfPanelsInQuestionnaire = 1;

    //    if (healthQuestions.InnerXml.Contains("<SHOW_DEPENDENTS_BOX />") && CensusData.Count > 1)
    //        showSelectDependentsBox = true;

    //    if (healthQuestions.InnerXml.Contains("<CURRENT_DEDUCTIONS_TABLE />"))
    //        healthQuestions.InnerXml.Replace("<RATES_TABLE />", addCurrentDeductions());

    //    if (productCode.Contains("LOYAL_CRI"))
    //        return healthQuestions.InnerXml.Replace("<RATES_TABLE />", createCriticalIllnessTable(birthdate));
    //    else if (productCode.Substring(productCode.Length - 3, 3) == "_CH")
    //        return healthQuestions.InnerXml.Replace("<RATES_TABLE />", createChildrensLifeRateTable());
    //    else if (hasAgeBand && hasSalaryBand)
    //        return healthQuestions.InnerXml.Replace("<RATES_TABLE />", createABSBRateTable(productCode, (string)Session["salary"], birthdate));
    //    else if (hasSalaryBand)
    //        return healthQuestions.InnerXml.Replace("<RATES_TABLE />", createSBRateTable((string)Session["salary"]));
    //    else if ((hasAgeBand || hasAge) && hasFaceValue)
    //        return healthQuestions.InnerXml.Replace("<RATES_TABLE />", createABRateTable(productCode));
    //    else if ((hasAgeBand || hasAge) && hasCoverageLevel)
    //        return healthQuestions.InnerXml.Replace("<RATES_TABLE />", createABMTRateTable(productCode));
    //    else if (hasAgeBand || hasAge)
    //        return healthQuestions.InnerXml.Replace("<RATES_TABLE />", createABSTRateTable(productCode));
    //    else if (hasCoverageLevel)
    //        return healthQuestions.InnerXml.Replace("<RATES_TABLE />", createCRateTablewHQ(productCode));
    //    else
    //        return healthQuestions.InnerXml.Replace("<RATES_TABLE />", createSTRateTable(productCode));
    //}

    //new function pulled from situs on 4/20/16
    private string createRateTableWithHQ(string productCode)
    {
        // Initialize();
       // Response.Write("rtwithhq");
        string filePath = SiteMaster.getFolderPath("Documents");
        string healthQuestions = "";
        if (File.Exists(filePath + @"\" + Session["group"] + @"\Questionnaires\" + productCode + "_HQ.htm"))
            healthQuestions = File.ReadAllText(filePath + @"\" + Session["group"] + @"\Questionnaires\" + productCode + "_HQ.htm");


        if (!healthQuestions.Contains("data-role=\"panel\""))
        {
            numOfPanelsInQuestionnaire = 0;
        }
        else
            numOfPanelsInQuestionnaire = 1;


        if (Session["group"] == "agilityfs" && productCode == "LOYAL_CA3" &&
            Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM " + Session["currentAgency"] + "." + Session["group"] + ".Employees " +
            " WHERE empID = '" + Session["empID"] + "' AND states_written_in = 'CA') then 1 \n   else 0 \n end")))
        {
            healthQuestions = "";
            healthQuestions = File.ReadAllText(filePath + @"\" + Session["group"] + @"\Questionnaires\LOYAL_CA3_CA_HQ.htm");
        }

        showSelectDependentsBox = (healthQuestions.Contains("<SHOW_DEPENDENTS_BOX />") && CensusData.Count > 1);


        hasSBHQ = healthQuestions.Contains("<NO_SB_HQ />");

        hasActiveHQ = healthQuestions.Contains("name='active'") || healthQuestions.Contains("name=\"active\"");

        if (healthQuestions.Contains("<CURRENT_DEDUCTIONS_TABLE />"))
            healthQuestions.Replace("<CURRENT_DEDUCTIONS_TABLE />", addCurrentDeductions());

        string ratesTable = "";


        if (productCode.Substring(productCode.Length - 3, 3) == "_CH")
            ratesTable = createChildrensLifeRateTable();

        else if (hasSalaryBand)
            ratesTable = createSBRateTable(productCode);
        else if ((hasAgeBand || hasAge) && hasFaceValue && hasTier)
            ratesTable = createCriticalIllnessTable(productCode);
        else if ((hasAgeBand || hasAge) && hasFaceValue)
            ratesTable = createABRateTable(productCode);
        else if (hasCoverageLevel)
            ratesTable = createCRateTablewHQ(productCode);
        else if (hasTier)
            ratesTable = createSTRateTable(productCode);

        if (!String.IsNullOrWhiteSpace(healthQuestions))
            return healthQuestions.Replace("<RATES_TABLE />", ratesTable);
        else
            return ratesTable;
    }


    private string createChildrensLifeRateTable()
    {
        string table = "<table style='width:100%' class='table table-condensed table-bordered'><tbody> \n" +
                             "<tr><td colspan='2' bgcolor='#ffffff'><font color='#000000'>Please enter the Child's data:</font></td></tr> \n" +
                             "<tr><td style='width: 300px'>Name: </td> <td style='vertical-align:middle'> <input type='text' id='childName' name='childName' /></td></tr> \n" +
                             "<tr><td style='width: 300px'>Relation: </td> <td style='vertical-align:middle'> <input type='text' id='tier' name='tier' /></td></tr> \n" +
                             "<tr><td style='width: 300px'>Date of Birth: </td>" +
                             "    <td style='vertical-align:middle'> <input type='text' name='childDOB_M' style='width:20px' maxlength='2' />" +
                             "         / <input type='text' name='childDOB_D' style='width:20px' maxlength='2' />" +
                             "         / <input type='text' name='childDOB_Y' style='width:50px' maxlength='4' /></td></tr> \n" +
                             "<tr><td style='width: 300px'>Gender: </td> \n" +
                             "    <td style='vertical-align:middle'> <label><input type='radio' id='gender_0' name='childGender' value='M' /> Male</label> \n " +
                             "       <label><input type='radio' id='gender_1' name='childGender' value='F' /> Female</label> </td></tr> \n" +
                             "<tr><td style='width: 300px'>Tobacco: </td> \n" +
                             "    <td style='vertical-align:middle'> <label><input type='radio' id='smoker_0' name='childSmoker' value='No' /> No</label> \n " +
                             "       <label><input type='radio' id='smoker_1' name='childSmoker' value='Yes' /> Yes</label> </td></tr> \n";

        table += "<tr><td>Is the employee actively at work performing the regular duties of the job in the usual manner and the usual place of employment?</td> \n" +
            "<td> <input type='radio' name='active' value='No'> No " +
            "<input type='radio' name='active' value='Yes'> Yes " + "</td></tr><br><br> \n";

        table += "<tr><td>Is the Proposed Insured a U.S. Citizen or a permanent resident?</td> \n" +
            "<td> <input type='radio' name='resident' value='No'> No " +
            "<input type='radio' name='resident' value='Yes'> Yes " + "</td></tr> \n";
        table += "</tbody></table> \n";
        return table;
    }

    private string createCriticalIllnessTable(string productCode)
    {
        //*** SETUP TABLE ***
        string table = "";
        string riderTable = "";
        string tableS = "";
        bool hasSPRates = false;
        bool usingEEAge = false;

        SqlDataReader myReader2 = null;
        string SQL = null;

        try
        {
            if (String.IsNullOrWhiteSpace(effectiveDate))
                throw new Exception("The effective date for this employee has not been set.");
            if (String.IsNullOrWhiteSpace(birthdate))
                throw new AgeException("The birthdate for this employee has not been set.");
            int age = ageAtEffectiveDate(birthdate, effectiveDate);

            if (productCode.Substring(productCode.Length - 3, 3) == "_SP")
                age = ageAtEffectiveDate(birthdateS, effectiveDate);
            //Response.Write(age);

            int ageS = 0;
            if (hasSpouse)
            {
                if (String.IsNullOrWhiteSpace(birthdateS))
                    throw new AgeException("The birthdate for this employee's <b>Spouse</b> has not been set.");
                ageS = ageAtEffectiveDate(birthdateS, effectiveDate);
            }

            // *** FOR CERTAIN GROUPS, USE EMPLOYEE'S AGE FOR SPOUSE'S RATE TABLE ***
            if ((!productCode.Contains("FIDEL_LI") && !productCode.Contains("COMBI_LI") && !productCode.Contains("LOYAL_CR"))
                && ((string)Session["group"] == "platte"
                 || (string)Session["group"] == "mdor"
                 || (string)Session["group"] == "umass"
                 || (string)Session["group"] == "mdes"
                 || (string)Session["group"] == "marionUSD"
                 || (string)Session["group"] == "RCEC"
                 || (string)Session["group"] == "eldorado"
                 || (string)Session["group"] == "burrton"
                 || (string)Session["group"] == "Brookesmith"
                 || (string)Session["group"] == "Zephyr"
                 || (string)Session["group"] == "smrc"
                 || (string)Session["group"] == "desoto"
                 || (string)Session["group"] == "trivalley"
                 || (string)Session["group"] == "ESSDACK"
          || (string)Session["group"] == "agilityfs"))
            {
                ageS = age;
                usingEEAge = true;
            }


            if ((hasAgeBand &&
                (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age_high]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]") ||
                age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age_low]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]"))) ||
                (hasAge &&
                (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]") ||
                age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]"))))
                throw new AgeException();

            table = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTable'>" +
             "<tr><td colspan='' style='background: #fff; color:#000'><strong>Age " + age.ToString() + "</strong> on Effective Date <strong>" + String.Format("{0:MM/dd/yyyy}", effectiveDate) +
             "</strong></td> </tr> \n";

            if (hasSmoker)
                table += "<tr><td colspan='' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smoker' id='smoker' value='No' checked='checked'> No " +
                "   <input type='radio' name='smoker' id='smoker' value='Yes'> Yes </td> </tr> \n";

            if (productCode == "ALLST_CRI") table = table.Replace("Is the insured a Tobacco User?", "Has anyone to be insured used tobacco in the last 12 months?");


            table += "<tr><td width=40%><center><b>Benefit Amount</b></td>";

            int clCount = 0;
            if (hasCoverageLevel)
                clCount = (int)SiteMaster.getSingleSQLData("SELECT COUNT(DISTINCT [coverage_level]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]");

            //myReader2 = new SqlCommand("SELECT DISTINCT [coverage_level] FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]", myConnection).ExecuteReader();

            // *** INITIALIZE ADDITIONAL TABLES ***
            if (hasCoverageLevel && clCount > 1)
                riderTable = "\n <table class='table table-condensed table-bordered'><tr><td colspan='10' bgcolor='#ffffff'><font color=#000000>Critical Illness with Cancer Rider</td></tr>" +
                    "<tr><td width='40%'><center><b>Benefit Amount</td>";
            if (spouseRatesDifferent && hasSpouse)
            {
                tableS = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTableS'> \n" +
                    "<tr><td colspan='' style='background: #fff; color:#000'><strong>Spouse Coverage - Age " + ageS.ToString();
                if (usingEEAge)
                    tableS += " (using employee's age)";
                tableS += "</strong> on Effective Date <strong>" + String.Format("{0:MM/dd/yyyy}", effectiveDate) +
                "</strong></td></tr> \n";
                if (hasSmoker)
                    tableS += "<tr><td colspan='' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smokerS' id='smokerS' value='No' checked='checked'> No " +
            "<input type='radio' name='smokerS' id='smokerS' value='Yes' > Yes " + "</td> </tr> \n";

                if (productCode == "ALLST_CRI") tableS = tableS.Replace("Is the insured a Tobacco User?", "Has anyone to be insured used tobacco in the last 12 months?");
                tableS += "<td> <b>Benefit Amount</b> </td> \n";
                //tableS += "<tr class='ins_rowS alert-success'><td> <input type='radio' name='ins_optionS' value='NONE' checked> </td> <td> NONE </td> <td>  --- </td> </tr> \n";

                //table += "<tr class='ins_row'><td> <input type='radio' name='ins_option' value='NONE'> </td> <td> NONE </td> <td>  --- </td> </tr> \n";
            }
            // *** ADD TIER NAMES TO TABLE ***
            List<string> tier = new List<string>();
            List<string> attributeList = new List<string>();

            myReader2 = null;
            SQL = "SELECT [tier] FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] GROUP BY [tier] ORDER BY ";
            //Response.Write(SQL);
            if (productCode.Contains("_CRI"))
                SQL += " CASE" +
                    "    WHEN [tier] = 'Individual' THEN 0 " +
                    "    WHEN [tier] = 'Employee Only' THEN 0 " +
                    "    WHEN [tier] = 'ee' THEN 0 " +
                    "    WHEN [tier] = 'Base' THEN 0 " +
                    "    WHEN [tier] = 'Spouse Only' THEN 1 " +
                    "    WHEN [tier] = 'Employee + Spouse' THEN 1 " +
                    "    WHEN [tier] = 'spouse' THEN 1 " +
                "    WHEN [tier] = 'Cancer Rider' THEN 1 " +
                    "    WHEN [tier] = 'Single Parent' THEN 2 " +
                    "    WHEN [tier] = 'Employee + Child(ren)' THEN 2 " +
                    "    ELSE 3 " +
                    " END";
            else
                SQL += "min([cost]) ";
            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            for (int i = 0; myReader2.Read(); i++)
            {
                table += "   <td><b>" + SiteMaster.UppercaseFirst(myReader2["tier"].ToString()) + "</b></td> \n";
                if (hasCoverageLevel && clCount > 1)
                    riderTable += "   <td><b>" + SiteMaster.UppercaseFirst(myReader2["tier"].ToString()) + "</b></td> \n";
                if (spouseRatesDifferent && hasSpouse)
                    tableS += "   <td><b>" + SiteMaster.UppercaseFirst(myReader2["tier"].ToString()) + "</b></td> \n";


                tier.Add(myReader2["tier"].ToString());
            }
            table = table.Replace("colspan=''", "colspan='" + tier.Count + 1 + "'") + "</tr> \n";
            if (hasCoverageLevel && clCount > 1)
                riderTable += "</tr> \n";
            if (spouseRatesDifferent && hasSpouse)
                tableS = tableS.Replace("colspan=''", "colspan='" + tier.Count + 1 + "'") + "</tr> \n" +
                    "<tr class='ins_rowS none_row alert-success'><td> <input type='radio' name='ins_optionS' value='NONE' checked> </td> <td> NONE </td> <td>  --- </td> </tr> \n";

            string faceValue = "";
            string oldFaceValue = "";
            int tierCount = 0;
            SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] ";
            SQL += generateWhereClause() + " ORDER BY ";
            if (spouseRatesDifferent)
                SQL += " [ee_or_spouse],";

            if (hasCoverageLevel)
                SQL += " [coverage_level],";
            if (hasFaceValue)
                SQL += " [face_value],";
            if (productCode.Contains("_CRI"))
                SQL += " CASE" +
                    "    WHEN [tier] = 'Individual' THEN 0 " +
                    "    WHEN [tier] = 'Employee Only' THEN 0 " +
                    "    WHEN [tier] = 'ee' THEN 0 " +
"    WHEN [tier] = 'Base' THEN 0 " +
                    "    WHEN [tier] = 'Spouse Only' THEN 1 " +
                    "    WHEN [tier] = 'Employee + Spouse' THEN 1 " +
                    "    WHEN [tier] = 'spouse' THEN 1 " +
 "    WHEN [tier] = 'Cancer Rider' THEN 1 " +
                    "    WHEN [tier] = 'Single Parent' THEN 2 " +
  "    WHEN [tier] = 'Employee + 1' THEN 2 " +
                    "    WHEN [tier] = 'Employee + Child(ren)' THEN 2 " +
                    "    ELSE 3 " +
                    " END";
            else
                SQL += " [tier]";
            //Response.Write(SQL);

            SqlCommand myCommand = new SqlCommand(SQL, myConnection);
            myReader2 = myCommand.ExecuteReader();
            while (myReader2.Read())
            {
                int tierMod = tierCount % 3;
                faceValue = myReader2["face_value"].ToString();
                string eeSpouse = "";
                if (spouseRatesDifferent)
                    eeSpouse = myReader2["ee_or_spouse"].ToString();
                if (String.IsNullOrWhiteSpace(oldFaceValue) || faceValue != oldFaceValue)
                {
                    if (!String.IsNullOrWhiteSpace(oldFaceValue))
                        table += "</tr>\n ";

                    if (hasCoverageLevel && myReader2["coverage_level"].ToString().Contains("Cancer Rider"))
                    {
                        if (!String.IsNullOrWhiteSpace(oldFaceValue))
                            riderTable += "</tr>\n ";
                        riderTable += "<tr class='CritIllPlan smokerN'><td width='40%'>$" + SiteMaster.toCurrency(faceValue, true) + "</td>";

                    }
                    else if (!String.IsNullOrWhiteSpace(eeSpouse))
                    {
                        if (eeSpouse == "ee" || eeSpouse == "1")
                            table += "<tr class='CritIllPlan smokerN'><td width='40%'>$" + SiteMaster.toCurrency(faceValue, true) + "</td>";
                        else if (eeSpouse == "spouse" || eeSpouse == "2")
                            tableS += "<tr class='CritIllPlan smokerNS'><td width='40%'>$" + SiteMaster.toCurrency(faceValue, true) + "</td>";
                    }
                    else
                        table += "<tr class='CritIllPlan smokerN'><td width='40%' >$" + SiteMaster.toCurrency(faceValue, true) + "</td>";

                    faceValue = myReader2["face_value"].ToString();
                    oldFaceValue = myReader2["face_value"].ToString();
                }

                string entry = "   <td class='ins_row' ";
                if (myReader2["tier"].ToString().Contains("Spouse") && !hasSpouse)
                    entry += " disabled='disabled' ";

                if ((myReader2["tier"].ToString() == "Single Parent" || myReader2["tier"].ToString().Contains(" + Children") || myReader2["tier"].ToString().Contains(" + Child(ren)")) && !hasChild)
                    entry += " disabled='disabled' ";

                //   if (myReader2["tier"].ToString().Contains("Family") && (/*!hasChild ||*/ !hasSpouse))
                if (myReader2["tier"].ToString().Contains("Family") && (!hasSpouse))
                    entry += " disabled='disabled' ";

                if (productCode == "ASSUR_CRI")
                    entry += "><input type='radio' name='ins_option' face=" + myReader2["face_value"].ToString() + " value='" + myReader2["cost"].ToString() + "' " +
                    "level='" + myReader2["tier"].ToString() + "' ";
                else
                    entry += "><input type='radio' name='ins_option' face=" + myReader2["face_value"].ToString() + " value='" + myReader2["cost"].ToString() + "' " +
                    "tier='" + myReader2["tier"].ToString() + "' ";

                if (hasCoverageLevel)
                    entry += "level='" + myReader2["coverage_level"].ToString() + "' ";

                if (myReader2.HasColumn("cancer_rider_cost") && !String.IsNullOrWhiteSpace(myReader2["cancer_rider_cost"].ToString()) && myReader2.GetDouble(myReader2.GetOrdinal("cancer_rider_cost")) != 0)
                {
                    entry += "cr_cost='" + myReader2["cancer_rider_cost"].ToString() + "' ";
                }

                if (myReader2["tier"].ToString().Contains("Spouse") && !hasSpouse)
                    entry += " disabled='disabled' ";

                if ((myReader2["tier"].ToString() == "Single Parent" || myReader2["tier"].ToString().Contains(" + Children") || myReader2["tier"].ToString().Contains(" + Child(ren)")) && !hasChild)
                    entry += " disabled='disabled' ";

                // if (myReader2["tier"].ToString().Contains("Family") && (/*!hasChild ||*/ !hasSpouse))
                if (myReader2["tier"].ToString().Contains("Family") && (!hasSpouse))
                    entry += " disabled='disabled' ";

                entry += " /> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> \n";

                if (hasCoverageLevel && myReader2["coverage_level"].ToString().Contains("Cancer Rider"))
                    riderTable += entry;
                else if (!String.IsNullOrWhiteSpace(eeSpouse) && (eeSpouse == "spouse" || eeSpouse == "2"))
                    tableS += entry.Replace("name='ins_option'", "name='ins_optionS'").Replace("class='ins_row'", "class='ins_rowS'");
                else
                    table += entry;
                tierCount++;
            }
            myReader2.Close();

            if (hasSmoker)
            {
                //*** ADD SMOKER OPTIONS ***
                oldFaceValue = "";
                SQL = SQL.Insert(SQL.IndexOf(productCode) + productCode.Length, "S");
                //Response.Write(SQL);
                myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
                while (myReader2.Read())
                {
                    int tierMod = tierCount % 3;
                    faceValue = myReader2["face_value"].ToString();

                    string eeSpouse = "";
                    if (spouseRatesDifferent)
                        eeSpouse = myReader2["ee_or_spouse"].ToString();

                    if (String.IsNullOrWhiteSpace(oldFaceValue) || faceValue != oldFaceValue)
                    {
                        if (!String.IsNullOrWhiteSpace(oldFaceValue))
                            table += "</tr>\n ";

                        if (hasCoverageLevel && myReader2["coverage_level"].ToString().Contains("Cancer Rider"))
                        {
                            if (!String.IsNullOrWhiteSpace(oldFaceValue))
                                riderTable += "</tr>\n ";
                            riderTable += "<tr class='CritIllPlan smokerY'><td width='40%'>$" + SiteMaster.toCurrency(faceValue, true) + "</td>";
                        }
                        else if (!String.IsNullOrWhiteSpace(eeSpouse))
                        {
                            if (eeSpouse == "ee" || eeSpouse == "1")
                                table += "<tr class='CritIllPlan smokerY'><td width='40%'>$" + SiteMaster.toCurrency(faceValue, true) + "</td>";
                            else if (eeSpouse == "spouse" || eeSpouse == "2")
                                tableS += "<tr class='CritIllPlan smokerYS'><td width='40%'>$" + SiteMaster.toCurrency(faceValue, true) + "</td>";
                        }
                        else
                            table += "<tr class='CritIllPlan smokerY'><td width='40%'>$" + SiteMaster.toCurrency(faceValue, true) + "</td>";

                        faceValue = myReader2["face_value"].ToString();
                        oldFaceValue = myReader2["face_value"].ToString();
                    }


                    string entry = "   <td class='ins_row' ";
                    if (myReader2["tier"].ToString().Contains("Spouse") && !hasSpouse)
                        entry += " disabled='disabled' ";

                    if ((myReader2["tier"].ToString() == "Single Parent" || myReader2["tier"].ToString().Contains(" + Children") || myReader2["tier"].ToString().Contains(" + Child(ren)")) && !hasChild)
                        entry += " disabled='disabled' ";

                    // if (myReader2["tier"].ToString().Contains("Family") && (/*!hasChild ||*/ !hasSpouse))
                    if (myReader2["tier"].ToString().Contains("Family") && (!hasSpouse))
                        entry += " disabled='disabled' ";

                    entry += "><input type='radio' name='ins_option' face=" + myReader2["face_value"].ToString() + " value='" + myReader2["cost"].ToString() + "' " +
                        "tier='" + myReader2["tier"].ToString() + "' ";

                    if (hasCoverageLevel)
                        entry += "level='" + myReader2["coverage_level"].ToString() + "' ";

                    if (myReader2.HasColumn("cancer_rider_cost") && !String.IsNullOrWhiteSpace(myReader2["cancer_rider_cost"].ToString()) && myReader2.GetDouble(myReader2.GetOrdinal("cancer_rider_cost")) != 0)
                    {
                        entry += "cr_cost='" + myReader2["cancer_rider_cost"].ToString() + "' ";
                    }

                    if (myReader2["tier"].ToString().Contains("Spouse") && !hasSpouse)
                        entry += " disabled='disabled' ";

                    if ((myReader2["tier"].ToString() == "Single Parent" || myReader2["tier"].ToString().Contains(" + Children") || myReader2["tier"].ToString().Contains(" + Child(ren)")) && !hasChild)
                        entry += " disabled='disabled' ";

                    //        if (myReader2["tier"].ToString().Contains("Family") && (/*!hasChild ||*/ !hasSpouse))
                    if (myReader2["tier"].ToString().Contains("Family") && (!hasSpouse))
                        entry += " disabled='disabled' ";

                    entry += " /> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> \n";

                    if (hasCoverageLevel && myReader2["coverage_level"].ToString().Contains("Cancer Rider"))
                        riderTable += entry;
                    else if (!String.IsNullOrWhiteSpace(eeSpouse) && (eeSpouse == "spouse" || eeSpouse == "2"))
                        tableS += entry.Replace("name='ins_option'", "name='ins_optionS'").Replace("class='ins_row'", "class='ins_rowS'");

                    else
                        table += entry;
                    tierCount++;
                }
                myReader2.Close();
            }
            //*** BUILD REST OF TABLE ***
            string sbHQ = "";
            if ((bool)Session["hasHQ"] && (string)Session["group"] != "wireless")
            {
                if (!hasSBHQ)
                    sbHQ += "<tr><td valign='top' align='left' colspan='7'>State of Birth:&nbsp;&nbsp;&nbsp;" +
                                    "<input type='text' name='HQ_State_Birth' maxlength='2' size='2' />" +
                                    "<br></td></tr>";

                if (!hasActiveHQ)
                    sbHQ += "<tr><td colspan='" + tier.Count + "'>Is the employee actively at work performing the regular duties of the job in the usual manner and the<br /> usual place of employment?</td> \n" +
                        "<td> <input type='radio' name='active' value='No'> No " +
                        "<input type='radio' name='active' value='Yes'> Yes " + "</td></tr>" +
                                "<tr><td colspan='7'>&nbsp;</td></tr></table> \n ";
                if (String.IsNullOrWhiteSpace(riderTable))
                    riderTable = "<table class='table table-condensed table-bordered'>";
            }
            if (!hasSpouse)
                tableS = "";

            //System.IO.File.WriteAllText(@"C:\inetpub\wwwroot\Documents\testCI.txt", table + riderTable + sbHQ);
            if (String.IsNullOrWhiteSpace(tableS) && String.IsNullOrWhiteSpace(riderTable))
                return table + sbHQ;
            else
                return String.Join("\n    </table> \n", new string[] { table, tableS, riderTable }) + sbHQ;


        }
        catch (AgeException e)
        {
            return "<div class='alert alert-danger alert-block'> \n" +
                                "	<a class='close' data-dismiss='alert' href='../ProductsGrid.aspx'>Go Back</a> \n" +
                                "	<h4 class='alert-heading'>Error!</h4> \n" +
                                "	Due to age restrictions, this person is ineligible for this product. \n" +
                                "</div>";
        }
        catch (Exception e)
        {
            return "<div class='alert alert-danger alert-block'> \n" +
                                   "	<a class='close' data-dismiss='alert' href='../ProductsGrid.aspx'>Go Back</a> \n" +
                                   "	<h4 class='alert-heading'>Error!</h4> \n" +
                                   "An error has occured while creating the " + productName + " rates table: " + e.Message +
                                   "<p /> Source: " + e.StackTrace + "</div>";
        }


    }

    private string createABRateTable(string productCode)
    {
       // Response.Write("in abratetable");
        //*** SETUP TABLE ***
        string table = "";
        string tableS = "";
        bool hasSPRates = false;
        bool usingEEAge = false;
        //Response.Write(productCode);

        try
        {
            if (String.IsNullOrWhiteSpace(effectiveDate))
                throw new Exception("The effective date for this employee has not been set.");
            if (String.IsNullOrWhiteSpace(birthdate))
                throw new AgeException("The birthdate for this employee has not been set.");
            int age = ageAtEffectiveDate(birthdate, effectiveDate);
            int ageS = 0;
            if (hasSpouse)
            {
                if (String.IsNullOrWhiteSpace(birthdateS))
                    throw new AgeException("The birthdate for this employee's <b>Spouse</b> has not been set.");
                ageS = ageAtEffectiveDate(birthdateS, effectiveDate);
            }

            // *** FOR CERTAIN GROUPS, USE EMPLOYEE'S AGE FOR SPOUSE'S RATE TABLE ***

            if ((!productCode.Contains("FIDEL_LI") && !productCode.Contains("COMBI_LI") && !productCode.Contains("UNUM"))

               && ((string)Session["group"] == "platte"
                || (string)Session["group"] == "mdor"
                || (string)Session["group"] == "umass"
                || (string)Session["group"] == "mdes"
                || (string)Session["group"] == "marionUSD"
                || (string)Session["group"] == "RCEC"
                || (string)Session["group"] == "eldorado"
                || (string)Session["group"] == "burrton"
                || (string)Session["group"] == "Brookesmith"
                || (string)Session["group"] == "Zephyr"
                || (string)Session["group"] == "smrc"
                || (string)Session["group"] == "desoto"
                || (string)Session["group"] == "trivalley"
                || (string)Session["group"] == "ESSDACK"
                || (string)Session["group"] == "NKC"
        || (string)Session["group"] == "keystone"
                || ((string)Session["group"] == "agilityfs" && productCode == "ASSUR_CRI")))
            {
                ageS = age;
                usingEEAge = true;
            }

            if ((hasAgeBand &&
                (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age_high]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]") ||
                age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age_low]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]"))) ||
                (hasAge &&
                (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]") ||
                age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]"))))
                throw new AgeException();

            string attributes = "";

            table = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTable'> \n" +
                "<tr><td colspan='3' style='background: #fff; color:#000'><strong>Age " + age.ToString() + "</strong> on Effective Date <strong>" + String.Format("{0:MM/dd/yyyy}", effectiveDate) +
                "</strong></td> </tr> \n";
            if (hasSmoker)
                table += "<tr><td colspan='3' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smoker' id='smoker' value='No' checked='checked'> No " +
                "   <input type='radio' name='smoker' id='smoker' value='Yes'> Yes </td> </tr> \n";

            table += "<tr><td> &nbsp; </td>";
            if (hasCoverageLevel)
                table += " <td>Coverage Amount </td>";
            if (hasFaceValue)
                table += " <td>Face Value </td>";
            table += " <td>Premium </td> </tr> \n";


            if (spouseRatesDifferent && hasSpouse)
            {
                tableS = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTableS'> \n" +
                    "<tr><td colspan='3' style='background: #fff; color:#000'><strong>Spouse Coverage - Age " + ageS.ToString();
                if (usingEEAge)
                    tableS += " (using employee's age)";
                tableS += "</strong> on Effective Date <strong>" + String.Format("{0:MM/dd/yyyy}", effectiveDate) +
                "</strong></td></tr> \n";
                if (hasSmoker)
                    tableS += "<tr><td colspan='3' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smokerS' id='smokerS' value='No' checked='checked'> No " +
            "<input type='radio' name='smokerS' id='smokerS' value='Yes' > Yes " + "</td> </tr> \n";
                tableS += "<tr><td> &nbsp; </td>";
                if (hasCoverageLevel)
                    tableS += " <td>Coverage Amount </td>";
                if (hasFaceValue)
                    tableS += " <td>Face Value </td>";
                tableS += "<td> Premium </td> </tr> \n";
                tableS += "<tr class='ins_rowS none_row alert-success'><td> <input type='radio' name='ins_optionS' value='NONE' checked> </td> <td> NONE </td> <td>  --- </td> </tr> \n";

                //table += "<tr class='ins_row'><td> <input type='radio' name='ins_option' value='NONE'> </td> <td> NONE </td> <td>  --- </td> </tr> \n";

                if (productCode != "MUTUA_LIF")
                {
                    //  Response.Write("in ab rate");
                    table += "<tr class='ins_row'><td> <input type='radio' name='ins_option' value='NONE'> </td> <td> NONE </td> <td>  --- </td> </tr> \n";
                }
            }


            //*** ADD NON-SMOKER OPTIONS ***
            SqlDataReader myReader2 = null;
            string SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]" + generateWhereClause();
            
            //Response.Write(SQL);


            if ((string)Session["group"] == "marion") SQL += " AND [face_value]<=" + (Convert.ToDouble(Session["salary"]) * 5);


            if (hasFaceValue)
                SQL += " ORDER BY [face_value]";

            //Response.Write(SQL);
            int count = 0;
            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            while (myReader2.Read())
            {
                // if (Convert.ToDouble(myReader2["cost"].ToString()) != 0 || (productName.Contains("ER Paid")))
                if (Convert.ToDouble(myReader2["cost"].ToString()) != 0 || (productName.Contains("ER Paid")) || (productName.Contains("Group Paid")) || (productName.Contains("District Paid")) || (myReader2.HasColumn("er_cost") && Convert.ToDouble(myReader2["er_cost"].ToString()) > 0))
                {
                    attributes = "";
                    if (hasCoverageLevel)
                        attributes += " level='" + myReader2["coverage_level"].ToString() + "'";
                    if (hasFaceValue)
                        attributes += " face='" + myReader2["face_value"].ToString() + "'";
                    if ((bool)Session["erPaid"])
                        attributes += " er_cost='" + myReader2["er_cost"].ToString() + "'";

                    //*** ADD INSURANCE OPTIONS ***
                    if (spouseRatesDifferent)
                    {
                        count++;
                        string eeSpouse = myReader2["ee_or_spouse"].ToString();
                        if (eeSpouse == "ee" || eeSpouse == "1")
                        {
                            table += "<tr class='ins_row smokerN'><td> <input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "'" + attributes + " > </td> ";
                            if (hasCoverageLevel)
                                table += " <td> " + SiteMaster.UppercaseFirst(myReader2["coverage_level"].ToString()) + "</td>";
                            if (hasFaceValue)
                                table += " <td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td>";
                            table += "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
                        }
                        if ((eeSpouse == "spouse" || eeSpouse == "2") && hasSpouse)
                        {
                            tableS += "<tr class='ins_rowS smokerNS'><td> <input type='radio' name='ins_optionS' value='" + myReader2["cost"].ToString() + "'" + attributes + "> </td> ";
                            if (hasCoverageLevel)
                                tableS += " <td> " + SiteMaster.UppercaseFirst(myReader2["coverage_level"].ToString()) + "</td>";
                            if (hasFaceValue)
                                tableS += " <td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td>";
                            tableS += "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
                            hasSPRates = true;
                        }
                    }
                    else
                    {
                        //GROUP SPECIFIC CODE: product UI
                        if ((Session["group"].ToString() == "wireless" || Session["group"].ToString() == "newtest") && productCode.Contains("UNITE"))
                        {
                            table += "<tr class='ins_row smokerN alert-success'><td> <input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "'" + attributes + " checked='checked'> </td> ";
                            //table += "<script>$(window).load(function() {$('input[name='face_value').val('15000');});</script>";
                        }
                        else
                            table += "<tr class='ins_row smokerN'><td> <input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "'" + attributes + "> </td> ";

                        if (hasCoverageLevel)
                            table += " <td> " + SiteMaster.UppercaseFirst(myReader2["coverage_level"].ToString()) + "</td>";
                        if (hasFaceValue)
                            table += " <td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td>";
                        table += "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
                    }
                }
            }

            if (hasSmoker)
            {
                //*** ADD SMOKER OPTIONS ***
                SQL = SQL.Insert(SQL.IndexOf(productCode) + productCode.Length, "S");
                //Response.Write(SQL);

                myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
                while (myReader2.Read())
                {
                    if (Convert.ToDouble(myReader2["cost"].ToString()) != 0)
                    {
                        attributes = "";
                        if (hasCoverageLevel)
                            attributes += " level='" + myReader2["coverage_level"].ToString() + "'";
                        if (hasFaceValue)
                            attributes += " face='" + myReader2["face_value"].ToString() + "'";

                        if (spouseRatesDifferent)
                        {
                            //*** ADD INSURANCE OPTIONS ***
                            string eeSpouse = myReader2["ee_or_spouse"].ToString();
                            if (eeSpouse == "ee" || eeSpouse == "1")
                            {
                                table += "<tr class='ins_row smokerY'><td> <input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "'" + attributes + "> </td>";
                                if (hasCoverageLevel)
                                    table += " <td> " + SiteMaster.UppercaseFirst(myReader2["coverage_level"].ToString()) + "</td>";
                                if (hasFaceValue)
                                    table += " <td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td>";
                                table += "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
                            }
                            if ((eeSpouse == "spouse" || eeSpouse == "2") && hasSpouse)
                            {
                                tableS += "<tr class='ins_rowS smokerYS'><td> <input type='radio' name='ins_optionS' value='" + myReader2["cost"].ToString() + "'" + attributes + "> </td> ";
                                if (hasCoverageLevel)
                                    tableS += " <td> " + SiteMaster.UppercaseFirst(myReader2["coverage_level"].ToString()) + "</td>";
                                if (hasFaceValue)
                                    tableS += " <td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td>";
                                tableS += "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
                                hasSPRates = true;
                            }

                        }
                        else
                        {
                            table += "<tr class='ins_row smokerY'><td> <input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "'" + attributes + "> </td> ";
                            if (hasCoverageLevel)
                                table += " <td> " + SiteMaster.UppercaseFirst(myReader2["coverage_level"].ToString()) + "</td>";
                            if (hasFaceValue)
                                table += " <td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td>";
                            table += "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";

                        }
                    }
                }
            }

            //*** BUILD REST OF TABLE ***
            if ((bool)Session["hasHQ"])
            {
                if (!hasActiveHQ)
                    table += "<tr><td colspan='2'>Is the employee actively at work performing the regular duties of the job in the usual manner and the usual place of employment?</td> \n" +
                        "<td style='display:block'><span class='reqQuestions'><label><input type='radio' name='active' value='Yes'> Yes </label> </span>" +
                        "<span class='reqQuestions'><label> <input type='radio' name='active' value='No'> No </label> </span></td></tr> \n";

                table += "<tr><td colspan='2'>Is the Proposed Insured a U.S. Citizen or a permanent resident?</td> \n" +
                    "<td style='display:block'><span class='reqQuestions'> <label><input type='radio' name='resident' value='Yes'> Yes </label> </span>" +
                    "<span class='reqQuestions'><label><input type='radio' name='resident' value='No'> No </label> </span></td></tr> \n";

                if (spouseRatesDifferent && hasSpouse)
                {
                    tableS += "<tr><td colspan='2'>Is the Proposed Insured a U.S. Citizen or a permanent resident?</td> \n" +
                        "<td><span class='reqQuestions'><span class='reqQuestions'><label><input type='radio' name='residentS' value='Yes'> Yes </label> </span>" +
                        "<span class='reqQuestions'><label> <input type='radio' name='residentS' value='No'> No </label> </span></td></tr> \n";
                    if (productCode.Contains("COMBI_") || productCode.Contains("FIDEL_"))
                        tableS += "<tr><td colspan='2'>If applying for coverage, is your Spouse currently hospitalized, receiving home health care or receiving or applying to receive disability benefits? </td> \n" +
                   "<td style='display:block'><span class='reqQuestions'> <label><input type='radio' name='HQ_1B' value='Yes' validate='false'> Yes </label> </span>" +
                   "<span class='reqQuestions'><label><input type='radio' name='HQ_1B' value='No' validate='false'> No </label> </span></td></tr> \n";


                }
            }

            table += "</table> <br/>";
            //Response.Write(hasSpouse);
            if (hasSpouse)
            {
                if (spouseRatesDifferent)
                //    tableS += "</table> <br/>";
                //else if(!productName.Contains("ER Paid"))
                //    table += createABRateTable(ageS);

                //if (hasSPRates == false)
                //    tableS = "";
                {
                    tableS += "</table> <br/>";

                    if (hasSPRates == false)
                        tableS = "<div class='alert alert-danger alert-block'> \n" +
                                    "	<h4 class='alert-heading'>Error!</h4> \n" +
                                    "	Due to age restrictions, this person's <b>Spouse</b> is ineligible for this product. \n" +
                                    "   <input type='hidden' name='ins_optionS' value='NONE' /> \n " +
                                    "</div>";
                }

                table += tableS;
            }
        }
        catch (AgeException e)
        {
            return "<div class='alert alert-danger alert-block'> \n" +
                                "	<a class='close' data-dismiss='alert' href='../ProductsGrid.aspx'>Go Back</a> \n" +
                                "	<h4 class='alert-heading'>Error!</h4> \n" +
                                "	Due to age restrictions, this person is ineligible for this product. \n" +
                                "</div>";
        }
        catch (Exception e)
        {
            return "<div class='alert alert-danger alert-block'> \n" +
                                   "	<a class='close' data-dismiss='alert' href='../ProductsGrid.aspx'>Go Back</a> \n" +
                                   "	<h4 class='alert-heading'>Error!</h4> \n" +
                                   "An error has occured while creating the " + productName + " rates table: " + e.ToString() +
                                   "<p /> Source: " + e.StackTrace + "</div>";
        }
        return table;
    }




    private string createABRateTable(int age)
    {
        if (String.IsNullOrWhiteSpace(effectiveDate))
            throw new Exception("The effective date for this employee has not been set.");
        if (String.IsNullOrWhiteSpace(birthdateS))
            throw new AgeException("The birthdate for this employee's <b>Spouse</b> has not been set.");

        string table;
        bool hasSPRates = false;
        //*** SETUP TABLE ***
        table = "<table style='width:100%' class='table table-condensed table-bordered' id='rateTableS'> \n" +
            "<tr><td colspan='3' style='background: #fff; color:#000'><strong>SPOUSE COVERAGE</strong> - <strong> Age " + age.ToString() + "</strong>" +
                " on Effective Date " + String.Format("{0:MM/dd/yyyy}", effectiveDate) + "</td></tr> \n";
        if (hasSmoker)
            table += "<tr><td colspan='3' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smokerS' id='smokerS' value='No' checked='checked'> No " +
            "<input type='radio' name='smokerS' id='smokerS' value='Yes' > Yes " + "</td> </tr> \n";

        table += "<tr><td> &nbsp; </td>";
        if (hasCoverageLevel)
            table += " <td>Coverage Amount </td>";
        if (hasFaceValue)
            table += " <td>Face Value </td>";

        // table += "<tr class='ins_rowS'><td> <input type='radio' name='ins_optionS' value='NONE'> </td> <td> NONE </td> <td>  --- </td> </tr> \n";

        //*** ADD NON-SMOKER OPTIONS ***
        SqlDataReader myReader2 = null;
        string SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] ";
        //Response.Write(SQL);

        if (hasAgeBand) SQL += " WHERE [age_low]<=" + age.ToString() + " AND [age_high]>=" + age.ToString();
        else SQL += " WHERE [age]=" + age.ToString();
        if (hasJobClass)
            SQL += " AND [jobclass]='" + Session["jobClass"] + "'";

        table += "<tr class='ins_rowS none_row alert-success'><td> <input type='radio' name='ins_optionS' value='NONE' checked> </td> <td> NONE </td> <td>  --- </td> </tr> \n";

        myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
        while (myReader2.Read())
        {
            if (Convert.ToDouble(myReader2["cost"].ToString()) != 0)
                table += "<tr class='ins_rowS smokerNS'><td> <input type='radio' name='ins_optionS' id='smokerNS' value='" + myReader2["cost"].ToString() + "' face='" + myReader2["face_value"].ToString() + "'> </td> " +
                    "<td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td> " +
                    "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
            else
                table += "<tr class='NA smokerNS'><td> <input type='radio' name='ins_optionS' id='smokerNS' value='" + myReader2["cost"].ToString() + "' face='" + myReader2["face_value"].ToString() + "' disabled> </td> " +
                    "<td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td> " +
                    "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
            hasSPRates = true;

        }
        myReader2.Close();

        if (hasSmoker)
        {
            //*** ADD SMOKER OPTIONS ***
            SQL = SQL.Insert(SQL.IndexOf(productCode) + productCode.Length, "S");

            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            while (myReader2.Read())
            {
                if (Convert.ToDouble(myReader2["cost"].ToString()) != 0)
                    table += "<tr class='ins_rowS smokerYS'><td> <input type='radio' name='ins_optionS' id='smokerYS' value='" + myReader2["cost"].ToString() + "' face='" + myReader2["face_value"].ToString() + "'> </td> " +
                        "<td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td> " +
                        "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
                else
                    table += "<tr class='NA smokerYS'><td> <input type='radio' name='ins_optionS' id='smokerYS' value='" + myReader2["cost"].ToString() + "' face='" + myReader2["face_value"].ToString() + "' disabled> </td> " +
                        "<td class='face'> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString(), true) + "</td> " +
                        "<td class='cost'> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
            }
            table += "<tr><td style='font-size:10px' colspan='3'>&nbsp;</td></tr> \n";
            myReader2.Close();
        }

        //*** BUILD REST OF TABLE ***
        if ((bool)Session["hasHQ"])
            table += "<tr><td colspan='2'>Is the Proposed Insured a U.S. Citizen or a permanent resident?</td> \n" +
                "<td><span class='reqQuestions'><span class='reqQuestions'><label><input type='radio' name='residentS' value='Yes'> Yes </label> </span>" +
                "<span class='reqQuestions'><label> <input type='radio' name='residentS' value='No'> No </label> </span></td></tr> \n";

        table += "</table> <br/>";
        spouseRatesDifferent = true;
        if (hasSPRates == false)
            table = "<div class='alert alert-danger alert-block'> \n" +
                        "	<h4 class='alert-heading'>Error!</h4> \n" +
                        "	Due to age restrictions, this person's <b>Spouse</b> is ineligible for this product. \n" +
                        "   <input type='hidden' name='ins_optionS' value='NONE' /> \n " +
                        "</div>";
        return table;
    }
    private string createSBRateTable(string productCode)
    {
        // Response.Write("SBRateTable");
        //*** SETUP TABLE ***
        string table = "";
        SqlDataReader myReader2 = null;
        try
        {
            if (String.IsNullOrWhiteSpace((string)Session["salary"]))
                throw new Exception("This employee's <b>salary</b> has not been set.");
            else if (Convert.ToDouble((string)Session["salary"]) == 0)
                throw new Exception("This employee's <b>salary</b> has been set to <b>0</b>.");

            table = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTable'> \n" +
                    "   <tr><td><center><b>Benefit</b></center></td> \n";
            List<string> options = new List<string>();

            string SQL = "SELECT COLUMN_NAME FROM [" + Session["currentAgency"] + "].INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + productCode + "_RT' AND TABLE_SCHEMA = '" + Session["group"] + "' " +
                       "AND COLUMN_NAME NOT IN ('ID', 'salary', 'face_value','er_cost', 'age_low', 'age_high')";
            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            while (myReader2.Read())
            {
                if (myReader2["COLUMN_NAME"].ToString() == "cost")
                    table += "       <td><center><b>Premium</b></center></td> \n";
                else
                    table += "       <td><center><b>" + myReader2["COLUMN_NAME"].ToString() + "</b></center></td> \n";
                options.Add(myReader2["COLUMN_NAME"].ToString());
            }

            table += "</tr> \n";

            SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] " + generateWhereClause(); // Salary is listed per annum in DB

            if (hasERTable)
            {
                SQL = "SELECT t.* ";

                foreach (string option in options)
                {
                    SQL += ",et.[" + option + "] as [" + option + "_er_cost] ";
                }
                SQL += "FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] as t " +
                          "JOIN [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_ER_RT] as et ON t.face_value=et.face_value " + generateWhereClause();
            }
            //Response.Write(SQL);

            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            while (myReader2.Read())
            {
                //*** ADD DISABILITTY OPTIONS ***
                table += "  <tr><td class='" + productName + "Plan'> $" + myReader2["face_value"].ToString() + "</td>";
                foreach (string option in options)
                {
                    string disabledText = "";
                    string cost = "$" + SiteMaster.toCurrency(myReader2[option].ToString());

                    if (Convert.ToDouble(myReader2[option]) == 0 && !(bool)Session["erPaid"])
                    {
                        disabledText = " disabled='disabled' ";
                        cost = " - ";
                    }
                    table += "       <td class='ins_row' " + disabledText + "><input type='radio' name='ins_option' face='" + myReader2["face_value"].ToString() + "' value='" + myReader2[option].ToString() + "'";

                    if (option != "cost")
                        table += " tier='" + option + "'";

                    if ((hasERTable) && myReader2.HasColumn(option + "_er_cost"))
                        table += " er_cost='" + myReader2[option + "_er_cost"].ToString() + "'";
                    else if (((bool)Session["erPaid"]) && myReader2.HasColumn("er_cost"))
                        table += " er_cost='" + myReader2["er_cost"].ToString() + "'";

                    table += disabledText + "/> " + cost + "</td> \n";
                }
                table += "</tr> \n";
            }
            myReader2.Close();
            if (hasERTable)
                Session["erPaid"] = hasERTable;
        }
        catch (Exception e)
        {
            Response.Write("<div class='alert alert-danger alert-block'> \n" +
                                "	<a class='close' data-dismiss='alert' href='../ProductsGrid.aspx'>Go Back</a> \n" +
                                "	<h4 class='alert-heading'>Error!</h4> \n" +
                                "Error while creating " + productName + " rates table: " + e +
                                "</div>");
        }
        table += "</table> <br/><br/> \n ";
        return table;
    }

    private string createCRateTablewHQ(string productCode)
    {
        // *** SETUP TABLE ***
        //Response.Write("cratetablewhq");
        string table = "<asp:Panel id='ratePanel'>";
        table += " <table class='table table-condensed table-bordered' style='width:100%; margin-bottom:-10px' id='rateTable'> " +
        "<tr><td width='40%'><b>Plan</b></td> \n";

        // *** ADD PREMIUMS TO TABLE ***
        try
        {
            int age = 0;
            int ageS = 0;
            if ((hasAgeBand || hasAge))
            {
                if (String.IsNullOrWhiteSpace(effectiveDate))
                    throw new Exception("The effective date for this employee has not been set.");
                if (String.IsNullOrWhiteSpace(birthdate))
                    throw new AgeException("The birthdate for this employee has not been set.");
                age = ageAtEffectiveDate(birthdate, effectiveDate);
                ageS = 0;
                if (hasSpouse)
                {
                    if (String.IsNullOrWhiteSpace(birthdateS))
                        throw new AgeException("The birthdate for this employee's <b>Spouse</b> has not been set.");
                    ageS = ageAtEffectiveDate(birthdateS, effectiveDate);
                }
            }

            //Response.Write(Session["jobTitle"]);
            if (productCode.Contains("LOYAL_CA"))
            {

                Session["jobTitle"] = SiteMaster.getSingleSQLData("SELECT [title] FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[Employees] where [empID]=" + empID);
                if (String.IsNullOrWhiteSpace((string)Session["jobTitle"])) throw new Exception("This employee's <strong>job title</strong> has not been set. ");
            }
            // *** ADD TIER NAMES TO TABLE ***
            List<string> tier = new List<string>();
            List<string> attributeList = new List<string>();

            SqlDataReader myReader2 = null;
            string SQL = "SELECT DISTINCT [tier],[ID] FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] ";
            if (hasJobClass)
                SQL += " WHERE [jobclass] LIKE '|%" + Session["jobClass"] + "%|'";

            SQL += "ORDER BY [ID]";
            //Response.Write(SQL);
            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            for (int i = 0; myReader2.Read(); i++)
            {
                if (!tier.Contains(myReader2["tier"].ToString()))
                {
                    table += "   <td><b>" + SiteMaster.UppercaseFirst(myReader2["tier"].ToString()) + "</b></td> \n";
                    tier.Add(myReader2["tier"].ToString());
                }
            }
            table += "</tr> \n";

            SQL = "SELECT COLUMN_NAME FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + productCode + "_RT' AND TABLE_SCHEMA='" + Session["group"] + "' " +
                "AND COLUMN_NAME NOT IN ('ID', 'coverage_level', 'tier', 'face_value', 'cost','er_cost')";

            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            if (myReader2.HasRows)
            {
                while (myReader2.Read())
                    attributeList.Add(myReader2["COLUMN_NAME"].ToString());
                myReader2.Close();
                Session["hiddenVars"] = attributeList;
            }

            // *** ADD RATES DATA TO TABLE ***
            string currentPlan = "";
            string previousPlan = "";
            bool hasSP = false;
            myReader2 = null;
            SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] ";
            SQL += generateWhereClause() + " ORDER BY [ID]";

            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            if (myReader2.HasRows)
            {
                for (int i = 0; myReader2.Read(); i++)
                {
                    string disabledText = "";
                    if (CensusData.Count > 1 && !showSelectDependentsBox && myReader2["tier"].ToString().Contains("Employee +"))
                        showSelectDependentsBox = true;
                    // Check if any of the plans should be colored differently.
                    string fontColor = "#000000";
                    if (myReader2.HasColumn("color"))
                    {
                        fontColor = myReader2["color"].ToString();
                    }
                    if (i == 0)
                        currentPlan = myReader2["coverage_level"].ToString();
                    // *** CHECK IF PLAN HAS BEEN ADDED ***
                    if (currentPlan != previousPlan)
                    {
                        // *** IF NO, ADD PLAN ENTRY AND FIRST OPTION ***

                        if (tier[i].Contains(" + Spouse") && !hasSpouse)
                            disabledText = " disabled='disabled' ";

                        if ((tier[i] == "Single Parent" || tier[i].Contains(" + Children") || tier[i].Contains(" + Child(ren)")) && !hasChild)
                            disabledText = " disabled='disabled' ";

                        if (tier[i].Contains("Family") && (!hasChild || !hasSpouse))
                            disabledText = " disabled='disabled' ";

                        if (!hasSP)
                            hasSP = tier[i] == "Single Parent";

                        if (hasSP && tier[i].Contains("Family") && hasSpouse)
                            disabledText = "";
                        table += "<tr>" +
                               "   <td class='CancerPlan'><font color='" + fontColor + "'>" + myReader2["coverage_level"].ToString() + " </font></td> \n" +
                                 "   <td class='ins_row'" + disabledText + "><input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "' level='" + myReader2["coverage_level"].ToString() + "' tier='" + tier[i] + "'" + disabledText;

                        if (attributeList.Count > 0)
                            foreach (string attribute in attributeList)
                                table += " " + attribute + "='" + myReader2[attribute].ToString() + "'";

                        /*       if (tier[i].Contains(" + Spouse") && !hasSpouse)
                                   table += " disabled='disabled' ";

                               if ((tier[i] == "Single Parent" || tier[i].Contains(" + Children") || tier[i].Contains(" + Child(ren)")) && !hasChild)
                                   table += " disabled='disabled' ";

                               if (tier[i].Contains("Family") && (!hasChild && !hasSpouse))
                                   table += " disabled='disabled' ";*/

                        if ((bool)Session["erPaid"])
                            table += " er_cost='" + myReader2["er_cost"].ToString() + "'";

                        table += " /> " + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> \n" +
                                "</tr> \n";

                    }
                    else
                    {
                        // *** IF YES, ADD NEXT OPTION ***

                        int pos = table.LastIndexOf("</td>");


                        if (tier[i].Contains(" + Spouse") && !hasSpouse)
                            disabledText = " disabled='disabled' ";

                        if ((tier[i] == "Single Parent" || tier[i].Contains(" + Children") || tier[i].Contains(" + Child(ren)")) && !hasChild)
                            disabledText = " disabled='disabled' ";

                        if (tier[i].Contains("Family") && (!hasChild || !hasSpouse))
                            disabledText = " disabled='disabled' ";

                        if (!hasSP)
                            hasSP = tier[i] == "Single Parent";

                        if (hasSP && tier[i].Contains("Family") && hasSpouse)
                            disabledText = "";

                        string entry = "   <td class='ins_row' " + disabledText + "><input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "' level='" + myReader2["coverage_level"].ToString() + "' tier='" + tier[i] + "'" + disabledText;

                        if (attributeList.Count > 0)
                            foreach (string attribute in attributeList)
                                entry += " " + attribute + "='" + myReader2[attribute].ToString() + "'";

                        /*          if (tier[i].Contains(" + Spouse") && !hasSpouse)
                                      entry += " disabled='disabled' ";

                                  if ((tier[i] == "Single Parent" || tier[i].Contains(" + Children") || tier[i].Contains(" + Child(ren)")) && !hasChild)
                                      entry += " disabled='disabled' ";

                                  if (tier[i].Contains("Family") && (!hasChild && !hasSpouse))
                                      entry += " disabled='disabled' ";*/

                        if ((bool)Session["erPaid"])
                            entry += " er_cost='" + myReader2["er_cost"].ToString() + "'";

                        entry += " /> " + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> \n";
                        //Response.Write(pos + " = " + entry + " <br/> \n");

                        table = table.Insert(pos, entry);
                    }
                    if (i == (tier.Count - 1)) i = -1;
                    //Response.Write(tier.Count);
                    previousPlan = myReader2["coverage_level"].ToString();
                }
            }
            else
                table += "<tr class='ins_row none_row alert-success'><td> <input type='radio' name='ins_optionS' value='NONE' checked> NONE </td> <td colspan='" + tier.Count + "'>  --- </td> </tr> \n";

            table += "</table> \n ";
            table += "</asp:Panel>";
            myReader2.Close();
            return table;
        }
        catch (Exception e)
        {
            return "<div class='alert alert-danger'> \n" +
                                   "	<a class='close' data-dismiss='alert' href='../ProductsGrid.aspx'>Go Back</a> \n" +
                                   "	<h4 class='alert-heading'>Error!</h4> \n" +
                                   "An error has occured while creating the " + productName + " rates table: " + e.ToString() +
                //"<p /> Source: " + e.StackTrace + 
                                   "</div>\n" +
                                   "<script>$(window).load(function() {$('.cancer-table').hide();});</script>";
        }
    }




    private string createMTRateTable(string productCode)
    {
        //Response.Write("in mt rate table");
        //*** SETUP TABLE ***
        //Response.Write("Test");
        string table = "";
        string tableS = "";
        int age = 0;
        int ageS = 0;
        bool allowFamily = false;
        SqlDataReader myReader2 = null;

        try
        {
            table = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTable'> \n";
            if (hasAgeBand || hasAge)
            {
                if (String.IsNullOrWhiteSpace(effectiveDate))
                    throw new Exception("The effective date for this employee has not been set.");

                age = ageAtEffectiveDate(birthdate, effectiveDate);
                if (hasSpouse)
                    ageS = ageAtEffectiveDate(birthdateS, effectiveDate);

                table += "<tr><td colspan='3' style='background: #fff; color:#000'><strong>Age " + age.ToString() + "</strong> on Effective Date <strong>" + String.Format("{0:MM/dd/yyyy}", effectiveDate) +
                    "</strong></td> </tr> \n";
                if ((hasAgeBand &&
                    (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age_high]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]")
                    || age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age_low]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]")))
                    || (hasAge &&
                    (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]") ||
                    age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]"))))
                    throw new AgeException();
            }

            if (hasSmoker)
                table += "<tr><td colspan='3' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smoker' id='smoker' value='No' checked='checked'> No " +
                "   <input type='radio' name='smoker' id='smoker' value='Yes'> Yes </td> </tr> \n";

            if (Session["group"].ToString() == "wireless")
            {
                table = AddToolTip(table);

            }
            else
                table += " <tr><th>&nbsp;</th><th><center>Family Type</th><th><center>" + Session["billing_mode"] + " Premium</center></th></tr> \n";

            if (spouseRatesDifferent && hasSpouse)
            {
                tableS = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTableS'> \n";

                if (hasAgeBand || hasAge)
                {
                    tableS += "<tr><td colspan='3' style='background: #fff; color:#000'><strong>Spouse Coverage - Age " + ageS.ToString() + "</strong> on Effective Date <strong>" + String.Format("{0:MM/dd/yyyy}", effectiveDate) +
                 "</strong></td></tr> \n";
                }
                if (hasSmoker)
                    tableS += "<tr><td colspan='3' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smokerS' id='smokerS' value='No' checked='checked'> No " +
            "<input type='radio' name='smokerS' id='smokerS' value='Yes' > Yes " + "</td> </tr> \n";
                tableS += "       <tr><th>&nbsp;</th><th><center>Family Type</th><th><center>" + Session["billing_mode"] + " Premium</center></th></tr> \n" +
                "<tr class='ins_rowS none_row'><td> <input type='radio' name='ins_optionS' value='NONE'> </td> <td> NONE </td> <td>  --- </td> </tr> \n";

            }

            // *** CREATE MULTI-TIERED TABLE ***
            string SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] ";

            SQL += generateWhereClause() + " ORDER BY [coverage_level],[ID]";

            //Response.Write(SQL);
            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();

            string coverageLevel = "";
            string coverageSection = "";
            string fontColor = "#000000";
            string smokerClass = "";
            while (myReader2.Read())
            {
                string disabledText = "";
                if (hasSmoker)
                    smokerClass = " smokerN";
                // Check to see if this coverage level header should appear in a different font color
                if (myReader2.HasColumn("color"))
                {
                    fontColor = myReader2["color"].ToString();
                }
                //*** ADD COVERAGE LEVEL HEADER ***
                if (String.IsNullOrWhiteSpace(coverageLevel) || coverageLevel != myReader2["coverage_level"].ToString())
                {
                    coverageSection += "  <tr>";
                    if (hasSmoker)
                        coverageSection = coverageSection.Insert(coverageSection.LastIndexOf(">"), " class='" + smokerClass.Trim() + "'");
                    coverageLevel = myReader2["coverage_level"].ToString();
                    if (Session["group"].ToString() == "wireless")
                        coverageSection += AddCoverageLevelPopups(myReader2["coverage_level"].ToString(), fontColor, productCode);
                    else
                        coverageSection += "<td colspan='3' style='background: #fff; font-weight:bold'; color:" + fontColor + "'>" + myReader2["coverage_level"].ToString() + "</td></tr>";
                    if (coverageLevel != myReader2["coverage_level"].ToString())
                        coverageLevel = myReader2["coverage_level"].ToString();
                }

                //*** ADD RATE OPTION ***

                if (hasSmoker)
                    coverageSection = coverageSection.Insert(coverageSection.LastIndexOf("'"), smokerClass);

                if (CensusData.Count > 1 && !showSelectDependentsBox && myReader2["tier"].ToString().Contains("Employee +"))
                    showSelectDependentsBox = true;

                if (myReader2["tier"].ToString().Contains(" + Spouse") && !hasSpouse)
                    disabledText = " disabled='disabled' ";

                if (myReader2["tier"].ToString() == "Single Parent" && !hasChild)
                    disabledText = " disabled='disabled' ";

                if (myReader2["tier"].ToString() == "Employee + Child(ren)" && !hasChild)
                    disabledText = " disabled='disabled' ";

                if (myReader2["tier"].ToString() == "Employee + 1")
                {
                    if (!hasChild && !hasSpouse)
                        disabledText = " disabled='disabled' ";
                    else if (CensusData.Count > 2)
                        allowFamily = true;
                }

                //if (myReader2["tier"].ToString().Contains("Family") && (!hasChild && !hasSpouse))
                if (myReader2["tier"].ToString().Contains("Family") && (!hasChild || !hasSpouse) && !allowFamily)
                    disabledText = " disabled='disabled' ";

                coverageSection += "  <tr class='ins_row'" + disabledText + "><td class='ins_cell' style ='width:10%'>" +
                "       <input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "' tier='" + myReader2["tier"].ToString() + "'" + disabledText;

                if (hasCoverageLevel)
                    coverageSection += " level='" + myReader2["coverage_level"].ToString() + "'";
                if (hasFaceValue)
                    coverageSection += " face='" + myReader2["face_value"].ToString() + "'";

                if ((bool)Session["erPaid"])
                    coverageSection += " er_cost='" + myReader2["er_cost"].ToString() + "'";

                coverageSection += "> </td> \n" +
                "       <td> " + myReader2["tier"].ToString() + "</td> <td> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
            }
            table += coverageSection;
            myReader2.Close();

            if (hasSmoker)
            {
                string disabledText = "";
                //*** ADD SMOKER OPTIONS ***
                SQL = SQL.Insert(SQL.IndexOf(productCode) + productCode.Length, "S");
                //Response.Write(SQL);
                coverageLevel = "";
                coverageSection = "";
                smokerClass = "";

                myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
                while (myReader2.Read())
                {
                    smokerClass = " smokerY";
                    //*** ADD COVERAGE LEVEL HEADER ***
                    if (String.IsNullOrWhiteSpace(coverageLevel) || coverageLevel != myReader2["coverage_level"].ToString())
                    {
                        coverageSection += "  <tr class='" + smokerClass.Trim() + "'>";
                        coverageLevel = myReader2["coverage_level"].ToString();
                        coverageSection += "<td colspan='3' style='background: #fff; font-weight:bold'>" + myReader2["coverage_level"].ToString() + "</td></tr>";
                        if (coverageLevel != myReader2["coverage_level"].ToString())
                            coverageLevel = myReader2["coverage_level"].ToString();
                    }

                    //*** ADD RATE OPTION ***

                    if (myReader2["tier"].ToString().Contains(" + Spouse") && !hasSpouse)
                        disabledText = " disabled='disabled' ";

                    if (myReader2["tier"].ToString() == "Single Parent" && !hasChild)
                        disabledText = " disabled='disabled' ";

                    if (myReader2["tier"].ToString() == "Employee + Child(ren)" && !hasChild)
                        disabledText = " disabled='disabled' ";

                    if (myReader2["tier"].ToString() == "Employee + 1")
                    {
                        if (!hasChild && !hasSpouse)
                            disabledText = " disabled='disabled' ";
                        else if (CensusData.Count > 2)
                            allowFamily = true;
                    }

                    //if (myReader2["tier"].ToString().Contains("Family") && (!hasChild && !hasSpouse))
                    if (myReader2["tier"].ToString().Contains("Family") && (!hasChild || !hasSpouse) && !allowFamily)
                        disabledText = " disabled='disabled' ";

                    coverageSection += "  <tr class='ins_row'" + disabledText + "><td class='ins_cell' style ='width:10%'>" +
                    "       <input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "' tier='" + myReader2["tier"].ToString() + "'" + disabledText;


                    if (hasCoverageLevel)
                        coverageSection += " level='" + myReader2["coverage_level"].ToString() + "'";
                    if (hasFaceValue)
                        coverageSection += " level='" + myReader2["face_value"].ToString() + "'";
                    if ((bool)Session["erPaid"])
                        coverageSection += " er_cost='" + myReader2["er_cost"].ToString() + "'";

                    coverageSection += "> </td> \n" +
                    "       <td> " + myReader2["tier"].ToString() + "</td> <td> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";

                    coverageLevel = myReader2["coverage_level"].ToString();
                }
                table += coverageSection;
            }

            //*** BUILD REST OF TABLE ***
            if ((bool)Session["hasHQ"] && productCode != "DELTA_DEN")
                table += "<tr><td colspan='2'>Is the employee actively at work performing the regular duties of the job in the usual manner and the usual place of employment?</td> \n" +
                    "<td> <input type='radio' name='active' value='No'> No " +
                    "<input type='radio' name='active' value='Yes'> Yes " + "</td></tr>" +
                            "<tr><td colspan='7'>&nbsp;</td></tr></table> \n ";
            else
                table += "</table> \n ";

            return table;
        }
        catch (AgeException e)
        {
            return "<div class='alert alert-danger alert-block'> \n" +
                                "	<a class='close' data-dismiss='alert' href='../ProductsGrid.aspx'>Go Back</a> \n" +
                                "	<h4 class='alert-heading'>Error!</h4> \n" +
                                "	Due to age restrictions, this person is ineligible for this product. \n" +
                                "</div>";
        }
        catch (Exception e)
        {
            return "<div class='alert alert-danger alert-block'> \n" +
                                "	<a class='close' data-dismiss='alert' href='../ProductsGrid.aspx'>Go Back</a> \n" +
                                "	<h4 class='alert-heading'>Error!</h4> \n" +
                                "An has error occured while creating the " + productName + " rates table: " + e.ToString() +
                                "<p /> Source: " + e.StackTrace +
                                "</div>";
        }
    }

    private string AddToolTip(string table)
    {
        string deductionVerbiage = "Premiums will come out of 24 of your 26 bi-weekly paychecks. The two months that have three paychecks will only receive deductions from the first two checks.";
        table += "<tr><th>&nbsp;</th><th><center>Family Type</th><th><a href='#' data-toggle='tooltip' data-placement='bottom' title='" + deductionVerbiage + "'><center>" + Session["billing_mode"] + " Premium</center></a></th></tr> \n";
        return table;
    }

    private string AddCoverageLevelPopups(string coverageLevel, string fontColor, string productCode)
    {
        string hoverText = "";
        if (productCode == "BCBS__MED" || productCode == "DELTA_DEN")
        {
            hoverText = "<span class='open-product-dialog btn-link' >[Comparison Chart] </span>";
            return "<td colspan='3' style='background: #fff; font-weight:bold'; color:" + fontColor + "'>" + coverageLevel + " " + hoverText + "</td></tr>";
        }
        else
            return "<td colspan='3' style='background: #fff; font-weight:bold'; color:" + fontColor + "'>" + coverageLevel + "</td></tr>";
    }

    private string createSTRateTable(string productCode)
    {
        //*** SETUP TABLE ***
        //Response.Write("strate");
        string table = "";
        string tableS = "";
        int age = 0;
        int ageS = 0;
        bool allowFamily = false;
        SqlDataReader myReader2 = null;

        try
        {
            table = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTable'> \n";
            if (hasAgeBand || hasAge)
            {
                if (String.IsNullOrWhiteSpace(effectiveDate))
                    throw new Exception("The effective date for this employee has not been set.");

                age = ageAtEffectiveDate(birthdate, effectiveDate);
                if (hasSpouse)
                    ageS = ageAtEffectiveDate(birthdateS, effectiveDate);


                table += "<tr><td colspan='3' style='background: #fff; color:#000'><strong>Age " + age.ToString() + "</strong> on Effective Date <strong>" + String.Format("{0:MM/dd/yyyy}", effectiveDate) +
                    "</strong></td> </tr> \n";
                if ((hasAgeBand &&
                    (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age_high]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]")
                    || age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age_low]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]")))
                    || (hasAge &&
                    (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]") ||
                    age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]"))))
                    throw new AgeException();
            }

            if (hasSmoker)
                table += "<tr><td colspan='3' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smoker' id='smoker' value='No' checked='checked'> No " +
                "   <input type='radio' name='smoker' id='smoker' value='Yes'> Yes </td> </tr> \n";
            table += "       <tr><th>&nbsp;</th><th><center>Family Type</th><th><center>" + Session["billing_mode"] + " Premium</center></th></tr> \n";

            if (spouseRatesDifferent && hasSpouse)
            {
                tableS = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTableS'> \n";

                if (hasAgeBand || hasAge)
                {
                    tableS += "<tr><td colspan='3' style='background: #fff; color:#000'><strong>Spouse Coverage - Age " + ageS.ToString() + "</strong> on Effective Date <strong>" + String.Format("{0:MM/dd/yyyy}", effectiveDate) +
                 "</strong></td></tr> \n";
                }
                if (hasSmoker)
                    tableS += "<tr><td colspan='3' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smokerS' id='smokerS' value='No' checked='checked'> No " +
            "<input type='radio' name='smokerS' id='smokerS' value='Yes' > Yes " + "</td> </tr> \n";
                tableS += "       <tr><th>&nbsp;</th><th><center>Family Type</th><th><center>" + Session["billing_mode"] + " Premium</center></th></tr> \n" +
                "<tr class='ins_rowS none_row'><td> <input type='radio' name='ins_optionS' value='NONE'> </td> <td> NONE </td> <td>  --- </td> </tr> \n";

            }
            List<string> tiers = new List<string>();


            // *** ADD SIMPLE RATES TABLE ***
            string SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] ";

            SQL += generateWhereClause() + " ORDER BY [ID]";

            //Response.Write(hasGender + " |" + SQL);
            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            if (myReader2.HasRows)
            {
                table = "   <table class='table table-condensed table-bordered' style='width:100%' id='rateTable'> \n";

                if (Session["group"].ToString() == "wireless")
                {
                    table = AddToolTip(table);

                }
                else
                    table += "       <tr><th>&nbsp;</th><th><center>Family Type</th><th><center>" + Session["billing_mode"] + " Premium</center></th></tr> \n";
                while (myReader2.Read())
                {
                    //*** ADD RATE OPTION ***
                    string disabledText = "";


                    if (CensusData.Count > 1 && !showSelectDependentsBox && myReader2["tier"].ToString().Contains("Employee +"))
                        showSelectDependentsBox = true;

                    if (myReader2["tier"].ToString().Contains(" + Spouse") && !hasSpouse)
                        disabledText = " disabled='disabled' ";

                    if (myReader2["tier"].ToString() == "Single Parent" && !hasChild)
                        disabledText = " disabled='disabled' ";

                    if (myReader2["tier"].ToString() == "Employee + Child(ren)" && !hasChild)
                        disabledText = " disabled='disabled' ";

                    if (myReader2["tier"].ToString() == "Employee + 1")
                    {
                        if (!hasChild && !hasSpouse)
                            disabledText = " disabled='disabled' ";
                        else if (CensusData.Count > 2)
                            allowFamily = true;
                    }

                    //Response.Write(myReader2["tier"] + "|" + allowFamily +"child:"+hasChild+"spouse:"+hasSpouse+ "prodcode:"+productCode+"<Br/>");
                        
                    //if (myReader2["tier"].ToString().Contains("Family") && (!hasChild && !hasSpouse)). true && (false||true) && true)
                    //NEXT TWO LINES AND ELSE IS SPECIFIC code for UNION CO. REMOVE WHEN NEXT UPDATE FROM BALA
                    if (productCode == "INFOA_PRI" && Session["group"].ToString() == "union_co")
                    {
                    }
                    else if ((myReader2["tier"].ToString().Contains("Family") && (!hasChild || !hasSpouse) && !allowFamily))
                        disabledText = " disabled='disabled' ";


                    table += "  <tr class='ins_row'" + disabledText + "><td class='ins_cell' style ='width:10%'>" +
                     "       <input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "' tier='" + myReader2["tier"].ToString() + "'" + disabledText;


                    if ((bool)Session["erPaid"])
                        table += " er_cost='" + myReader2["er_cost"].ToString() + "'";

                    table += "> </td> \n" +
                    "       <td> " + myReader2["tier"].ToString() + "</td> <td> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
                }
                myReader2.Close();
            }
            //table += "</table> <br/><br/> \n ";
        }
        catch (AgeException e)
        {
            return "<div class='alert alert-danger alert-block'> \n" +
                                "	<a class='close' data-dismiss='alert' href='../Default.aspx'>Go Back</a> \n" +
                                "	<h4 class='alert-heading'>Error!</h4> \n" +
                                "	Due to age restrictions, this person is ineligible for this product. \n" +
                                "</div>";
        }
        catch (Exception e)
        {
            Response.Write("An has error occured while creating the " + productName + " rates table: " + e +
                "<p /> Source: " + e.Source);
        }

        //*** BUILD REST OF TABLE ***
        if ((bool)Session["hasHQ"] && !hasActiveHQ)
            table += "<tr><td colspan='2'>Is the employee actively at work performing the regular duties of the job in the usual manner and the usual place of employment?</td> \n" +
                "<td> <input type='radio' name='active' value='No'> No " +
                "<input type='radio' name='active' value='Yes'> Yes " + "</td></tr>" +
                /* "<tr><td colspan='7'>&nbsp;</td></tr>*/"</table> \n ";
        else
            table += "</table> \n ";
        //Response.Write(table);
        return table;
    }

    private string createFactoredRateTable(string productCode)
    {
        //*** SETUP TABLE ***
        //Response.Write(Session["erPaid"]);
        string table = "";
        SqlDataReader myReader2 = null;

        try
        {
            if (String.IsNullOrWhiteSpace(effectiveDate))
                throw new Exception("The effective date for this employee has not been set.");
            if (String.IsNullOrWhiteSpace(birthdate))
                throw new Exception("The birthdate for this employee has not been set.");
            int age = ageAtEffectiveDate(birthdate, effectiveDate);
            int ageS = 0;
            if (hasSpouse)
                ageS = ageAtEffectiveDate(birthdateS, effectiveDate);

            if ((hasAgeBand &&
                (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age_high]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_FT]") ||
                age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age_low]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_FT]"))) ||
                (hasAge &&
                (age > (int)SiteMaster.getSingleSQLData("SELECT MAX([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_FT]") ||
                age < (int)SiteMaster.getSingleSQLData("SELECT MIN([age]) FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_FT]"))))
                throw new AgeException();

            // *** ADD SIMPLE RATES TABLE ***
            string SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_FT] ";

            //if (spouseRatesDifferent && hasSpouse)
            //{
            //    if (hasAgeBand) SQL += " WHERE (([age_low]<=" + age.ToString() + " AND [age_high]>=" + age.ToString() + " AND [ee_or_spouse]='ee') " +
            //        "OR ([age_low]<=" + ageS.ToString() + " AND [age_high]>=" + ageS.ToString() + " AND [ee_or_spouse]='spouse'))";
            //    else SQL += " WHERE [age]=" + age.ToString() + " OR [age]=" + ageS.ToString();
            //}
            //else if (hasAgeBand)
            //{
            //    if (hasAgeBand) SQL += " WHERE [age_low]<=" + age.ToString() + " AND [age_high]>=" + age.ToString();
            //    else SQL += " WHERE [age]=" + age.ToString();
            //}
            SQL += generateWhereClause();
            if (hasFaceValue)
                SQL += " ORDER BY [face_value]";

            //Response.Write(SQL);
            myReader2 = new SqlCommand(SQL, myConnection).ExecuteReader();
            if (myReader2.HasRows)
            {
                table = " <asp:Panel>";

                table += "   <table class='table table-condensed table-bordered' style='width:100%' id='rateTable'> \n" +
                        "       <tr><th>&nbsp;</th>";

                if (myReader2.HasColumn("tier"))
                    table += "<th><center>Plan</center></th>";

                table += "<th><center>Benefit Amount</center></th><th><center>" + Session["billing_mode"] + " Premium</center></th></tr> \n";
                while (myReader2.Read())
                {
                    double factor = Convert.ToDouble(myReader2["factor"].ToString());
                    double pctCovered = Convert.ToDouble(myReader2["pct_covered"].ToString());
                    int period = Convert.ToInt32(myReader2["period"].ToString());
                    bool flatRate = false;
                    int divisor = 100;
                    int maxBenefit = -1;
                    int benefitPlus = 0; // This is a static value added to the benefit, e.g. 2x salary + $5000
                    int roundBenefitDownTo = 0;  // Round the benefit down to the nearest increment of this value.
                    int roundBenefitUpTo = 0; // Round the benefit up to the nearest increment of this value.

                    int roundSalaryTo = 0;   // Round the salary up to the nearest increment of this value.
                    int calculateByFaceValue = 0;  // If this is 1, use the actual face value (reduced weekly/monthly earnings) to calculate the rate. 
                    //Otherwise use the weekly/monthly earnings.
                    double effectiveSalary = Convert.ToDouble(Session["salary"]);  // If no rounding, then this is just the salary.
                    double maximumSalary = 0;
                    double er_factor = 0;
                    double er_cost = 0;
                    double cost;

                    if (myReader2.HasColumn("max_benefit"))
                        maxBenefit = Convert.ToInt32(myReader2["max_benefit"].ToString());
                    if (myReader2.HasColumn("benefit_plus"))
                        benefitPlus = Convert.ToInt32(myReader2["benefit_plus"].ToString());

                    // Round the benefit down to this increment
                    if (myReader2.HasColumn("round_benefit"))
                        roundBenefitDownTo = Convert.ToInt32(myReader2["round_benefit"].ToString());
                    // Round the benefit up to this increment
                    if (myReader2.HasColumn("round_benefit_up"))
                        roundBenefitUpTo = Convert.ToInt32(myReader2["round_benefit_up"].ToString());


                    if (myReader2.HasColumn("round_salary"))
                        roundSalaryTo = Convert.ToInt32(myReader2["round_salary"].ToString());

                    if (myReader2.HasColumn("er_factor"))
                        er_factor = Convert.ToDouble(myReader2["er_factor"].ToString());

                    if (myReader2.HasColumn("flat_rate"))
                        flatRate = Convert.ToBoolean(myReader2["flat_rate"].ToString());
                    if (myReader2.HasColumn("max_salary"))
                        maximumSalary = Convert.ToDouble(myReader2["max_salary"].ToString());

                    // Check to see if the rate should be calculated from the face value.
                    if (myReader2.HasColumn("calc_by_face"))
                        calculateByFaceValue = Convert.ToInt32(myReader2["calc_by_face"].ToString());
                    // If the employee's salary is over the max considered for the benefit, just set it to the max salary.
                    if (maximumSalary > 0 && effectiveSalary > maximumSalary)
                        effectiveSalary = maximumSalary;
                    // Check to see if the salary used to calculate the benefit should be rounded up.
                    if (roundSalaryTo > 0 && effectiveSalary % roundSalaryTo > 0)
                        effectiveSalary = effectiveSalary - (effectiveSalary % roundSalaryTo) + roundSalaryTo;

                    double faceValue = (effectiveSalary * pctCovered) / period;

                    // If LTD (monthly), the formula should divide by 100.
                    // If STD (weekly), the formula should divide by 10.
                    // If Basic Life based on annual salary, the formula should divide by 1 (identity).

                    if (period == 12)
                        divisor = 100;
                    else if (period == 52)
                        divisor = 10;
                    else if (period == 1)
                        divisor = 1;


                    // If there is some amount that should be added to the benefit, add it.
                    if (benefitPlus > 0)
                        faceValue = faceValue + benefitPlus;
                    // Check to see if the benefit should be rounded down to some nearby increment.
                    if (roundBenefitDownTo > 0)
                        faceValue = faceValue - (faceValue % roundBenefitDownTo);

                            // Or see if it should be rounded down to some increment
                    else if (roundBenefitUpTo > 0 && faceValue % roundBenefitUpTo > 0)
                        faceValue = faceValue + (roundBenefitUpTo - (faceValue % roundBenefitUpTo));



                    if (faceValue > maxBenefit && maxBenefit > 0)
                    {    // faceValue cannot exceed maxBenefit.
                        faceValue = maxBenefit;
                        // Use the real face value to calculate the rate.
                        if (calculateByFaceValue > 0)
                        {
                            cost = (maxBenefit * factor) / divisor;
                        }
                        // Or use the corresponding weekly/monthly salary to calculate the rate.
                        else
                        {
                            cost = (maxBenefit / pctCovered) * factor / divisor;
                        }
                    } // Otherwise calculate the cost based on the actual salary.
                    else
                    { // Use the real face value to calculate the rate.
                        if (calculateByFaceValue > 0)
                            cost = (faceValue * factor) / divisor;
                        else // Or use the corresponding weekly/monthly salary to calculate the rate.
                            cost = (effectiveSalary / period) * factor / divisor;
                    }

                    // Calculate the ER contribution using the er_factor.
                    if (er_factor > 0)
                    {
                        // Use the real face value to calculate the rate.
                        if (calculateByFaceValue > 0)
                            er_cost = (faceValue * er_factor) / divisor;
                        else // Or use the corresponding weekly/monthly salary to calculate the rate.
                            er_cost = (effectiveSalary / period) * er_factor / divisor;

                    }

                    // If this group uses a flat rate scheme, simply set the costs to whatever rate is offered.
                    if (flatRate == true)
                    {
                        cost = factor;
                        if (er_factor > 0)
                            er_cost = er_factor;
                    }

                    if ((string)Session["IC"] == "Guardian")
                    {
                        double roundedCost = Math.Round(cost / 0.02, 0) * 0.02;

                        if (roundedCost < cost)
                            roundedCost += 0.02;

                        cost = roundedCost;
                        //Response.Write(cost);
                    }


                    //*** ADD DISABILITTY OPTION ***
                    table += "  <tr class='ins_row'>";
                    table += "       <td class='" + productName + "Plan'><input type='radio' name='ins_option' face='" + SiteMaster.toCurrency(faceValue.ToString()) +
                        "' value='" + SiteMaster.toCurrency(cost.ToString()) + "' ";

                    if (myReader2.HasColumn("tier"))
                        table += " tier='" + myReader2["tier"].ToString() + "'";

                    // If there is a static er_cost, add that.
                    if (myReader2.HasColumn("er_cost"))
                        table += " er_cost='" + myReader2["er_cost"].ToString() + "'";

                    // Or if there is a calculated er_cost, add that.
                    else if (er_factor > 0)
                    {
                        table += " er_cost='" + er_cost.ToString() + "'";
                    }


                    table += "/> </td> \n";

                    if (myReader2.HasColumn("tier"))
                        table += "<td> " + myReader2["tier"].ToString() + " </td> ";

                    table += "<td> $" + SiteMaster.toCurrency(faceValue.ToString()) + "</td> \n" +
                        "<td> $" + SiteMaster.toCurrency(cost.ToString()) + "</td> </tr> \n";
                }
                myReader2.Close();
                table += "</table> \n";
                table += "</asp:Panel>";
            }
            else
                table = "<div class='alert alert-danger'> \n" +
                            "   <button class='close' onclick='window.history.back()' data-dismiss='alert'>Go Back</button> \n" +
                            "   <strong>Error!</strong> This person is not eligible for this product. \n" +
                            "</div>";

            if ((bool)Session["hasHQ"])
            {
                SqlDataReader sql_TableInfo = new SqlCommand("SELECT [COLUMN_NAME] FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + productCode +
            "_RT' AND TABLE_SCHEMA = '" + Session["group"] + "'", myConnection).ExecuteReader();

                if (sql_TableInfo.HasRows)
                {
                    while (sql_TableInfo.Read())
                    {
                        if (!hasCoverageLevel)
                            hasCoverageLevel = sql_TableInfo["COLUMN_NAME"].ToString() == "coverage_level";

                        if (!hasFaceValue)
                            hasFaceValue = sql_TableInfo["COLUMN_NAME"].ToString() == "face_value";

                        if (!hasSalaryBand)
                            hasSalaryBand = sql_TableInfo["COLUMN_NAME"].ToString() == "salary";

                        if (!hasAgeBand)
                            hasAgeBand = (sql_TableInfo["COLUMN_NAME"].ToString() == "age_low" || sql_TableInfo["COLUMN_NAME"].ToString() == "age_high");

                        if (!hasAge)
                            hasAge = sql_TableInfo["COLUMN_NAME"].ToString() == "age";

                        if (!hasGender)
                            hasGender = sql_TableInfo["COLUMN_NAME"].ToString() == "gender";

                        if (!hasJobClass)
                            hasJobClass = sql_TableInfo["COLUMN_NAME"].ToString() == "jobclass";

                        if (!spouseRatesDifferent)
                            spouseRatesDifferent = sql_TableInfo["COLUMN_NAME"].ToString() == "ee_or_spouse";

                        if (!(bool)Session["erPaid"])
                            Session["erPaid"] = sql_TableInfo["COLUMN_NAME"].ToString() == "er_cost";

                        if (!(bool)Session["erPaid"])
                            Session["erPaid"] = sql_TableInfo["COLUMN_NAME"].ToString() == "er_factor";
                    }
                    sql_TableInfo.Close();
                    table += createRateTableWithHQ(productCode);
                }
            }



        }

        catch (Exception e)
        {
            Response.Write("An error occured while generating the rate table for " + productName + ": <br/>");
            Response.Write(e.ToString() + e.StackTrace);
        }
        return table;
    }

    protected string addCurrentDeductions()
    {
        string table = "    <table class='table table-condensed table-bordered'> " +
                        "        <tr> " +
                        "            <td colspan='4' bgcolor='#ffffff'>Current " + productName + " Coverage</td> " +
                        "        </tr> ";
        string tableHead = "";
        SqlDataReader CDT = new SqlCommand("SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[CurrentDeductions] WHERE code='" + productCode + "' AND employee_ID=" + Request["employeeID"], myConnection).ExecuteReader();
        if (CDT.HasRows)
            while (CDT.Read())
            {
                if (String.IsNullOrWhiteSpace(tableHead))
                {
                    tableHead = "        <tr> \n";
                    if (!String.IsNullOrWhiteSpace(CDT["coverage_level"].ToString()))
                        tableHead += "            <td><b>Plan</b></td> ";
                    if (!String.IsNullOrWhiteSpace(CDT["tier"].ToString()))
                        tableHead += "            <td><b>Tier</b></td> ";
                    if (!String.IsNullOrWhiteSpace(CDT["face_value"].ToString()))
                        tableHead += "            <td><b>Benefit</b></td> ";
                    tableHead += "            <td><b>Premium</b></td> " +
                                "        </tr> ";

                    table += tableHead;
                }
                table += "        <tr> \n";
                if (!String.IsNullOrWhiteSpace(CDT["coverage_level"].ToString()))
                    table += "            <td>" + CDT["coverage_level"].ToString() + "</td> ";
                if (!String.IsNullOrWhiteSpace(CDT["tier"].ToString()))
                    table += "            <td>" + CDT["tier"].ToString() + "</td> ";
                if (!String.IsNullOrWhiteSpace(CDT["face_value"].ToString()))
                    tableHead += "            <td>" + CDT["face_value"].ToString() + "</td> ";
                table += "            <td>$ " + SiteMaster.toCurrency(CDT["amount"].ToString()) + "</td> " +
                            "        </tr> ";
            }
        else
            table += "        <tr> " +
                    "            <td colspan='4'>No " + productName + " Coverage</td> " +
                    "        </tr> ";

        table += "    </table>";

        return table;
    }

    protected string addChildRider(string rateTable)
    {
        string childRiderString = "";

        int type = Convert.ToInt16(SiteMaster.getSingleSQLData("select case " +
                                        "when exists (SELECT 1 FROM [" + Session["currentAgency"] + "].INFORMATION_SCHEMA.COLUMNS " +
                                            "WHERE TABLE_SCHEMA = '" + Session["group"] + "' AND TABLE_NAME = '" + productCode + "_CHRT' ) " +
                                            "then 1  " +
                                        "when exists (SELECT 1 FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[Riders] " +
                                            " WHERE code='" + productCode + "' ) " +
                                            "then 2  " +
                                        "else 0  " +
                                        "end"));
        if (type == 1)
        {
            int age = ageAtEffectiveDate(birthdate, effectiveDate);
            SqlDataReader ridersList = new SqlCommand("SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_CHRT] " +
                " WHERE [age_low]<=" + age.ToString() + " AND [age_high]>=" + age.ToString(), myConnection).ExecuteReader();
            if (ridersList.HasRows)
            {
                string chrLetters = "CHILD RIDER";
                if (productCode == "LINCO_CRI")
                    chrLetters = "Child Policy.";
                childRiderString = "<tr><td colspan='2' style='background-color:#fff;'>Choose " + SiteMaster.UppercaseFirst(chrLetters) + ":</td></tr>\n <tr> " +
                                          "<tr class='chr_row none_row'><td colspan='2'><label><input id='CHILD-RIDER_0' name='CHILD-RIDER' value='NONE' type='radio' validate='false' /> I Decline the " + chrLetters + ".</label></td> \n	</tr>";
                while (ridersList.Read())
                {
                    childRiderString += "		<td colspan='2'><label><input id='CHILD-RIDER_" + ridersList["ID"] + "' name='CHILD-RIDER' value='" + ridersList["cost"].ToString() + "' type='radio' validate='false' /> Purchase the optional $" +
                                       SiteMaster.toCurrency(ridersList["face_value"].ToString(), true) + " " + chrLetters + " for $" +
                                       SiteMaster.toCurrency(ridersList["cost"].ToString()) + ".</label></td> \n" +
                                       "	</tr>";
                }
                ridersList.Close();
            }
        }
        else if (type == 2)
        {
            SqlDataReader ridersList = new SqlCommand("SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[Riders] WHERE code='" + productCode + "'", myConnection).ExecuteReader();
            if (ridersList.HasRows)
            {
                string chrLetters = "CHILD RIDER";
                if (productCode == "LINCO_CRI")
                    chrLetters = "Child Policy.";
                childRiderString = "<tr><td colspan='2' style='background-color:#fff;'>Choose " + SiteMaster.UppercaseFirst(chrLetters) + ":</td></tr>\n <tr> " +
                                          "<tr class='chr_row none_row'><td colspan='2'><label><input id='CHILD-RIDER_0' name='CHILD-RIDER' value='NONE' type='radio' validate='false' /> I Decline the " + chrLetters + ".</label></td> \n	</tr>";
                while (ridersList.Read())
                {
                    if (ridersList.HasColumn("text") && !String.IsNullOrWhiteSpace(ridersList["text"].ToString()))
                        chrLetters = ridersList["text"].ToString();

                    childRiderString += "		<td colspan='2'><label><input id='CHILD-RIDER_" + ridersList["ID"] + "' name='CHILD-RIDER' value='" + ridersList["cost"].ToString() + "' type='radio' validate='false' /> Purchase the optional $" +
                                       SiteMaster.toCurrency(ridersList["face_value"].ToString(), true) + " " + chrLetters + " for $" +
                                       SiteMaster.toCurrency(ridersList["cost"].ToString()) + ".</label></td> \n" +
                                       "	</tr>";
                    //if (productCode == "COMBI_LIF")
                    //{
                    //    childRiderString += "<tr style='display:none'><td colspan='2'> \n";

                    //    if (ridersList["type"].ToString() == "Accidental" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='Accidental' value='Yes' /> \n";
                    //    if (ridersList["type"].ToString() == "Acceleration" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='Acceleration' value='Yes' /> \n";
                    //    if (ridersList["type"].ToString() == "Extension" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='Extension' value='Yes' /> \n";
                    //    if (ridersList["type"].ToString() == "Waiver" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='Waiver' value='Yes' /> \n";
                    //    if (ridersList["type"].ToString() == "WaiverOfPremium" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='WaiverOfPremium' value='Yes' /> \n";

                    //    childRiderString += "</td> </tr> ";
                    //}
                }
                ridersList.Close();
            }
        }

        if (!String.IsNullOrWhiteSpace(childRiderString))
        {
            if (rateTable.Contains("<CHILD_RIDER_ROW />"))
                return rateTable.Replace("<CHILD_RIDER_ROW />", childRiderString);
            else if (rateTable.Contains("<CHILD_RIDER />"))
                return rateTable.Replace("<CHILD_RIDER />", "<table class='table table-condensed table-bordered'>" + childRiderString + "\n</table>");
            else
                return rateTable + "<table class='table table-condensed table-bordered'>" + childRiderString + "\n</table>";
        }

        return rateTable;
    }


    protected string addChildRider(string rateTable, int type)
    {
        string childRiderString = "";

        if (type == 1)
        {
            int age = ageAtEffectiveDate(birthdate, effectiveDate);
            string SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_CHRT] ";

            string whereClause = "";

            if (Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE " +
                                            "TABLE_SCHEMA = '" + Session["group"] + "' AND TABLE_NAME = '" + productCode + "_CHRT' AND (COLUMN_NAME = 'age_low' OR COLUMN_NAME = 'age_high')) \n   then 1 \n   else 0 \n end")))
                whereClause += " [age_low]<=" + age.ToString() + " AND [age_high]>=" + age.ToString();

            if (Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM " + Session["currentAgency"] + ".INFORMATION_SCHEMA.COLUMNS WHERE " +
                                            " TABLE_SCHEMA = '" + Session["group"] + "' AND TABLE_NAME = '" + productCode + "_CHRT' AND COLUMN_NAME = 'state') \n   then 1 \n   else 0 \n end")))
            {
                if (!String.IsNullOrWhiteSpace(whereClause))
                    whereClause += " AND ";

                if (Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM [" + Session["currentAgency"] + "].[" +
                                            Session["group"] + "].[" + productCode + "_CHRT] WHERE [state] LIKE '|%" + Session["state"] + "%|') then 1 \n   else 0 \n end")))
                    whereClause += "[state] LIKE '|%" + Session["state"] + "%|' ";
                else
                    whereClause += "[state] LIKE '%|ALL_STATES|%' ";
            }

            if (!String.IsNullOrWhiteSpace(whereClause))
                SQL += " WHERE " + whereClause;

            //Response.Write(SQL);               
            SqlDataReader ridersList = new SqlCommand(SQL, myConnection).ExecuteReader();
            if (ridersList.HasRows)
            {
                string chrLetters = "CHILD RIDER";
                if (productCode == "LINCO_CRI")
                    chrLetters = "Child Policy";
                childRiderString = "<tr><td colspan='2' style='background-color:#fff;'>Choose " + SiteMaster.UppercaseFirst(chrLetters) + ":</td></tr>\n <tr> " +
                                          "<tr class='chr_row none_row'><td colspan='2'><label><input id='CHILD-RIDER_0' name='CHILD-RIDER' value='NONE' type='radio' validate='false' /> I Decline the " + chrLetters + ".</label></td> \n	</tr>";
                while (ridersList.Read())
                {
                    childRiderString += "		<td colspan='2'><label><input id='CHILD-RIDER_" + ridersList["ID"] + "' name='CHILD-RIDER' value='" + ridersList["cost"].ToString() + "' type='radio' validate='false' /> Purchase the optional $" +
                                       SiteMaster.toCurrency(ridersList["face_value"].ToString(), true) + " " + chrLetters + " for $" +
                                       SiteMaster.toCurrency(ridersList["cost"].ToString()) + ".</label></td> \n" +
                                       "	</tr>";
                }
                ridersList.Close();
            }
        }
        else if (type == 2)
        {
            SqlDataReader ridersList = new SqlCommand("SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[Riders] WHERE code='" + productCode + "'", myConnection).ExecuteReader();
            if (ridersList.HasRows)
            {
                string chrLetters = "CHILD RIDER";
                if (productCode == "LINCO_CRI")
                    chrLetters = "Child Policy.";
                childRiderString = "<tr><td colspan='2' style='background-color:#fff;'>Choose " + SiteMaster.UppercaseFirst(chrLetters) + ":</td></tr>\n <tr> " +
                                          "<tr class='chr_row none_row'><td colspan='2'><label><input id='CHILD-RIDER_0' name='CHILD-RIDER' value='NONE' type='radio' validate='false' /> I Decline the " + chrLetters + ".</label></td> \n	</tr>";
                while (ridersList.Read())
                {
                    if (ridersList.HasColumn("text") && !String.IsNullOrWhiteSpace(ridersList["text"].ToString()))
                        chrLetters = ridersList["text"].ToString();
                    childRiderString += "		<td colspan='2'><label><input id='CHILD-RIDER_" + ridersList["ID"] + "' name='CHILD-RIDER' value='" + ridersList["cost"].ToString() + "' type='radio' validate='false' /> Purchase the optional $" +
                                       SiteMaster.toCurrency(ridersList["face_value"].ToString(), true) + " " + chrLetters + " for $" +
                                       SiteMaster.toCurrency(ridersList["cost"].ToString()) + ".</label></td> \n" +
                                       "	</tr>";
                    //if (productCode == "COMBI_LIF")
                    //{
                    //    childRiderString += "<tr style='display:none'><td colspan='2'> \n";

                    //    if (ridersList["type"].ToString() == "Accidental" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='Accidental' value='Yes' /> \n";
                    //    if (ridersList["type"].ToString() == "Acceleration" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='Acceleration' value='Yes' /> \n";
                    //    if (ridersList["type"].ToString() == "Extension" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='Extension' value='Yes' /> \n";
                    //    if (ridersList["type"].ToString() == "Waiver" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='Waiver' value='Yes' /> \n";
                    //    if (ridersList["type"].ToString() == "WaiverOfPremium" && ridersList["cost"].ToString() == "1")
                    //        childRiderString += "<input type='hidden' name='WaiverOfPremium' value='Yes' /> \n";

                    //    childRiderString += "</td> </tr> ";
                    //}
                }
                ridersList.Close();
            }
        }

        if (!String.IsNullOrWhiteSpace(childRiderString))
        {
            if (rateTable.Contains("<CHILD_RIDER_ROW />"))
                return rateTable.Replace("<CHILD_RIDER_ROW />", childRiderString);
            else if (rateTable.Contains("<CHILD_RIDER />"))
                return rateTable.Replace("<CHILD_RIDER />", "<table class='table table-condensed table-bordered'>" + childRiderString + "\n</table>");
            else
                return rateTable + "<table class='table table-condensed table-bordered'>" + childRiderString + "\n</table>";
        }

        return rateTable;
    }





    protected string addDependentsBox()
    {
        string depTable = "   <table id='dependents_box' class='table table-condensed table-bordered' id='dependentsTable'>" +
                          "       <tr style='background: #fff'><td colspan='2'> SELECT DEPENDENT </td> </tr> \n";
        //        "       <tr><th>&nbsp;</th><th><center>Name</th> <th><center>Percent</center></th> <th><center>Type</center></th> </tr> \n";
        depTable += "  <tr class='alert alert-success emp_only_row dep_row' style='font-size: 12px; display: none'><td> " +
                "       <input type='radio' class='dep_option' name='dep_option' value='N/A' checked='checked'> </td> \n" +
                "       <td> N/A </td> </tr> \n";

        try
        {
            //HttpContext.Current.Response.Write(CensusData.Count);
            // *** IF YES, ADD DEPENDENTS BOX ***
            if (CensusData.Count == 1)
            {
                depTable += "  <tr class='alert alert-danger emp_1_row' style='font-size: 12px'><td><span class='icon-ban-circle'></span> " +
                        "       No Dependents listed.</td> \n" +
                        "       <td>  </td> </tr> \n";

                depTable += "  <tr class='alert alert-danger sp_row' style='font-size: 12px'><td><span class='icon-ban-circle'></span> " +
                        "       No Dependents listed.</td> \n" +
                        "       <td>  </td> </tr> \n";

                depTable += "  <tr class='alert alert-danger emp_spouse_row' style='font-size: 12px'><td><span class='icon-ban-circle'></span> " +
                        "       No Spouse listed.</td> \n" +
                        "       <td>  </td> </tr> \n";
            }
            else foreach (Dictionary<string, string> data in CensusData)
                {
                    if (data["type"] == "Spouse")
                    {
                        depTable += "  <tr class='alert alert-success emp_spouse_row dep_row' style='font-size: 12px'><td> " +
                            "       <input type='radio' class='dep_option' name='dep_option' value='" + data["ID"] + "' age='" + ageAtEffectiveDate(data["birthdate"], effectiveDate) + "'> </td> \n" +
                                    "       <td> " + data["fname"] + " " + data["lname"] + " </td> </tr> \n";
                    }
                    else if (data["type"] == "Child")
                    {
                        depTable += "  <tr class='alert alert-success sp_row dep_row' style='font-size: 12px; opacity: .75;'> \n" +
                             "       <td><input type='checkbox' class='dep_option' name='dep_option' id='dep_option_chk' value='" + data["ID"] + "' age='" + ageAtEffectiveDate(data["birthdate"], effectiveDate) + "'> </td> \n" +
                                   "       <td> " + data["fname"] + " " + data["lname"] + " </td> </tr> \n";
                    }

                    if (data["type"] != "Self")
                    {
                        depTable += "  <tr class='alert alert-success emp_1_row dep_row' style='font-size: 12px; opacity: .75;'> \n" +
                               "       <td><input type='radio' class='dep_option' name='dep_option' value='" + data["ID"] + "' age='" + ageAtEffectiveDate(data["birthdate"], effectiveDate) + "'> </td> \n" +
                                  "       <td> " + data["fname"] + " " + data["lname"] + " </td> </tr> \n";
                    }

                }
        }
        catch (Exception e)
        {
            Response.Write("Error while creating Dependents table: " + e.Message);
        }
        depTable += "</table> \n ";
        return depTable;
    }

    protected string addSimpleDependentsBox()
    {
        string depTable = "   <table id='dependents_box' class='table table-condensed table-bordered' id='dependentsTable'>" +
                          "       <tr style='background: #fff'><td colspan='2'> SELECT DEPENDENT </td> </tr> \n";
        //        "       <tr><th>&nbsp;</th><th><center>Name</th> <th><center>Percent</center></th> <th><center>Type</center></th> </tr> \n";


        try
        {
            bool hasHeader = false;
            // *** IF YES, ADD DEPENDENTS BOX ***
            if (CensusData.Count == 1)
            {
                depTable += "  <tr class='alert alert-success emp_only_row dep_row' style='font-size: 12px;'><td> " +
                "       <input type='radio' class='dep_option' name='dep_option' value='NONE' checked='checked'> </td> \n" +
                "       <td> No Dependents Listed. </td> </tr> \n";
            }
            else foreach (Dictionary<string, string> data in CensusData)
                {
                    //Response.Write(data["type"]);
                    if (data["type"] == "Spouse")
                    {
                        depTable += "<tr style='font-size: 12px; opacity: .75;background: #fff'><td colspan='2'> <b>Spouse </b> </td> </tr> \n" +
                                "  <tr class='alert alert-success dep_row' style='font-size: 12px; opacity: .75;'> \n" +
                                "       <td><input type='checkbox' class='dep_option' name='dep_option[]' id='dep_option_chk' value='" + data["ID"] + "' dep_type='spouse' /> </td> \n" +
                                "       <td> " + data["fname"] + " " + data["lname"] + " </td> </tr> \n";
                    }
                    if (data["type"] == "Child")
                    {
                        if (!hasHeader)
                        {
                            depTable += "<tr style='font-size: 12px; opacity: .75;background: #fff'><td colspan='2'> <b>Child </b> </td> </tr> \n";
                            hasHeader = true;
                        }
                        depTable += "  <tr class='alert alert-success dep_row' style='font-size: 12px; opacity: .75;'> \n" +
                                "       <td><input type='checkbox' class='dep_option' name='dep_option[]' id='dep_option_chk' value='" + data["ID"] + "' dep_type='child' /> </td> \n" +
                                "       <td> " + data["fname"] + " " + data["lname"] + " </td> </tr> \n";
                    }
                }
        }
        catch (Exception e)
        {
            Response.Write("Error while creating Dependents table: " + e.Message);
        }
        depTable += "</table> \n ";
        return depTable;
    }

    protected string addBeneficiariesList()
    {
        string benTable = "   <table class='table table-condensed table-bordered table-hover' border='1'> <tr> <td style='background:#fff' colspan='4'>SELECT BENEFICIARY | " +
            //"<button style='padding: 0;border: none;background: none; color:#000; font-size: 11px;' type='button' onclick='showBenPop(this);' onblur='hideBenPop();'> " +
            //"Edit <i class='icon icon-pencil icon-black'></i></button> " +
                        " </td> </tr> \n" +
                "       <tr><th>&nbsp;</th><th><center>Name</th> <th><center>Percent</center></th> <th><center>Type</center></th> </tr> \n";
        try
        {
            SqlDataReader myReader2 = null;

            string SQL = "SELECT [percent], name, min(ID) AS [ID] FROM  [" + Session["currentAgency"] + "].[" + Session["group"] + "].[Beneficiaries] WHERE [EmpID] = '" + empID + "' GROUP BY name, [percent]";
            SqlCommand myCommand = new SqlCommand(SQL, myConnection);
            myReader2 = myCommand.ExecuteReader();
            numOfBeneficiaries = 0;
            if (myReader2.HasRows)
            {
                while (myReader2.Read())
                {
                    benTable += "  <tr name='beneficiary_row' class='alert alert-info' style='font-size: 12px;'><td> " +
                             "       <input type='checkbox' name='ben_option[]' value='" + myReader2["ID"].ToString() + "' checked='checked' > </td> \n" +
                             "       <td> <input class='ben_name' type='text' name='ben_name_" + myReader2["ID"].ToString() + "' value='" + myReader2["group"].ToString() + "' readonly='readonly' /> </td> " +
                             "       <td> <input style='width: 30px' class='benPercent' id='ben_percent_" + numOfBeneficiaries + "' type='text' name='ben_percent_" + myReader2["ID"].ToString() + "' maxlength='3' size='3' value='" + myReader2["percent"].ToString() + "' />% </td> " +
                             "       <td> <select class='benType' name='ben_type_" + myReader2["ID"].ToString() + "' > \n" +
                             "               <option value=''>Select... </option> \n" +
                             "               <option value='Primary'>Primary </option> \n" +
                             "               <option value='Contingent'>Contingent </option> \n" +
                             "           </select> \n" +
                             "       </td> </tr> \n";
                    numOfBeneficiaries++;
                }
                myReader2.Close();
            }
            else return "<div class='alert alert-block' style='position:absolute; top: 0px; width: 97%'> \n" +
            "   <button class='close' onclick='javascript:location.reload();' data-dismiss='alert'>Refresh.</button> \n" +
            "   <strong>Warning!</strong> This Employee does not have any beneficiaries listed.</div> \n" +
            "<div style='background:#000; position:absolute; top: 55px; bottom: 0px; height:125%; width: 100%; opacity: 0.3; filter:alpha(opacity=30);' ></div>";
        }
        catch (Exception e)
        {
            Response.Write("Error while creating Beneficiaries table: " + e.Message);
        }
        benTable += "</table> <br/><br/> \n ";
        return benTable;
    }

    public static int ageAtEffectiveDate(string birthDate, string effectiveDate)
    {
        // HttpContext.Current.Response.Write(birthDate + "||e" + effectiveDate);
        DateTime bday = DateTime.Parse(birthDate);
        //HttpContext.Current.Response.Write("}}3" + effectiveDate);
        DateTime d_effectiveDate = DateTime.Parse(effectiveDate);
        int age = d_effectiveDate.Year - bday.Year;
        if (bday > d_effectiveDate.AddYears(-age)) age--;
        return age;
    }

    public string generateWhereClause()
    {
        string whereClause = "";
        int age = 0;
        int ageS = 0;

        if (String.IsNullOrWhiteSpace(effectiveDate))
            throw new Exception("The effective date for this employee has not been set.");

        age = ageAtEffectiveDate(birthdate, effectiveDate);
        if (hasSpouse)
        {
            if (usingEEAge)
                ageS = age;
            else
                ageS = ageAtEffectiveDate(birthdateS, effectiveDate);
        }

        if ((hasAgeBand || hasAge))
        {
            if (spouseRatesDifferent && hasSpouse)
            {
                if (hasAgeBand) whereClause += " AND (([age_low]<=" + age.ToString() + " AND [age_high]>=" + age.ToString() + " AND [ee_or_spouse]='ee') " +
                    "OR ([age_low]<=" + ageS.ToString() + " AND [age_high]>=" + ageS.ToString() + " AND [ee_or_spouse]='spouse'))";
                else whereClause += " AND ([age]=" + age.ToString() + " AND [ee_or_spouse]='ee') OR ([age]=" + ageS.ToString() + " AND [ee_or_spouse]='spouse')";
            }
            else
            {
                if (hasAgeBand) whereClause += " AND [age_low]<=" + age.ToString() + " AND [age_high]>=" + age.ToString();
                else whereClause += " AND [age]=" + age.ToString();
            }
        }

        if (hasGender)
            if (String.IsNullOrWhiteSpace(gender))
                throw new Exception("This employee's <b>gender</b> has not been set.");
            else
                whereClause += " AND [gender]='" + gender + "'";

        if (hasSalaryBand)
            whereClause += " AND [salary]<" + Session["salary"];
        if (hasJobClass)
            if (Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM [" + Session["currentAgency"] + "].[" +
            Session["group"] + "].[" + productCode + "_RT] WHERE [jobclass] LIKE '|%" + Session["jobClass"] + "%|') then 1 \n   else 0 \n end")))
                whereClause += " AND [jobclass] LIKE '|%" + Session["jobClass"] + "%|' ";
            else
                whereClause += " AND [jobclass] LIKE '%|ALL_JOBCLASSES|%' ";

        if (hasLocation)
            if (Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM [" + Session["currentAgency"] + "].[" +
            Session["group"] + "].[" + productCode + "_RT] WHERE [location] LIKE '|%" + Session["location"] + "%|') then 1 \n   else 0 \n end")))
                whereClause += " AND [location] LIKE '|%" + Session["location"] + "%|' ";
            else
                whereClause += " AND [location] LIKE '%|ALL_LOCATIONS|%' ";

        if (hasState)
            if (Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM [" + Session["currentAgency"] + "].[" +
            Session["group"] + "].[" + productCode + "_RT] WHERE [state] LIKE '|%" + Session["State"] + "%|') then 1 \n   else 0 \n end")))
                whereClause += " AND [state] LIKE '|%" + Session["State"] + "%|' ";
            else
                whereClause += " AND [state] LIKE '%|ALL_STATES|%' ";
        if (hasBillingMode)
            if (Convert.ToBoolean(SiteMaster.getSingleSQLData(" select case \n   when exists (SELECT 1 FROM [" + Session["currentAgency"] + "].[" +
            Session["group"] + "].[" + productCode + "_RT] WHERE [billing_mode] LIKE '|%" + Session["billing_mode"] + "%|') then 1 \n   else 0 \n end")))
                whereClause += " AND [billing_mode] LIKE '|%" + Session["billing_mode"] + "%|' ";
            else
                whereClause += " AND [billing_mode] LIKE '%|ALL_PAYGROUPS|%' ";
        if (!String.IsNullOrWhiteSpace(whereClause)) whereClause = " WHERE " + whereClause.Substring(whereClause.IndexOf("AND") + 3);
        //SELF ENROLL CODE FOR GILIMIT VIEW
        if (hasUpperLimitGI && guaranteedIssue>0)
            whereClause += " AND [face_value]<="+guaranteedIssue;

        return whereClause;
    }

    public static bool changeAppStatus(string newStatus, string employeeID)
    {
        //*** CREATE THE SQL CONNECTION ***
        SqlConnection myConnection = new SqlConnection(SiteMaster.MyGlobals.connectionString);

        string SQL = "";
        bool status = false;

        myConnection.Open();
        Dictionary<string, string> logData = new Dictionary<string, string>();

        logData["currentAgency"] = (string)HttpContext.Current.Session["currentAgency"];
        logData["groupName"] = (string)HttpContext.Current.Session["group"];
        logData["username"] = "SELF";
        logData["date"] = DateTime.Now.ToString();
        logData["EmpID"] = employeeID;
        logData["type"] = SiteMaster.LogType.Census.ToString();
        logData["details"] = "";

        //throw new Exception (archiverString);
        if (!String.IsNullOrWhiteSpace((string)HttpContext.Current.Session["reason"]))
        {
            logData["reason"] = (string)HttpContext.Current.Session["reason"];
            HttpContext.Current.Session.Remove("reason");
        }
        else if (newStatus == "pending")
            logData["reason"] = "Change to Pending";
        else if (newStatus == "completed")
            logData["reason"] = "Change to Completed";
        else if (newStatus == "terminated")
            logData["reason"] = "Employee Terminated";
        else if (newStatus == "payment")
            logData["reason"] = "Employee Payment Details Pending";
        else
            throw new Exception("Invalid app status provided.");


        SQL = "UPDATE [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Employees] SET [app_status]='" + newStatus + "' \n"
        + " WHERE [empID]=" + employeeID;
        //throw new Exception(SQL);

        if (new SqlCommand(SQL, myConnection).ExecuteNonQuery() == 1)
            status = true;
        else
            throw new Exception("Update failed.");

        //SQL = "UPDATE [" + Session["currentAgency"] + "].[" + Session["group"] + "].[Transactions] SET [old_transaction]='yes' \n"
        //+ "WHERE [employee_ID]=" + Session["empID"];

        //if (new SqlCommand(SQL, myConnection).ExecuteNonQuery() == 1)
        //    Response.Write(SQL);
        SqlDataReader sql_Details = new SqlCommand("SELECT * FROM [" + HttpContext.Current.Session["currentAgency"] + "].[" + HttpContext.Current.Session["group"] + "].[Employees] " +
            " WHERE [empID]=" + employeeID, myConnection).ExecuteReader();

        while (sql_Details.Read())
        {
            for (int j = 0; j < sql_Details.FieldCount; j++)
                logData["details"] += "|" + sql_Details.GetName(j) + "=" + sql_Details.GetValue(j);
        }
        sql_Details.Close();

        SiteMaster.logEntry(logData);

        myConnection.Close();
        return status;
    }
    public static int logEntry(Dictionary<string, string> logData)
    {
        SqlConnection currentConnection = new SqlConnection(SiteMaster.MyGlobals.connectionString);
        //HttpContext.Current.Response.Write(MyGlobals.myConnection.State.ToString());
        currentConnection.Open();
        if (logData["t_ID"] != "0")
        {
            logData["details"] += "\n\n::INSERT ACTION::";
            SqlDataReader sql_Details = new SqlCommand("SELECT * FROM [" + logData["currentAgency"] + "].[" + logData["groupName"] + "].[Transactions] " +
                " WHERE [ID]=" + logData["t_ID"], currentConnection).ExecuteReader();

            while (sql_Details.Read())
            {
                for (int j = 0; j < sql_Details.FieldCount; j++)
                    logData["details"] += "|" + sql_Details.GetName(j) + "=" + sql_Details.GetValue(j);
            }
            sql_Details.Close();

            currentConnection.Close();
            return SiteMaster.logEntry(logData);
        }
        else
        {
            currentConnection.Close();
            throw new Exception("Could not enter Transaction data.");
        }
    }

    protected string signCS(string PDFLocation, string signature, string finalName)
    {
        //Create PDF Creator Object
        SitusPDFCreator newPDF = new SitusPDFCreator();
        newPDF.LoadFromFile(PDFLocation, "");
        //Response.Write(SiteMaster.getFolderPath("Documents") + unsignedPDF);
        //newPDF.SetTextExtractionOptions(15, 1);
        //Response.Write(newPDF.GetPageText(4));
        string pText = newPDF.GetPageText(4);
        //StringBuilder pT = new StringBuilder(pText);
        //pT.Replace("\"", "");
        //pT.Replace(" Arial", "").Replace(" Arial,Bold", "").Replace("Arial", "").Replace("Arial,Bold", "");
        pText = pText.Replace("\"", "").Replace(" ", "").Replace("  ", "").Replace(":", ",").Replace("Arial", "").Replace("Arial,Bold", "").Replace("Bold", "").Replace(";", "");
        string[] pageText = pText.Split(',').ToArray();
        string buf = string.Join(";", pageText);
        int ind = Array.LastIndexOf(pageText, "Signature");
        Response.Write("<p/> IND: " + ind + "<p/>");
        double sy = (Double.Parse(pageText[ind - 1])) * (.355) + 10;
        double sx = Double.Parse(pageText[ind - 2]) - 22;

        byte[] signatureImg = Convert.FromBase64String(signature);

        //using (System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(signatureImg)))
        //{
        //    image.Save(filename, ImageFormat.Png);  // Or Png
        //}

        newPDF.SetMeasurementUnits(1);
        newPDF.SetOrigin(0);
        //newPDF.CompressImages(1);
        int newpic = newPDF.AddImageFromString(signatureImg, 2);
        newPDF.SelectImage(newpic);
        //newPDF.ReverseImage(0);
        //newPDF.DrawImage(30, 253.6, 50, 10);
        newPDF.DrawImage(sx, sy, 50, 15);

        newPDF.insertHTMLLine(sx + 85, sy - 8, 50, "<font size=\"3\">" + DateTime.Now.ToString("MM/dd/yyyy") + "</font>");

        newPDF.FlattenAndSave(finalName);

        ////newPDF.AddImageFromString();
        if (File.Exists(finalName))
            return finalName;
        else
            throw new Exception("Signed PDF not saved.");
    }

    protected string signPDF(string PDFLocation, string signature, string finalName)
    {
        //Create PDF Creator Object
        SitusPDFCreator newPDF = new SitusPDFCreator();
        newPDF.LoadFromFile(PDFLocation, "");
        //Response.Write(SiteMaster.getFolderPath("Documents") + unsignedPDF);
        //newPDF.SetTextExtractionOptions(15, 1);
        //Response.Write(newPDF.GetPageText(4));
        string pText = newPDF.GetPageText(4);
        //StringBuilder pT = new StringBuilder(pText);
        //pT.Replace("\"", "");
        //pT.Replace(" Arial", "").Replace(" Arial,Bold", "").Replace("Arial", "").Replace("Arial,Bold", "");
        pText = pText.Replace("\"", "").Replace(" ", "").Replace("  ", "").Replace(":", ",").Replace("Arial", "").Replace("Arial,Bold", "").Replace("Bold", "").Replace(";", "");
        string[] pageText = pText.Split(',').ToArray();
        string buf = string.Join(";", pageText);
        byte[] signatureImg = Convert.FromBase64String(signature);
        double sy = -1;
        double sx = -1;
        double height = -1;
        double width = -1;

        int FieldID = newPDF.FindFormFieldByTitle("EE_Sign");
        if (FieldID > 0)
        {
            newPDF.SelectPage(newPDF.GetFormFieldPage(FieldID));
            newPDF.SetMeasurementUnits(1);
            newPDF.SetOrigin(0);
            //double test = newPDF.GetFormFieldBound(FieldID, 0);
            //Response.Write("<p/> test: " + test + "<p/>");
            sy = newPDF.GetFormFieldBound(FieldID, 1) + 3;
            sx = newPDF.GetFormFieldBound(FieldID, 0) - 7;
            height = newPDF.GetFormFieldBound(FieldID, 3) + 5;
            width = newPDF.GetFormFieldBound(FieldID, 2);

            newPDF.DeleteFormField(FieldID);
            //using (System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(signatureImg)))
            //{
            //    image.Save(filename, ImageFormat.Png);  // Or Png
            //}
            //newPDF.CompressImages(1);
            int newpic = newPDF.AddImageFromString(signatureImg, 2);
            newPDF.SelectImage(newpic);
            //newPDF.ReverseImage(0);
            //newPDF.DrawImage(30, 253.6, 50, 10);
            newPDF.DrawImage(sx, sy, 50 * (15 / height), height);

            //newPDF.insertHTMLLine(sx + 85, sy - 8, 50, "<font size=\"3\">" + DateTime.Now.ToString("MM/dd/yyyy") + "</font>");

            //newPDF.FlattenAndSave(finalName);
        }

        FieldID = newPDF.FindFormFieldByTitle("Agent_Sign");
        if (FieldID > 0) newPDF.DeleteFormField(FieldID);
        newPDF.SaveToFile(finalName);
        ////newPDF.AddImageFromString();
        if (File.Exists(finalName))
            return finalName;
        else
            throw new Exception("Signed PDF not saved.");
    }

    public static string determineReportType(Dictionary<string, string> Details)
    {
        string rt = null;
        double oldDeduction = -1;
        double newDeduction = -1;

        //Response.Write("<br> old = " + myReader["amount"] + " new = '" + myReader["deduction_amt"] + "'");
        if (!String.IsNullOrWhiteSpace(Details["amount"]))
            oldDeduction = Convert.ToDouble(Details["amount"]);
        if (!String.IsNullOrWhiteSpace(Details["deduction_amt"]))
            newDeduction = Convert.ToDouble(Details["deduction_amt"]);

        string coverageLevel = "";
        if (!String.IsNullOrWhiteSpace(Details["coverage_level"]))
            coverageLevel = Details["coverage_level"];
        string tier = "";
        if (Details.ContainsKey("tier") && !String.IsNullOrWhiteSpace(Details["tier"]))
            tier = Details["tier"];

        if (coverageLevel == "--WAIVED--" || tier == "--WAIVED--")
            return "Terminate";

        if (coverageLevel == "--TERMINATED--" || tier == "--TERMINATED--")
            return "Terminate";

        string faceValue = "";
        if (!String.IsNullOrWhiteSpace(Details["face_value"]))
            faceValue = Details["face_value"];

        if (newDeduction == -1 && oldDeduction != -1)
        {
            rt = "Terminate";
        }
        else
            rt = "No Change";

        if (newDeduction > -1 && oldDeduction == -1)
        {
            rt = "Add";
        }
        if (newDeduction > -1 && oldDeduction > -1)
        {
            rt = "Change";
        }
        if ((newDeduction == oldDeduction && newDeduction != -1) && coverageLevel != "--WAIVED--")
        //if (coverageLevel != "--WAIVED--" && coverageLevel == Details["cd_coverage_level"] && faceValue == Details["cd_face_value"])
        {
            rt = "No Change";
        }

        if (newDeduction == -1)
            newDeduction = 0;
        if (oldDeduction == -1)
            oldDeduction = 0;

        return rt;
    }

    protected LinkButton submit = new LinkButton()
    {
        ID = "submit",
        Visible = true,
        Text = "Submit",
        CssClass = "btn btn-info btn-mini",
        ClientIDMode = ClientIDMode.Static,
        Enabled = false
    };

    protected Panel generateSignaturePanel(bool noText, string code, bool confirmStatement)
    {
        Panel signaturePanel = new Panel();
        Label text = new Label();
        Label fraudWarning = new Label();
        Literal confirmTable = new Literal();
        Table tt = new Table();

        if (!noText)
        {
            if (!String.IsNullOrWhiteSpace(code))
            {
                text.Text = (string)SiteMaster.getSingleSQLData("SELECT [sign_text] FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[Products] WHERE [code]='" + code + "'");
            }
            else if (code == "COMBI_LIF" || code == "COMBI_LI1")
            {
                text.Text = "<center><b>Declaration, Agreement and Authorization to Release Information</b></center> <br/> \n" +
                    "I/We declare that each answer given to the questions contained in this enrollment is complete and true to the " +
                    "best of our knowledge and belief.  I understand and agree that the company will rely on these answers, and the " +
                    "answers and statements I may give in any other form taken as part of this Group Enrollment Form.  I also understand " +
                    "that The Company reserves the right to accept or deny this coverage after taking into account whatever information " +
                    "may be available to it. <br/><br/> \n" +

                    "I/We authorize any physician, medical practitioner, hospital, clinic, pharmacy benefit manager or other medical or " +
                    "medically related facility, insurance or reinsurance company, MIB, Inc., or employer to give to Combined Insurance " +
                    "Company of America any information they might have regarding the diagnosis, treatment, prescription and prognosis of " +
                    "any physical or mental condition as applicable. To facilitate the rapid transmissions of such information, I authorize " +
                    "all said sources, to give such records or knowledge to any agency employed by The Company to collect and transmit such " +
                    "information. I agree that this authorization shall remain in effect for two years (24 months) from the date that it is " +
                    "signed and that a copy of it shall be as valid as the original. I understand that the information obtained with this " +
                    "authorization shall be used to evaluate my request for insurance or to evaluate a claim during the time that this " +
                    "authorization is valid. I also understand that I, or someone I authorize to act on my behalf, may obtain a copy of this " +
                    "authorization. <br/><br/> \n" +

                    "I/We authorize The Company or its reinsurers to make a brief report of my protected health information to MIB, Inc. <br/> \n" +

                    "<b>If coverage cannot be issued as applied for under the rules of The Company, I/We authorize Combined Insurance Company of " +
                    "America to issue available reduced benefits and adjust premiums to match the coverage issued. </b> I authorize my emplyoer to " +
                    "deduct the premiums for this insurance from my earnings (unless the coverage for which I am applying allows for alternate " +
                    "methods to pay insurance premiums). <br/> \n";
            }
            else
            {
                text.Text = "<center><b>Declaration, Agreement and Authorization To Release Information</b></center> <br/> \n" +
                    "I declare that each answer given to the questions contained in this enrollment form is complete and true to the best of my knowledge and belief." +
                    "I understand and agree that the company will rely on these answers,and the answers and statements I may give in any other form taken as part of this enrollment form." +
                    "I also understand that the Company reserves the right to accept or deny this enrollment form after taking into account whatever information may be available to it, " +
                    "including availability as to coverage by its reinsurers. All statements and answers on this enrollment form are full, complete and true to the best knowledge and " +
                    "belief of each person who has signed below.<br/> \n" +
                    "<b>The insurance being applied for will be effective as of the enrollment form date, " +
                    "provided the person(s) to be insured is (are) found acceptable for such insurance as applied for.</b><br/><br/> \n" +

                    "I/We authorize any physician, medical practitioner, hospital, clinic, pharmacy, pharmacy benefit manager or other medical or medically related facility, insurance or " +
                    "reinsurance company, the Medical Information Bureau (MIB) or employer to give to Fidelity Life Association any information they might have regarding the diagnosis, " +
                    "treatment, prescription and prognosis of any physical or mental condition as applicable. To facilitate the rapid transmissions of such information, " +
                    "I authorize all said sources, to give such records or knowledge to any agency employed by the Company to collect and transmit such information. I agree that this " +
                    "authorization shall remain in effect for two years (24 months) from the date that it is signed and that a copy of it shall be as valid as the original. " +
                    "I understand that the information obtained with this authorization shall be used to evaluate my request for insurance or to evaluate a claim during the time " +
                    "that this authorization is valid. I also understand that I, or someone I authorize to act on my behalf, may obtain a copy of this authorization.<br/><br/>\n\n" +

                    "All or part of such information may be disclosed to a physician of my choosing, my insurance agent, the Medical Information Bureau (MIB), to other persons or " +
                    "organizations performing business or legal services in connection with this enrollment form, including reinsuring companies as may be required by law.<br/><br/>\n\n" +

                    "The Certificate Holder/Insured and the agent certify that no illustration conforming to the coverage applied for was provided, " +
                    "but that an illustration conforming to the coverage issued will be provided upon delivery. ";
            }
        }
        else
            text.Text = "";

        fraudWarning.Text = "<b>Fraud Warning:</b> Any person who knowingly presents a false or fraudulent claim for payment of a loss or knowingly makes a false statement in an " +
            "enrollment form for insurance for the purpose of defrauding the insurer or any other person may be guilty of a crime to provide false or misleading information. " +
            "Penalties include imprisonment and/or fines. An insurer may deny insurance benefits if the applicant provided false information materially related to a claim. <p/>";

        confirmTable.Text = "<center> <table class='table table-condensed table-bordered'> \n" +
            "<tr> <td> Please enter your mother's maiden name:</td> <td><input type='text' id='maiden' name='maiden' /> </td> </tr> \n" +
            "<tr> <td> Re-enter your mother's maiden name:</td> <td> <input type='text' id='maiden1' name='maiden1' onblur='checkMaidenNames()' /> </td> </tr> \n" +
            "<tr> <td colspan='2' id='errorBox' style='text-align:center'>  </td> </tr> \n" +
            "<tr> <td> Please enter the last 4 of your SSN:</td> <td> <input type='password' name='pin' onkeyup='checkPIN(this)' maxlength='4' /> </td> </tr> \n" +
            "<tr> <td colspan='2' style='text-align:center'> <input type='checkbox' id='submitYes' onclick='agreesubmit(this)' disabled /> <label for='submitYes'>I agree to the above terms.</label> </td> </tr> \n" +
            "<tr> <td colspan='2' style='text-align:center'> <input type='hidden' name='signPDF' id='signPDF' value='' /> ";
        // "<input type='submit' class='btn btn-info btn-mini' id='submit' value='Submit' disabled /> </td> </tr> </table> </center> \n";
        if (confirmStatement)
            confirmTable.Text += "      <input type='submit' id='submit' class='btn btn-dark-blue' value='Process Confirmation Statement' disabled /> <br />";
        else
            confirmTable.Text += "<input type='submit' class='btn btn-info btn-mini' id='submit' value='Submit' disabled /> </td> </tr> </table> </center> \n";

        TableRow tr = new TableRow();
        tr.Cells.Add(new TableCell() { Text = text.Text });
        tt.Rows.Add(tr);

        TableRow tr1 = new TableRow();
        tr1.Cells.Add(new TableCell() { Text = fraudWarning.Text });
        tt.Rows.Add(tr1);


        TableRow tr2 = new TableRow();
        tr2.Cells.Add(new TableCell() { Text = confirmTable.Text });
        tt.Rows.Add(tr2);

        tt.CssClass = "table table-condensed table-bordered";
        tt.Width = Unit.Percentage(60);

        signaturePanel.Controls.Add(new LiteralControl() { Text = "<center>" });
        signaturePanel.Controls.Add(tt);
        signaturePanel.Controls.Add(new LiteralControl() { Text = "</center>" });

        signaturePanel.Visible = true;

        return signaturePanel;
    }


    protected Panel generateSignaturePanel(bool noText, string code)
    {
        Panel signaturePanel = new Panel();
        Literal text = new Literal();
        Literal fraudWarning = new Literal();
        Literal confirmTable = new Literal();
        Table tt = new Table();

        if (!noText)
        {
            if (!String.IsNullOrWhiteSpace(code))
            {
                text.Text = (string)SiteMaster.getSingleSQLData("SELECT [sign_text] FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[Products] WHERE [code]='" + code + "'");
            }
            else if (code == "COMBI_LIF" || code == "COMBI_LI1")
            {
                text.Text = "<center><b>Declaration, Agreement and Authorization to Release Information</b></center> <br/> \n " +
                    "I/We declare that each answer given to the questions contained in this enrollment is complete and true to the " +
                    "best of our knowledge and belief.  I understand and agree that the company will rely on these answers, and the " +
                    "answers and statements I may give in any other form taken as part of this Group Enrollment Form.  I also understand " +
                    "that The Company reserves the right to accept or deny this coverage after taking into account whatever information " +
                    "may be available to it. <br/><br/> \n " +

                    "I/We authorize any physician, medical practitioner, hospital, clinic, pharmacy benefit manager or other medical or " +
                    "medically related facility, insurance or reinsurance company, MIB, Inc., or employer to give to Combined Insurance " +
                    "Company of America any information they might have regarding the diagnosis, treatment, prescription and prognosis of " +
                    "any physical or mental condition as applicable. To facilitate the rapid transmissions of such information, I authorize " +
                    "all said sources, to give such records or knowledge to any agency employed by The Company to collect and transmit such " +
                    "information. I agree that this authorization shall remain in effect for two years (24 months) from the date that it is " +
                    "signed and that a copy of it shall be as valid as the original. I understand that the information obtained with this " +
                    "authorization shall be used to evaluate my request for insurance or to evaluate a claim during the time that this " +
                    "authorization is valid. I also understand that I, or someone I authorize to act on my behalf, may obtain a copy of this " +
                    "authorization. <br/><br/> \n " +

                    "I/We authorize The Company or its reinsurers to make a brief report of my protected health information to MIB, Inc. <br/> \n " +

                    "<b>If coverage cannot be issued as applied for under the rules of The Company, I/We authorize Combined Insurance Company of " +
                    "America to issue available reduced benefits and adjust premiums to match the coverage issued. </b> I authorize my emplyoer to " +
                    "deduct the premiums for this insurance from my earnings (unless the coverage for which I am applying allows for alternate " +
                    "methods to pay insurance premiums). <br/> \n ";
            }
            else
            {
                text.Text = "<center><b>Declaration, Agreement and Authorization To Release Information</b></center> <br/> \n " +
                    "I declare that each answer given to the questions contained in this enrollment form is complete and true to the best of my knowledge and belief." +
                    "I understand and agree that the company will rely on these answers,and the answers and statements I may give in any other form taken as part of this enrollment form." +
                    "I also understand that the Company reserves the right to accept or deny this enrollment form after taking into account whatever information may be available to it, " +
                    "including availability as to coverage by its reinsurers. All statements and answers on this enrollment form are full, complete and true to the best knowledge and " +
                    "belief of each person who has signed below.<br/> \n " +
                    "<b>The insurance being applied for will be effective as of the enrollment form date, " +
                    "provided the person(s) to be insured is (are) found acceptable for such insurance as applied for.</b><br/><br/> \n " +

                    "I/We authorize any physician, medical practitioner, hospital, clinic, pharmacy, pharmacy benefit manager or other medical or medically related facility, insurance or " +
                    "reinsurance company, the Medical Information Bureau (MIB) or employer to give to Fidelity Life Association any information they might have regarding the diagnosis, " +
                    "treatment, prescription and prognosis of any physical or mental condition as applicable. To facilitate the rapid transmissions of such information, " +
                    "I authorize all said sources, to give such records or knowledge to any agency employed by the Company to collect and transmit such information. I agree that this " +
                    "authorization shall remain in effect for two years (24 months) from the date that it is signed and that a copy of it shall be as valid as the original. " +
                    "I understand that the information obtained with this authorization shall be used to evaluate my request for insurance or to evaluate a claim during the time " +
                    "that this authorization is valid. I also understand that I, or someone I authorize to act on my behalf, may obtain a copy of this authorization.<br/><br/>\n \n " +

                    "All or part of such information may be disclosed to a physician of my choosing, my insurance agent, the Medical Information Bureau (MIB), to other persons or " +
                    "organizations performing business or legal services in connection with this enrollment form, including reinsuring companies as may be required by law.<br/><br/>\n\n " +

                    "The Certificate Holder/Insured and the agent certify that no illustration conforming to the coverage applied for was provided, " +
                    "but that an illustration conforming to the coverage issued will be provided upon delivery. ";
            }
        }
        else
            text.Text = "";

        fraudWarning.Text = "<b>Fraud Warning:</b> Any person who knowingly presents a false or fraudulent claim for payment of a loss or knowingly makes a false statement in an " +
            "enrollment form for insurance for the purpose of defrauding the insurer or any other person may be guilty of a crime to provide false or misleading information. " +
            "Penalties include imprisonment and/or fines. An insurer may deny insurance benefits if the applicant provided false information materially related to a claim. <p/>";

        confirmTable.Text = "<center> <table class='table table-condensed table-bordered'> \n  " +
            "<tr> <td> Please enter your mother's maiden name:</td> <td><input type='text' id='maiden' name='maiden' /> </td> </tr> \n  " +
            "<tr> <td> Re-enter your mother's maiden name:</td> <td> <input type='text' id='maiden1' name='maiden1' onblur='checkMaidenNames()' /> </td> </tr> \n " +
            "<tr> <td colspan='2' id='errorBox' style='text-align:center'>  </td> </tr> \n " +
            "<tr> <td> Please enter the last 4 of your SSN:</td> <td> <input type='password' name='pin' onkeyup='checkPIN(this)' maxlength='4' /> </td> </tr> \n " +
            "<tr> <td colspan='2' style='text-align:center'> <input type='checkbox' id='submitYes' onclick='agreesubmit(this)' disabled /> <label for='submitYes'>I agree to the above terms.</label> </td> </tr> \n " +
            "<tr> <td colspan='2' style='text-align:center'> <input type='hidden' name='signPDF' id='signPDF' value='' /> " +
            "<input type='submit' class='btn btn-info btn-mini' id='submit' value='Submit' disabled /> </td> </tr> </table> </center> \n ";

        TableRow tr = new TableRow();
        tr.Cells.Add(new TableCell() { Text = text.Text });
        tt.Rows.Add(tr);

        TableRow tr1 = new TableRow();
        tr1.Cells.Add(new TableCell() { Text = fraudWarning.Text });
        tt.Rows.Add(tr1);


        TableRow tr2 = new TableRow();
        tr2.Cells.Add(new TableCell() { Text = confirmTable.Text });
        tt.Rows.Add(tr2);

        tt.CssClass = "table table-condensed table-bordered";
        tt.Width = 750;

        signaturePanel.Controls.Add(new LiteralControl() { Text = "<center>" });
        signaturePanel.Controls.Add(tt);
        signaturePanel.Controls.Add(new LiteralControl() { Text = "</center>" });

        signaturePanel.Visible = true;

        return signaturePanel;
    }


    protected void redirectToPP(object sender, EventArgs e)
    {
        Response.Redirect(ResolveUrl("~") + "productsGrid.aspx");
    }

    protected void enableSubmit(object sender, EventArgs e)
    {
        submit.Enabled = true;
    }

    public class AgeException : Exception
    {
        public AgeException()
        {
        }

        public AgeException(string message)
            : base(message)
        {
        }
    }

    public class RedirectException : Exception
    {
        public RedirectException()
        {
        }

        public RedirectException(string message)
            : base(message)
        {
        }
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

    //////////////////////////////////////// DEPRECATED FUNCTIONS ////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    private string createGroupLifeTable(string birthDate)
    {
        //*** SETUP TABLE ***
        string table = "";

        int age = ageAtEffectiveDate(birthDate, effectiveDate);

        table = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTable'> \n" +
            "<tr><td colspan='3' style='background: #fff; color:#000'>Age " + age.ToString() + " on Effective Date " + String.Format("{0:MM/dd/yyyy}", effectiveDate) + "</td></tr> \n";
        table += "<tr><td> &nbsp; </td> <td>Face Value </td> <td>Premium </td> </tr> \n";

        try
        {
            SqlDataReader myReader2 = null;
            string SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT]  WHERE [age_low]<" + age.ToString() + " AND [age_high]>" + age.ToString();
            SqlCommand myCommand = new SqlCommand(SQL, myConnection);
            myReader2 = myCommand.ExecuteReader();
            while (myReader2.Read())
            {
                //*** ADD GROUP LIFE OPTIONS ***
                table += "<tr class='ins_row GrpLifePlan'><td> <input type='radio' name='ins_option' value='" + myReader2["cost"].ToString() + "' face='" + myReader2["face_value"].ToString() + "'> </td> " +
                    "<td> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString()) + "</td> " +
                    "<td> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
            }
            myReader2.Close();
        }
        catch (Exception e)
        {
            Response.Write("Error while creating Group Life rates table: " + e.Message);
        }
        table += "</table> <br/><br/> \n ";
        return table;
    }  // *** THIS FUNCTION HAS BEEN DEPRECATED ***

    private string createCancerTable()
    {

        // *** SETUP TABLE ***
        string table = " <table class='table table-condensed table-bordered' style='width:100%; padding-bottom:-100px' id='rateTable'> " +
        "<tr><td colspan='7' bgcolor='#ffffff'>Enroll for Cancer</td></tr> \n" +
        "<tr><td width='40%'><b>Plan</b></td> \n";

        // *** ADD TIER NAMES TO TABLE ***
        List<string> tier = new List<string>();

        SqlDataReader myReader2 = null;
        string SQL = "SELECT [pl_family] FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] GROUP BY [pl_family] ORDER BY min([pl_prem]) ";
        SqlCommand myCommand = new SqlCommand(SQL, myConnection);
        myReader2 = myCommand.ExecuteReader();
        for (int i = 0; myReader2.Read(); i++)
        {
            table += "   <td><b>" + SiteMaster.UppercaseFirst(myReader2["pl_family"].ToString()) + "</b></td> \n";
            tier.Add(myReader2["pl_family"].ToString());
        }
        table += "</tr> \n";

        // *** ADD PREMIUMS TO TABLE ***
        try
        {
            myReader2 = null;
            SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] WHERE [group]='" + Session["group"] + "' ";
            myCommand = new SqlCommand(SQL, myConnection);
            myReader2 = myCommand.ExecuteReader();
            for (int i = 0; myReader2.Read(); i++)
            {
                // *** CHECK IF PLAN HAS BEEN ADDED ***
                if (table.IndexOf("<td class='CancerPlan'>" + myReader2["pl_desc"].ToString() + "</td>") == -1)
                {
                    // *** IF NO, ADD PLAN ENTRY AND FIRST OPTION ***
                    table += "<tr ><td class='CancerPlan'>" + myReader2["pl_desc"].ToString() + "</td>" +
                            "<td class='ins_row'><input type='radio' name='ins_option' value='" + myReader2["pl_prem"].ToString() + "' benefit='" + myReader2["pl_desc"].ToString() + "' tier='" + tier[i] +
                            "' ascb='" + myReader2["ascb"].ToString() +
                            "' fob='" + myReader2["fob"].ToString() +
                            "' fobb='" + myReader2["fobb"].ToString() +
                            "' rcib='" + myReader2["rcib"].ToString() +
                            "' sb='" + myReader2["sb"].ToString() +
                            "' dhcb='" + myReader2["dhcb"].ToString() +
                            "' sdb='" + myReader2["sdb"].ToString() +
                            "' icub='" + myReader2["icub"].ToString() +
                            "'/> " + SiteMaster.toCurrency(myReader2["pl_prem"].ToString()) + "</td>" + " </tr> \n";
                }
                else
                {
                    // *** IF YES, ADD NEXT OPTION ***

                    int pos = table.LastIndexOf("</td>");
                    string entry = "<td class='ins_row'><input type='radio' name='ins_option' value='" + myReader2["pl_prem"].ToString() + "' benefit='" + myReader2["pl_desc"].ToString() + "' tier='" + tier[i] +
                            "' ascb='" + myReader2["ascb"].ToString() +
                            "' fob='" + myReader2["fob"].ToString() +
                            "' fobb='" + myReader2["fobb"].ToString() +
                            "' rcib='" + myReader2["rcib"].ToString() +
                            "' sb='" + myReader2["sb"].ToString() +
                            "' dhcb='" + myReader2["dhcb"].ToString() +
                            "' sdb='" + myReader2["sdb"].ToString() +
                            "' icub='" + myReader2["icub"].ToString() +
                            "'/> " + SiteMaster.toCurrency(myReader2["pl_prem"].ToString()) + "</td>";
                    //Response.Write(pos + " = " + entry + " <br/> \n");

                    table = table.Insert(pos, entry);
                }
                if (i == (tier.Count - 1)) i = -1;
            }
            table += "</table> \n ";
            myReader2.Close();
        }

        catch (Exception e)
        {

            Response.Write("Error while creating Cancer rates table: " + e.Message);
        }
        return table;
    }  // *** THIS FUNCTION HAS BEEN DEPRECATED ***

    private string createDisabilityTable(string salary)
    {
        //*** SETUP TABLE ***
        string table = "";
        table = "<table class='table table-condensed table-bordered' style='width:100%' id='rateTable'> \n" +
                "   <tr><td><center><b>Benefit</b></td> \n" +
                "       <td><center><b>7/7</b></td> \n" +
                "       <td><center><b>14/14</b></td> \n" +
                "       <td><center><b>30/30</b></td> \n" +
                "       <td><center><b>60/60</b></td> \n" +
                "       <td><center><b>90/90</b></td> \n" +
                "       <td><center><b>120/120</b></td></tr> \n";

        try
        {
            SqlDataReader myReader2 = null;
            string SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] WHERE [salary]<" + (Convert.ToDouble(salary) / 12).ToString(); // Salary is listed per month in DB
            SqlCommand myCommand = new SqlCommand(SQL, myConnection);
            myReader2 = myCommand.ExecuteReader();
            while (myReader2.Read())
            {
                //*** ADD DISABILITTY OPTIONS ***
                table += "  <tr><td class='DisabilityPlan'> $" + myReader2["benefit"].ToString() + "</td>" +
                        "       <td class='ins_row'><input type='radio' name='ins_option' face=" + myReader2["benefit"].ToString() + " value='" + myReader2["option1"].ToString() + "' tier='7/7'/> $" + SiteMaster.toCurrency(myReader2["option1"].ToString()) + "</td> \n" +
                        "       <td class='ins_row'><input type='radio' name='ins_option' face=" + myReader2["benefit"].ToString() + " value='" + myReader2["option2"].ToString() + "' tier='14/14'/> $" + SiteMaster.toCurrency(myReader2["option2"].ToString()) + "</td> \n" +
                        "       <td class='ins_row'><input type='radio' name='ins_option' face=" + myReader2["benefit"].ToString() + " value='" + myReader2["option3"].ToString() + "' tier='30/30'/> $" + SiteMaster.toCurrency(myReader2["option3"].ToString()) + "</td> \n" +
                        "       <td class='ins_row'><input type='radio' name='ins_option' face=" + myReader2["benefit"].ToString() + " value='" + myReader2["option4"].ToString() + "' tier='60/60'/> $" + SiteMaster.toCurrency(myReader2["option4"].ToString()) + "</td> \n" +
                        "       <td class='ins_row'><input type='radio' name='ins_option' face=" + myReader2["benefit"].ToString() + " value='" + myReader2["option5"].ToString() + "' tier='90/90'/> $" + SiteMaster.toCurrency(myReader2["option5"].ToString()) + "</td> \n" +
                        "       <td class='ins_row'><input type='radio' name='ins_option' face=" + myReader2["benefit"].ToString() + " value='" + myReader2["option6"].ToString() + "' tier='120/120'/> $" + SiteMaster.toCurrency(myReader2["option6"].ToString()) + "</td> </tr> \n";
            }
            myReader2.Close();
        }
        catch (Exception e)
        {
            Response.Write("Error while creating Disability rates table: : " + e.Message);
        }
        table += "</table> <br/><br/> \n ";
        return table;
    }  // *** THIS FUNCTION HAS BEEN DEPRECATED ***

    private string createLifeTable()
    {
        string table;
        int age = ageAtEffectiveDate(birthdate, effectiveDate);

        //*** SETUP TABLE ***
        table = "<table style='width:100%' class='table table-condensed table-bordered' id='rateTable'> \n" +
            "<tr><td colspan='3' style='background: #fff; color:#000'>Age " + age.ToString() + " on Effective Date " + String.Format("{0:MM/dd/yyyy}", effectiveDate) + "</td></tr> \n" +
            "<tr><td colspan='3' style='background: #fff; color:#000'>Is the insured a Tobacco User? <input type='radio' name='smoker' id='smoker' value='No' checked='checked'> No " +
            "<input type='radio' name='smoker' id='smoker' value='Yes'> Yes " + "</td> </tr> \n";
        table += "<tr><td> &nbsp; </td> <td>Face Value </td> <td>Premium </td> </tr> \n";

        //*** ADD NON-SMOKER OPTIONS ***
        SqlDataReader myReader2 = null;
        string SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "_RT] WHERE [age]=" + age.ToString();
        //Response.Write(SQL);
        SqlCommand myCommand = new SqlCommand(SQL, myConnection);
        myReader2 = myCommand.ExecuteReader();
        while (myReader2.Read())
        {
            if (Convert.ToDouble(myReader2["cost"].ToString()) != 0)
                table += "<tr class='ins_row smokerN'><td> <input type='radio' name='ins_option' id='smokerN' value='" + myReader2["cost"].ToString() + "' face='" + myReader2["face_value"].ToString() + "'> </td> " +
                "<td> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString()) + "</td> " +
                "<td> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
            else
                table += "<tr class='NA smokerN'><td> <input type='radio' name='ins_option' id='smokerN' value='" + myReader2["cost"].ToString() + "' face='" + myReader2["face_value"].ToString() + "' disabled> </td> " +
                "<td> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString()) + "</td> " +
                "<td> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
        }
        myReader2.Close();

        //*** ADD SMOKER OPTIONS ***
        SQL = "SELECT * FROM [" + Session["currentAgency"] + "].[" + Session["group"] + "].[" + productCode + "S_RT] WHERE [age]=" + age.ToString();
        myCommand = new SqlCommand(SQL, myConnection);
        myReader2 = myCommand.ExecuteReader();
        while (myReader2.Read())
        {
            if (Convert.ToDouble(myReader2["cost"].ToString()) != 0)
                table += "<tr class='ins_row smokerY'><td> <input type='radio' name='ins_option' id='smokerY' value='" + myReader2["cost"].ToString() + "' face='" + myReader2["face_value"].ToString() + "'> </td> " +
                "<td> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString()) + "</td> " +
                "<td> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
            else
                table += "<tr class='NA smokerY'><td> <input type='radio' name='ins_option' id='smokerY' value='" + myReader2["cost"].ToString() + "' face='" + myReader2["face_value"].ToString() + "' disabled> </td> " +
                "<td> $" + SiteMaster.toCurrency(myReader2["face_value"].ToString()) + "</td> " +
                "<td> $" + SiteMaster.toCurrency(myReader2["cost"].ToString()) + "</td> </tr> \n";
        }
        table += "<tr><td style='font-size:10px' colspan='3'>&nbsp;</td></tr> \n";
        myReader2.Close();

        //*** BUILD REST OF TABLE ***
        table += "<tr><td colspan='2'>Is the employee actively at work performing the regular duties of the job in the usual manner and the usual place of employment?</td> \n" +
            "<td> <input type='radio' name='active' value='No'> No " +
            "<input type='radio' name='active' value='Yes'> Yes " + "</td></tr> \n";

        table += "<tr><td colspan='2'>Is the Proposed Insured a U.S. Citizen or a permanent resident?</td> \n" +
            "<td> <input type='radio' name='resident' value='No'> No " +
            "<input type='radio' name='resident' value='Yes'> Yes " + "</td></tr> \n";

        table += "</table> <br/>";

        return table;
    }  // *** THIS FUNCTION HAS BEEN DEPRECATED ***
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}