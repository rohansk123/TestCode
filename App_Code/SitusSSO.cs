using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// Summary description for SitusSSO
/// </summary>
public class SitusSSO
{
    Dictionary<string, string> dataFields = null;
    string BridgeURIEndpoint = null;

    public SitusSSO()
    {
        dataFields = new Dictionary<string, string>();
        //if (EndpointRadioButtonList.SelectedValue == "test")
        BridgeURIEndpoint = "http://uat-b2bg.cnoinc.com/B2BGExternalWeb/B2BGInboundServlet";
        //else
        //    BridgeURIEndpoint = "https://live-b2bezwork.cnoinc.com/B2BGExternalWeb/B2BGInboundServlet";
    }

    public SitusSSO(string URL)
    {
        dataFields = new Dictionary<string, string>();

        BridgeURIEndpoint = URL;
    }

    public void LaunchSAMLRequest(object sender, EventArgs e, Dictionary<string, string> SSOData)
    {
        dataFields = SSOData;
        //Fills the fields from the page

        // Build the XML for the request
        //String SOAPRequestXML = BuildXML(dataFields);
        String SOAPRequestXML = File.ReadAllText(SiteMaster.getFolderPath("Documents") + @"\" + SSOData["groupName"] + @"\New_VentureUSTest12-1.xml");

        File.WriteAllText(SiteMaster.getFolderPath("Documents") + @"\" + SSOData["groupName"] + @"\LatestSSORequest.xml", SOAPRequestXML);
        //throw new Exception(SOAPRequestXML);

        // Make the SOAP request from the enpoint.  Receive the response
        HttpWebResponse myWebResponse = MakeRequestToBridge(SOAPRequestXML);

        //Processes the response
        ProcessResponseFromBridge(myWebResponse);

    }

    private string BuildXML(Dictionary<string, string> inputData)
    {

        //xmllibrary myXML = new xmllibrary();
        //return myXML.testtrans;

        string XMLFilePath = SiteMaster.getFolderPath("Documents") + @"\SSOData\";

        string beginxml = File.ReadAllText(XMLFilePath + "beginxml.txt");
        string employeeguidxml = File.ReadAllText(XMLFilePath + "employeeguidxml.txt");
        string employeexml = File.ReadAllText(XMLFilePath + "employeexml.txt");
        string childxml = File.ReadAllText(XMLFilePath + "childxml.txt");
        string spousexml = File.ReadAllText(XMLFilePath + "spousexml.txt");
        string endxml = File.ReadAllText(XMLFilePath + "endxml.txt");

        //Start by adding header/common xml and filling with data
        beginxml = beginxml.Replace("{DateStamp}", DateTime.Today.ToString("yyyy-MM-dd"));
        beginxml = beginxml.Replace("{TimeStamp}", DateTime.Now.ToString("yyyy-MM-dd"));
        beginxml = beginxml.Replace("{SystemID}", "CoreBenefits");
        beginxml = beginxml.Replace("{MessageID}", "7ab83432-286c-471b-9993-63f5a181e838");
        beginxml = beginxml.Replace("{USERID}", "EzApp CB Session");
        beginxml = beginxml.Replace("{UserLoginName}", "VentureUS");
        beginxml = beginxml.Replace("{UserPassword}", "V3ntureU$");
        beginxml = beginxml.Replace("{BridgeURIEndpoint}", BridgeURIEndpoint);
        beginxml = beginxml.Replace("{CarrierCode}", "001");
        beginxml = beginxml.Replace("{AgentID}", "CS607");
        beginxml = beginxml.Replace("{EmployerGUID}", Guid.NewGuid().ToString());
        beginxml = beginxml.Replace("{LocationGUID}", Guid.NewGuid().ToString());
        string allxml = beginxml;

        //next add employee info if employeeGUID does NOT exist
        if (inputData.ContainsKey("EmpID"))
        {
            employeeguidxml = employeeguidxml.Replace("{EmployeeGUID}", inputData["appID"]);
            allxml += employeeguidxml;
        }
        else
        {
            employeexml = employeexml.Replace("{FirstName}", inputData["first"]);
            employeexml = employeexml.Replace("{LastName}", inputData["last"]);
            employeexml = employeexml.Replace("{DOB}", SiteMaster.formatDate(inputData["birthdate"], "yyyy-MM-dd"));
            employeexml = employeexml.Replace("{SSN}", inputData["ssn"]);
            //throw new Exception(inputData["birthD"]+ "," + inputData["effective_date"]);
            employeexml = employeexml.Replace("{Age}", SitusEnrollmentPage.ageAtEffectiveDate(inputData["birthdate"], inputData["effective_date"]).ToString());
            employeexml = employeexml.Replace("{Gender}", inputData["sex"]);
            allxml += employeexml;
        }

        List<Dictionary<string, string>> EEData = SiteMaster.getEEData(true, inputData["appID"]);

        foreach (Dictionary<string, string> Person in EEData)
        {
            //next add spouse info if firstname is present 
            if (Person["type"] == "Spouse")
            {
                spousexml = spousexml.Replace("{FirstName}", Person["fname"]);
                spousexml = spousexml.Replace("{LastName}", Person["lname"]);
                spousexml = spousexml.Replace("{DOB}", SiteMaster.formatDate(Person["birthdate"], "yyyy-MM-dd"));
                spousexml = spousexml.Replace("{SSN}", Person["ssn"]);
                spousexml = spousexml.Replace("{Age}", SitusEnrollmentPage.ageAtEffectiveDate(Person["birthdate"], inputData["effective_date"]).ToString());
                spousexml = spousexml.Replace("{Gender}", Person["sex"]);
                allxml += spousexml;
            }

            //next add child info info if firstname is present 
            if (Person["type"] == "Spouse")
            {
                childxml = childxml.Replace("{FirstName}", Person["fname"]);
                childxml = childxml.Replace("{LastName}", Person["lname"]);
                childxml = childxml.Replace("{DOB}", SiteMaster.formatDate(Person["birthdate"], "yyyy-MM-dd"));
                childxml = childxml.Replace("{SSN}", Person["ssn"]);
                childxml = childxml.Replace("{Age}", SitusEnrollmentPage.ageAtEffectiveDate(Person["birthdate"], inputData["effective_date"]).ToString());
                childxml = childxml.Replace("{Gender}", Person["sex"]);
                allxml += childxml;
                //(for demo purposes, only one child is assumed)
            }
        }


        //Add ending xml
        allxml += endxml;

        //RequestLiteral.Text = allxml;

        return allxml;

    }

    private HttpWebResponse MakeRequestToBridge(string SOAPRequestXML)
    {
        //Build the HTTP Request
        UTF8Encoding encoding = new UTF8Encoding();
        HttpWebRequest HTTPReq = (HttpWebRequest)WebRequest.Create(BridgeURIEndpoint);
        HTTPReq.Method = "POST";
        HTTPReq.ContentType = "application/soap+xml; charset=utf-8";



        //Build the SOAP
        byte[] SOAPRequestByte = encoding.GetBytes(SOAPRequestXML);
        HTTPReq.ContentLength = SOAPRequestByte.Length;

        //Build the stream
        Stream HTTPStream = null;
        HttpWebResponse HTTPResp = null;
        HTTPStream = HTTPReq.GetRequestStream();
        HTTPStream.Write(SOAPRequestByte, 0, SOAPRequestByte.Length);

        // Make the SOAP call to the endpoint.  Get the Response
        HTTPResp = (HttpWebResponse)HTTPReq.GetResponse();

        HTTPStream.Close();

        if (HTTPResp.StatusCode == HttpStatusCode.OK)
            return HTTPResp;
        else
        {
            String notOk = "Error in Response, Response Code: " + HTTPResp.StatusCode;
            HttpContext.Current.Response.Write(notOk);
            return null;
        }

    }

    private void ProcessResponseFromBridge(HttpWebResponse HTTPResp)
    {

        byte[] buffer = new byte[500000];  //note that Theme CNO servers do not include length so we put in a max.
        Stream RespStream = HTTPResp.GetResponseStream();
        RespStream.Read(buffer, 0, 500000);

        //need to log something?
        //File.WriteAllText(Server.MapPath("~/myfile.xml"), Encoding.ASCII.GetString(buffer).Trim(new char[] { (char)0x00 }));

        //output XML to page
        HttpContext.Current.Response.Write(UTF8Encoding.UTF8.GetString(buffer));


        // load the buffer into an xml document
        //*please note that the trim command is needed because there is no contentlength property given in the response
        //*therefore we create a larger than necessary buffer and trim the trailing nulls.
        XDocument Resp = XDocument.Parse(Encoding.ASCII.GetString(buffer).Trim(new char[] { '\0' }));
        RespStream.Close();

        // get the URL node from the xml document
        XElement UserSessionKey_URL = Resp.Descendants().SingleOrDefault(p => p.Name.LocalName == "UserSessionKey");
        XElement ResultCode = Resp.Descendants().SingleOrDefault(p => p.Name.LocalName == "ResultCode");



        if (UserSessionKey_URL != null && ResultCode != null)
        {
            // get the url from the XElement and clean
            String TargetURL = UserSessionKey_URL.Value.Replace("&amp;", "&");
            String RespResultCode = ResultCode.Value;

            //if the result code equals success then redirect
            if (RespResultCode == "Success")
                HttpContext.Current.Response.Redirect(TargetURL);
            else
                HttpContext.Current.Response.Write("Enpoint was reached, but response indicates a failure:::: Result Code: " + RespResultCode);

        }
        else
        {
            HttpContext.Current.Response.Write("Enpoint was reached, but the sessionkeyURL or result code was not present::: ");

        }

    }

}