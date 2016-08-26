using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Web.Security;
using System.Collections;
using System.Net;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Data;
using System.Web.UI.HtmlControls;
using System.Text.RegularExpressions;
using System.Net.Mail;

public partial class SiteMaster : System.Web.UI.MasterPage
{
    protected void Page_PreInit(object sender, EventArgs e)
    {
    }
 

    public static class MyGlobals
    {
        public static string defaultAgency = "BenefitsDirect";
        //public static SqlConnection myConnection = new SqlConnection("user id=vusmain;" +
        //          "password=showmein$2013;server=localhost;" +
        //          "Trusted_Connection=no;" +
        //          "database=" + MyGlobals.defaultAgency + "; " +
        //          "connection timeout=30;" +
        //           "MultipleActiveResultSets=True");

        public static SqlConnection myConnection = new SqlConnection("user id=situsmain;" +
                                                                   "password=WhiteStar708;server=localhost;" +
                                                                   "Trusted_Connection=no;" +
                                                                   "database=" + defaultAgency + "; " +
                                                                   "connection timeout=30;" +
                                                                   "MultipleActiveResultSets=True");       

        //public static string connectionString = "user id=vusmain;" +
        //                                        "password=showmein$2013;server=localhost;" +
        //                                        "Trusted_Connection=no;" +
        //                                        "database=" + MyGlobals.defaultAgency + "; " +
        //                                        "connection timeout=30;" +
        //                                        "MultipleActiveResultSets=True";
        public static string connectionString = "user id=situsmain;" +
                                                "password=WhiteStar708;server=localhost;" +
                                                "Trusted_Connection=no;" +
                                                "database=" + defaultAgency + "; " +
                                                "connection timeout=30;" +
                                                "MultipleActiveResultSets=True";
        public static ListItem[] USStatesList = new ListItem[] { new ListItem("AK", "AK"), new ListItem("TX", "TX") };
    }


    public enum LogType
    {
        Transactions = 1,
        Beneficiaries = 2,
        Census = 3,
        Dependents = 4,
        EditLedger = 5
    }


    /*protected void Page_Init(object sender, EventArgs e)
    {
        if ((string)new ProfileCommon().GetProfile(HttpContext.Current.User.Identity.Name).GetPropertyValue("Agency") == "Administration")
        {
            if (listOfAgencies == null)
            {
                Session["currentAgency"] = "Test";
                //Response.Write("Creating...");
                listOfAgencies = new DropDownList();
                listOfAgencies.SelectedIndexChanged += new EventHandler(changeAgencies);
                listOfAgencies.AutoPostBack = true;

                if (MyGlobals.myConnection.State == ConnectionState.Closed)
                    MyGlobals.myConnection.Open();
                SqlDataReader myReader = new SqlCommand("SELECT * FROM sys.databases WHERE owner_sid = 0xF2461B63781B9E4984D3F2499657D009", SiteMaster.MyGlobals.myConnection).ExecuteReader();
                while (myReader.Read())
                {
                    listOfAgencies.Items.Add(new ListItem(myReader["name"].ToString(), myReader["name"].ToString()));
                }
                myReader.Close();
                MyGlobals.myConnection.Close();

                resetAllCA(listOfAgencies.SelectedValue);
                //listOfAgencies.SelectedIndex = 0;
                //Response.Write("Created");
                Response.Write("\n<br> " + Session["currentAgency"]);
            }
        }
        else
        {
            resetAllCA((string)new ProfileCommon().GetProfile(HttpContext.Current.User.Identity.Name).GetPropertyValue("Agency"));
        }
    } */

    private void resetAllCA(string agency)
    {
        Session["currentAgency"] = agency;
        Session["agency"] = agency;
        Response.Redirect(ResolveUrl("~"));
    }

    public static string EncodeTo64(string toEncode)
    {
        byte[] toEncodeAsBytes
              = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
        string returnValue
              = System.Convert.ToBase64String(toEncodeAsBytes);
        return returnValue;
    }

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public class WebPostRequest
    {
        WebRequest theRequest;
        HttpWebResponse theResponse;
        ArrayList theQueryData;
        string Parameters = "";

        public WebPostRequest(string url)
        {
            theRequest = WebRequest.Create(url);
            theRequest.Method = "POST";
            theQueryData = new ArrayList();
        }

        public void Add(string key, string value)
        {
            theQueryData.Add(String.Format("{0}={1}", key, HttpUtility.UrlEncode(value)));
        }

        public string GetResponse()
        {
            // Set the encoding type
            theRequest.ContentType = "application/x-www-form-urlencoded";

            // Build a string containing all the parameters
            Parameters = String.Join("&", (String[])theQueryData.ToArray(typeof(string)));
            theRequest.ContentLength = Parameters.Length;

            // We write the parameters into the request
            StreamWriter sw = new StreamWriter(theRequest.GetRequestStream());
            sw.Write(Parameters);
            sw.Close();

            //return Parameters;
            // Execute the query
            theResponse = (HttpWebResponse)theRequest.GetResponse();
            StreamReader sr = new StreamReader(theResponse.GetResponseStream());
            //using (StreamWriter swa = File.AppendText(@"c:\inetpub\wwwroot\SitusNetwork\EnrollApp\MyTest.txt"))
            //{
            //    swa.WriteLine(theResponse.ResponseUri);
            //}
            return sr.ReadToEnd();
        }

        public string GetParameters()
        {
            return Parameters;
        }
    }

    public static string toCurrency(string preFormat)
    {
        if (!String.IsNullOrWhiteSpace(preFormat))
        {
            if (preFormat.IndexOf(".") != -1)
            {
                string[] buffer = preFormat.Split('.');
                if (buffer[1].Length == 1)
                    return (String.Join(".", buffer) + "0");
                else if (buffer[1].Length == 2)
                    return String.Join(".", buffer);
                else if (buffer[1].Length > 2)
                {
                    try
                    {
                        buffer[1] = Math.Round(Convert.ToDouble("0." + buffer[1]), 2).ToString();
                        //HttpContext.Current.Response.Write(buffer[1] + "<br/>");
                        if (buffer[1].Contains("."))
                            buffer[1] = buffer[1].Split('.')[1];
                        else if (buffer[1] == "0")
                            buffer[1] = "00";
                        else if (buffer[1] == "1")
                        {
                            buffer[0] = (Convert.ToInt32(buffer[0]) + 1).ToString();
                            buffer[1] = "00";
                        }

                        if (buffer[1].Length == 1)
                            buffer[1] = buffer[1] + 0;
                    }
                    catch (FormatException e)
                    {
                        buffer[1] = buffer[1].Substring(0, 2);
                    }
                    return String.Join(".", buffer);
                }
            }
            else return (preFormat + ".00");
        }
        else return "0.00";
        return preFormat;
    }

    public static string toCurrency(string preFormat, bool addCommas)
    {
        if (!String.IsNullOrWhiteSpace(preFormat))
        {
            if (preFormat.IndexOf(".") != -1)
            {
                string[] buffer = preFormat.Split('.');

                if (addCommas)
                {
                    string reverse = null;
                    char[] charArray = buffer[0].ToCharArray();
                    Array.Reverse(charArray);
                    for (int i = 0; i < charArray.Length; i++)
                    {
                        if (i % 3 == 0 && i != 0)
                            reverse += ',';
                        reverse += charArray[i];
                    }
                    charArray = reverse.ToCharArray();
                    Array.Reverse(charArray);
                    buffer[0] = new string(charArray);
                }

                if (buffer[1].Length == 1)
                    return (String.Join(".", buffer) + "0");
                else if (buffer[1].Length == 2)
                    return String.Join(".", buffer);
                else if (buffer[1].Length > 2)
                {
                    string initBuffer = buffer[1];
                    try
                    {
                        buffer[1] = Math.Round(Convert.ToDouble("0." + buffer[1]), 2).ToString();
                        //HttpContext.Current.Response.Write(buffer[1]);
                        if (buffer[1].Contains("."))
                            buffer[1] = buffer[1].Split('.')[1];
                        else if (buffer[1] == "0")
                            buffer[1] = "00";
                        else if (buffer[1] == "1")
                        {
                            //HttpContext.Current.Response.Write(" :" + buffer[0].Replace(",", "") + ": ");
                            buffer[0] = (Convert.ToInt32(buffer[0].Replace(",", "")) + 1).ToString();
                            
                            if (addCommas)
                            {
                                string reverse = null;
                                char[] charArray = buffer[0].ToCharArray();
                                Array.Reverse(charArray);
                                for (int i = 0; i < charArray.Length; i++)
                                {
                                    if (i % 3 == 0 && i != 0)
                                        reverse += ',';
                                    reverse += charArray[i];
                                }
                                charArray = reverse.ToCharArray();
                                Array.Reverse(charArray);
                                buffer[0] = new string(charArray);
                            }

                            buffer[1] = "00";
                        }
                    }
                    catch (FormatException e)
                    {
                        buffer[1] = initBuffer.Substring(0, 2);
                    }
                    return String.Join(".", buffer);
                }
            }
            else
            {
                if (addCommas)
                {
                    string reverse = null;
                    char[] charArray = preFormat.ToCharArray();
                    Array.Reverse(charArray);
                    for (int i = 0; i < charArray.Length; i++)
                    {
                        if (i % 3 == 0 && i != 0)
                            reverse += ',';
                        reverse += charArray[i];
                    }
                    charArray = reverse.ToCharArray();
                    Array.Reverse(charArray);
                    preFormat = new string(charArray);
                }

                return (preFormat + ".00");
            }
        }
        else return "0.00";
        return preFormat;
    }


    

public static bool IsDigitsOnly(string str)
{
    foreach (char c in str)
    {
        if (c < '0' || c > '9')
            return false;
    }

    return true;
}

    public static string UppercaseFirst(string s)
    {
        // Check for empty string.
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        string[] words = s.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (words.Length > 1)
            {
                words[i] = UppercaseFirst(word);
            }
            else return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        }
        return String.Join(" ", words);
    }

    public static string getFolderPath(string name)
    {
        //return System.Web.HttpContext.Current.Server.MapPath("~/" + name);
        return System.Web.HttpContext.Current.Server.MapPath("/" + name);

    }

    public static string getBillingMode(string shortForm)
    {
        if (shortForm == "M")
            return "Monthly";
        if (System.Web.HttpContext.Current.Session["group"].ToString() == "wireless" || System.Web.HttpContext.Current.Session["group"].ToString() == "newtest")
           if(shortForm=="SM")
               return "Bi-Weekly";
        if (shortForm == "SM")
            return "Semi-Monthly";
        if (shortForm == "BW")
            return "Bi-Weekly";
        if (shortForm == "W")
            return "Weekly";
        if (shortForm == "9")
            return "9-thly";
        if (shortForm == "10")
            return "10-thly";
        if (shortForm == "SM")
            return "Semi-Monthly";
        else
            return shortForm;
    }

    public static string formatSSN(string preformat)
    {
        string ssn = preformat;
        if (preformat.Length == 8 && !preformat.Contains("-"))
            preformat = preformat.Insert(0, "0");

        if (preformat.Length == 10 && preformat.Contains("-"))
            preformat = preformat.Insert(0, "0");

        if (preformat.Contains("-"))
            ssn = preformat;
        else if (preformat.Length == 9)
        {
            ssn = preformat.Substring(0, 3) + "-" + preformat.Substring(3, 2) + "-" + preformat.Substring(5, 4);
        }
        return ssn;
    }

    public static bool IsValidSSN(string stringToCheck)
    {
        Regex ssnPattern = new Regex("^\\d{9}$|^\\d{3}-\\d{2}-\\d{4}$|^\\d{3}\\s\\d{2}\\s\\d{4}$");
        return ssnPattern.IsMatch(stringToCheck);
    }

    // Formats given date string by given format string. Returns empty string for bad date, invalid characters, or undetected format.
    // Becca 1-8-2015.
    public static string formatDate(string format, string date)
    {   // Detected formats for date string.
        string[] formats = { "MM/dd/yyyy", "M/d/yyyy", "MM/d/yyyy", "M/dd/yyyy", "M/d/yy", "MM/d/yy", "M/dd/yy", "MMddyyyy", "yyyyMMdd" };
        string formattedDate = "";
        DateTime dateObject;
        // Try to parse the date.
        if (DateTime.TryParseExact(date, formats, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out dateObject))
        {
            formattedDate = dateObject.ToString(format);
        }
        return formattedDate;
    }

    public List<List<string>> getListofDependents(bool addEmployeeData)
    {
        SqlConnection myConnection = SiteMaster.MyGlobals.myConnection;

        myConnection.Close();
        myConnection.Open();
        //*** GET DEPENDENTS DATA ***
        List<List<string>> Dependents = null;

        string name = (string)Session["group"];
        string EEID = (string)Session["empID"];
        string SQL = "";

        SqlDataReader myReader2 = null;
        
        string eSQL = "SELECT ID='" + EEID + "', fname,lname,ssn,birthdate,sex AS gender,type='Self' FROM [" + Session["currentAgency"] + "].[" + name + "].[Employees] WHERE [EmpID]=" + EEID;
        string dSQL = "SELECT ID, fname,lname,ssn,birthdate,gender,type FROM [" + Session["currentAgency"] + "].[" + name + "].[Dependents] WHERE [EmpID]=" + EEID;
        
        if (addEmployeeData) SQL = eSQL + " UNION ALL " + dSQL;
        else SQL = dSQL;

        SqlCommand myCommand = new SqlCommand(SQL, myConnection);
        myReader2 = myCommand.ExecuteReader();
        if (myReader2.HasRows)
        {
            Dependents = new List<List<string>>();
            while (myReader2.Read())
            {
                Dependents.Add(new List<string> {myReader2["ID"].ToString(),
                    myReader2["fname"].ToString() + " " + myReader2["lname"].ToString(),
                    myReader2["birthdate"].ToString(),
                    myReader2["ssn"].ToString(),
                    myReader2["gender"].ToString(),
                    myReader2["type"].ToString()});
            }
        }
        myConnection.Close();
        return Dependents;
    }

    public static List<List<string>> getListofDependents(bool addEmployeeData, string EEID)
    {
        List<List<string>> Dependents = null;
        string SQL = "";
        string name = (string)HttpContext.Current.Session["group"];
        string agency = (string)HttpContext.Current.Session["currentAgency"];
        try
        {
            SqlConnection currentConnection = new SqlConnection(MyGlobals.connectionString);
            bool isClosed = false;
            //HttpContext.Current.Response.Write(MyGlobals.myConnection.State.ToString());
            if (currentConnection.State == ConnectionState.Closed)
            {
                isClosed = true;
                currentConnection.Open();
            }

            if (!String.IsNullOrWhiteSpace(agency))
            {
                //*** GET DEPENDENTS DATA ***

                SqlDataReader myReader2 = null;

                string eSQL = "SELECT ID=0, fname,lname,ssn,birthdate,sex AS gender,type='Self' FROM [" + agency + "].[" + name + "].[Employees] WHERE [EmpID]=" + EEID;
                string dSQL = "SELECT ID, fname,lname,ssn,birthdate,gender,type FROM [" + agency + "].[" + name + "].[Dependents] WHERE [EmpID]=" + EEID;

                if (addEmployeeData) SQL = eSQL + " UNION ALL " + dSQL;
                else SQL = dSQL;

                //HttpContext.Current.Response.Write(":::" + HttpContext.Current.Session["currentAgency"] + ":::" + SQL);
                SqlCommand myCommand = new SqlCommand(SQL, currentConnection);
                myReader2 = myCommand.ExecuteReader();
                if (myReader2.HasRows)
                {
                    Dependents = new List<List<string>>();
                    while (myReader2.Read())
                    {
                        Dependents.Add(new List<string> {myReader2["ID"].ToString(), // index: 0
                    myReader2["fname"].ToString() + " " + myReader2["lname"].ToString(), // index: 1
                    myReader2["birthdate"].ToString(), // index: 2
                    myReader2["ssn"].ToString(), // index: 3
                    myReader2["gender"].ToString(), // index: 4
                    myReader2["type"].ToString()}); // index: 5
                    }
                }
            }
            if (isClosed)
                currentConnection.Close();
        }
        catch (Exception e)
        {
            HttpContext.Current.Response.Write(e);
        }
        return Dependents;
    }

    public static List<List<string>> getDependents(bool addEmployeeData, string EEID)
    {
        List<List<string>> Dependents = null;
        string SQL = "";
        string name = (string)HttpContext.Current.Session["group"];
        string agency = (string)HttpContext.Current.Session["currentAgency"];
        try
        {
            bool isClosed = false;
            if (MyGlobals.myConnection.State == ConnectionState.Closed)
            {
                isClosed = true;
                MyGlobals.myConnection.Open();
            }

            if (!String.IsNullOrWhiteSpace(agency))
            {
                //*** GET DEPENDENTS DATA ***

                SqlDataReader myReader2 = null;

                string eSQL = "SELECT ID=0, fname,lname,ssn,birthdate,sex AS gender,type='Self' FROM [" + agency + "].[" + name + "].[Employees] WHERE [EmpID]=" + EEID;
                string dSQL = "SELECT ID, fname,lname,ssn,birthdate,gender,type FROM [" + agency + "].[" + name + "].[Dependents] WHERE [EmpID]=" + EEID;

                if (addEmployeeData) SQL = eSQL + " UNION ALL " + dSQL;
                else SQL = dSQL;

                //HttpContext.Current.Response.Write(":::" + HttpContext.Current.Session["currentAgency"] + ":::" + SQL);
                SqlCommand myCommand = new SqlCommand(SQL, MyGlobals.myConnection);
                myReader2 = myCommand.ExecuteReader();
                if (myReader2.HasRows)
                {
                    Dependents = new List<List<string>>();
                    while (myReader2.Read())
                    {
                        Dependents.Add(new List<string> {myReader2["ID"].ToString(), // index: 0
                    myReader2["fname"].ToString(), // index: 1
                    myReader2["lname"].ToString(), // index: 2
                    myReader2["birthdate"].ToString(), // index: 3
                    myReader2["ssn"].ToString(), // index: 4
                    myReader2["gender"].ToString(), // index: 5
                    myReader2["type"].ToString()}); // index: 6
                    }
                }
            }
            if (isClosed)
                MyGlobals.myConnection.Close();
        }
        catch (Exception e)
        {
            HttpContext.Current.Response.Write(e);
        }
        return Dependents;
    }

    public static List<Dictionary<string, string>> getEEData(bool addDependentData, string EEID)
    {
        List<Dictionary<string, string>> CensusData = null;
        string SQL = "";
        string agency = (string)HttpContext.Current.Session["currentAgency"];
        string name = (string)HttpContext.Current.Session["group"];

        SqlConnection currentConnection = new SqlConnection(MyGlobals.connectionString);
        currentConnection.Open();
        try
        {

            if (!String.IsNullOrWhiteSpace(agency))
            {
                //*** GET EMPLOYEE DATA ***

                SqlDataReader myReader2 = null;
                
                string eSQL = "SELECT ID=0,[Address],[City],[State],[Zip],[EmpID],[fname],[lname],[birthdate],[ssn],[sex],[homep],[email],[salary],[app_status],'Self' AS [type],[location],[comments] \n" +
                            "FROM [" + agency + "].[" + name + "].[Employees] WHERE [empID]=" + EEID;
                string dSQL = "SELECT ID, NULL [Address], NULL [City], NULL [State], NULL [Zip],[EmpID],[fname],[lname],[birthdate],[ssn],[gender] AS [sex], NULL [homep], NULL [email], NULL [salary], NULL [app_status],[type], NULL [location], NULL [comments] \n" +
                            "FROM [" + agency + "].[" + name + "].[Dependents] WHERE [EmpID]=" + EEID;
                
                if (addDependentData) SQL = eSQL + " UNION ALL " + dSQL;
                else SQL = dSQL;

                //HttpContext.Current.Response.Write(":::" + HttpContext.Current.Session["currentAgency"] + ":::" + SQL);
                SqlCommand myCommand = new SqlCommand(SQL, currentConnection);
                myReader2 = myCommand.ExecuteReader();
                if (myReader2.HasRows)
                {
                    CensusData = new List<Dictionary<string, string>>();
                    while (myReader2.Read())
                    {
                        Dictionary<string, string> newPerson = new Dictionary<string, string> ();
                        newPerson["ID"] = myReader2["ID"].ToString();
                        newPerson["fname"] = myReader2["fname"].ToString();
                        newPerson["lname"] = myReader2["lname"].ToString(); // index: 1
                        newPerson["birthdate"] = myReader2["birthdate"].ToString(); // index: 2
                        newPerson["Address"] = myReader2["Address"].ToString() + "<br/> \n"  // index: 3
                                + myReader2["City"].ToString() + ", "
                                + myReader2["State"].ToString() + " "
                                + myReader2["Zip"].ToString();
                        newPerson["ssn"] = myReader2["ssn"].ToString(); // index: 4
                        newPerson["homep"] = myReader2["homep"].ToString(); // index: 5
                        newPerson["email"] = myReader2["email"].ToString(); // index: 6
                        newPerson["sex"] = myReader2["sex"].ToString(); // index: 7
                        newPerson["type"] = myReader2["type"].ToString(); // index: 8
                        newPerson["app_status"] = myReader2["app_status"].ToString(); // index: 9
                        newPerson["location"] = myReader2["location"].ToString(); // index: 10
                        newPerson["comments"] = myReader2["comments"].ToString(); // index: 11
                        newPerson["salary"] = myReader2["salary"].ToString(); // index: 12

                        if(myReader2.HasColumn("effective_date"))
                            newPerson["effective_date"] = myReader2["effective_date"].ToString(); // index: 13

                        if (myReader2.HasColumn("COMBI_effective_date"))
                            newPerson["COMBI_effective_date"] = myReader2["COMBI_effective_date"].ToString();  // index: 14

                        CensusData.Add(newPerson);
                    }
                }
                if (String.IsNullOrWhiteSpace(CensusData[0]["ssn"]))
                    throw new Exception("<script> alert(\"This employee's SSN has hot been set.\"); </script>");
            }
        }
        catch (Exception e)
        {
            currentConnection.Close();
            HttpContext.Current.Response.Write(e.Message);
        }
        currentConnection.Close();
        return CensusData;
    }

    public static int logEntry(Dictionary<string, string> logData)
    {
        SqlConnection currentConnection = new SqlConnection(MyGlobals.connectionString);
        currentConnection.Open();

        string SQL = "INSERT INTO [SITUSLogs].[" + logData["groupName"] + "].[MainLog] " +
            "VALUES ('" + logData["username"] + "','" + DateTime.Now.ToString() + "','" + logData["type"] + "','" + logData["EmpID"] + "','" + logData["reason"] + "','" + logData["details"] + "') ";
        //Response.Write(SQL);

        return new SqlCommand(SQL, currentConnection).ExecuteNonQuery();
    }

    public static int getDigitCount(int toCount)
    {
        return (int)Math.Floor(Math.Log10(toCount) + 1);
    }

    public static object getSingleSQLData(string SQLString)
    {
        SqlConnection currentConnection = new SqlConnection(MyGlobals.connectionString);
        //HttpContext.Current.Response.Write(MyGlobals.myConnection.State.ToString());
        currentConnection.Open();
        try
        {

            var data = new SqlCommand(SQLString, currentConnection).ExecuteScalar();
            //if (data.GetType().ToString() != "System.String")
            //    throw new Exception("DATAIS|" + data.GetType().ToString() + "|");

            if (data != null)
            {
                if (data.GetType() == typeof(System.Data.SqlClient.SqlException))
                {
                    currentConnection.Close();
                    throw new Exception("Command failed to retreive data.");
                }

                if (data.GetType() == typeof(System.DBNull))
                {
                    currentConnection.Close();
                    return "";
                }

                if (data.GetType().ToString() != "System.Exception" && String.IsNullOrWhiteSpace(data.ToString()))
                {
                    currentConnection.Close();
                    return "";
                }
            }
            else
            {
                currentConnection.Close();
                return "";
            }

            currentConnection.Close();
            return data;
        }
        catch (SqlException sqlError)
        {
            currentConnection.Close();
            return new Exception(sqlError.Message + "\n Command:" + SQLString + "\n Stack:" + sqlError.StackTrace);
        }
    }

    public static object getSingleSQLDataParameterized(string SQLString,string param, string paramValue)
    {
        SqlConnection currentConnection = new SqlConnection(MyGlobals.connectionString);
        //HttpContext.Current.Response.Write(MyGlobals.myConnection.State.ToString());
        currentConnection.Open();
        try
        {
            SqlCommand command = new SqlCommand(SQLString, currentConnection);
            command.Parameters.Add(new SqlParameter(param, paramValue));
            HttpContext.Current.Response.Write(command.CommandText);
            var data = command.ExecuteScalar();
            //if (data.GetType().ToString() != "System.String")
            //    throw new Exception("DATAIS|" + data.GetType().ToString() + "|");

            if (data != null)
            {
                if (data.GetType() == typeof(System.Data.SqlClient.SqlException))
                {
                    currentConnection.Close();
                    throw new Exception("Command failed to retreive data.");
                }

                if (data.GetType() == typeof(System.DBNull))
                {
                    currentConnection.Close();
                    return "";
                }

                if (data.GetType().ToString() != "System.Exception" && String.IsNullOrWhiteSpace(data.ToString()))
                {
                    currentConnection.Close();
                    return "";
                }
            }
            else
            {
                currentConnection.Close();
                return "";
            }

            currentConnection.Close();
            return data;
        }
        catch (SqlException sqlError)
        {
            currentConnection.Close();
            return new Exception(sqlError.Message + "\n Command:" + SQLString + "\n Stack:" + sqlError.StackTrace);
        }
    }

    public static int executeSQLString(string SQLString)
    {
        SqlConnection currentConnection = new SqlConnection(MyGlobals.connectionString);
        bool isClosed = false;
        if (currentConnection.State == ConnectionState.Closed)
        {
            isClosed = true;
            currentConnection.Open();
        }

        int execute = new SqlCommand(SQLString, currentConnection).ExecuteNonQuery();

        if (isClosed)
            currentConnection.Close();

        return execute;
    }

    public static SqlDataReader getSQLReader(string SQLString)
    {
        bool isClosed = false;
        if (MyGlobals.myConnection.State == ConnectionState.Closed)
        {
            isClosed = true;
            MyGlobals.myConnection.Open();
        }

        SqlDataReader data = new SqlCommand(SQLString, MyGlobals.myConnection).ExecuteReader();

        if(isClosed)
            MyGlobals.myConnection.Close();
        return data;
    }

    public static DataTable getSQLData(string SQLString)
    {
        bool isClosed = false;
        if (MyGlobals.myConnection.State == ConnectionState.Closed)
        {
            isClosed = true;
            MyGlobals.myConnection.Open();
        }

        SqlDataReader myReader = new SqlCommand(SQLString, MyGlobals.myConnection).ExecuteReader();
        DataTable data = new DataTable();
        data.Load(myReader);

        if (isClosed)
            MyGlobals.myConnection.Close();
        return data;
    }

    public static string createFileString(string fileName)
    {
        string rawData = fileName.Trim();
        return rawData.Replace(" ", "_").Replace(",", "").Replace("/", "-").Replace(@"\", "-").Replace("'", "").Replace("&", "AND");
    }

    public static bool SendEmail(string subject, string emailText, string email, SqlConnection myConnection)
    {
        bool success = false;
        try
        {
            // myConnection.Open();
            var rng = new Random(); var agentName = "Rohan";
            //var email = "rsureshkumar@ventureusenterprises.com";
            ////  Response.Write("\n SELECT  [acct_manager],[acct_mgr_email] FROM [" + Session["currentAgency"] + "].[general].[Groups] where [short]='" + Session["group"].ToString() + "'");

            //SqlDataReader myReader = new SqlCommand("SELECT  [acct_manager],[acct_mgr_email] FROM [" + agency + "].[general].[Groups] where [short]='" + group + "'", myConnection).ExecuteReader();
            //while (myReader.Read())
            //{
            //    agentName = myReader["acct_manager"].ToString();
            //    email = myReader["acct_mgr_email"].ToString();
            //    // Response.Write("Email: "+email + "AgentName: " + agentName);
            //}
            if (email != "")
            {
                var fromAddress = new MailAddress("info@ventureuservers.com", "SITUS Administration");
                //var fromAddress = new MailAddress("customerservice@tandsbenefits.com", agentName);
                var toAddress = new MailAddress(email, agentName);
                const string fromPassword = "showmeins$2011";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                var message = new MailMessage(fromAddress, toAddress);
                message.Subject = subject;
                message.Body = emailText;
                message.IsBodyHtml = true;
                smtp.Send(message);
                success = true;
            }

            //Response.Write("<div style='width:100%; height:100%; background:#0a0;'>Document has been sent!</div>");
            //Response.Write("<script language='javascript'>alert('An email has been sent to the address on file.'); window.location('Login.aspx');</script>");

            // myConnection.Close();
        }
        catch (Exception ex)
        {
            HttpContext.Current.Response.Write("<script language='javascript'>alert('Error: " + ex.Message + "')</script>");
        }
        finally
        {
            myConnection.Close();
        }
        return success;
    }

    public static bool SendEmail(string subject, string emailText, string group, string agency, string senderEmail, string sender, string receiverEmail,
       string receiver, string [] imageNames)
    {
        bool success = false;
        try
        {
           
            var fromAddress = new MailAddress(senderEmail, sender);
         
            var toAddress = new MailAddress(receiverEmail, receiver);
            const string fromPassword = "showmeins$2011";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
         //var fromWirelessAddress = new MailAddress(
            var message = new MailMessage(fromAddress, toAddress);
            
            
           // message.Sender = new MailAddress("Benefits@wlexpress.com","Benefits" );
          //  message.ReplyToList.Add(senderEmail);

            message.Subject = subject;
        
            message.IsBodyHtml = true;

            if (imageNames != null)
                message.AlternateViews.Add(getEmbeddedImages(imageNames, emailText));
            else
                message.Body = emailText;
           // HttpContext.Current.Response.Write("heello");
            smtp.Send(message);
            success = true;
           
        }
        catch (Exception ex)
        {
            HttpContext.Current.Response.Write("<script language='javascript'>alert('Error: " + ex.Message + "')</script>");
            success = false;
        }
        finally
        {
            SiteMaster.MyGlobals.myConnection.Close();
        }
        return success;
    }

    public static bool SendEmail(string subject, string emailText, string group, string agency, string senderEmail, string sender, string receiverEmail,
      string receiver, string[] imageNames, string actualSender, string actualSenderName)
    {
        bool success = false;
        try
        {

            var fromAddress = new MailAddress(senderEmail, sender);

            var toAddress = new MailAddress(receiverEmail, receiver);
            const string fromPassword = "showmeins$2011";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            //var fromWirelessAddress = new MailAddress(
            var message = new MailMessage(fromAddress, toAddress);


            message.Sender = new MailAddress(actualSender, actualSenderName);
            //  message.ReplyToList.Add(senderEmail);

            message.Subject = subject;

            message.IsBodyHtml = true;

            message.AlternateViews.Add(getEmbeddedImages(imageNames, emailText));
            // HttpContext.Current.Response.Write("heello");
            smtp.Send(message);
            success = true;

        }
        catch (Exception ex)
        {
            HttpContext.Current.Response.Write("<script language='javascript'>alert('Error: " + ex.Message + "')</script>");
            success = false;
        }
        finally
        {
            SiteMaster.MyGlobals.myConnection.Close();
        }
        return success;
    }


    private static AlternateView getEmbeddedImages(string [] imageNames,string emailText)
    {
        string filePath = SiteMaster.getFolderPath("Group_Images") +@"/"+ HttpContext.Current.Session["group"] + @"/email/";
        emailText += "<p>";
        List<LinkedResource> inlines = new List<LinkedResource>();
        if(imageNames!=null)
        foreach (string imageName in imageNames)
        {
            LinkedResource inline = new LinkedResource(filePath + imageName);
            inline.ContentId = Guid.NewGuid().ToString();
            emailText += @"<img src='cid:" + inline.ContentId + @"'/> &nbsp;";
             inlines.Add(inline);
        }
        emailText += "</p>";
        HttpContext.Current.Response.Write(emailText);
        AlternateView alternateView = AlternateView.CreateAlternateViewFromString(emailText, null, System.Net.Mime.MediaTypeNames.Text.Html);
        foreach(LinkedResource inline in inlines)
            alternateView.LinkedResources.Add(inline);
        return alternateView;
    }

    /// <summary>
    /// Self Enroll function to log items
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="message"></param>
    public static void TextFileLog(string filePath,string message)
    {
        using (StreamWriter streamWriter = new StreamWriter(filePath, true))
        {

            streamWriter.WriteLine(message);

            streamWriter.Close();

        }

    }

}

public static class DataReaderCheck
{
    public static bool HasColumn(this IDataRecord dr, string columnName)
    {
        for (int i = 0; i < dr.FieldCount; i++)
        {
            if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }
        return false;
    }
}

public static class DateTimeExtensions
{
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = dt.DayOfWeek - startOfWeek;
        if (diff < 0)
        {
            diff += 7;
        }

        return dt.AddDays(-1 * diff).Date;
    }

   
}

