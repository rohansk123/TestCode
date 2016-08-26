using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DebenuPDFLibraryDLL0913;
using System.IO;

/// <summary>
/// Summary description for SitusPDFCreator
/// </summary>
public class SitusPDFCreator : PDFLibrary
{
    string errorMsgs = "";
    //HttpResponse
    // public static string DLLLocation = SiteMaster.getFolderPath("Bin") + @"\DebenuPDFLibraryDLL0913.dll";
    public static string DLLLocation = SiteMaster.getFolderPath("Bin") + @"\DebenuPDFLibrary64DLL0913.dll";
    //public static string DLLLocation = @"C:\inetpub\wwwroot\TandSB\App_Code\DebenuPDFLibrary64DLL0913.dll";

    public int lineCount = 0;



    public SitusPDFCreator(string PDFLocation)
        : base(DLLLocation)
    {

        if (this.LibraryLoaded())
        {
            // Create an instance of the class and give it the path to the DLL
            this.UnlockKey("jw98e6du3q76so9t48dc7ht4y");
            //UnHttpContext.Current.Response.Write("unlock key: ");
            if (this.Unlocked() == 0)
                throw new Exception("License unlock failed.  Please update your product key.");
        }
        else
        {
            throw new Exception("Could not locate the Debenu PDF Library DLL, please check the path: " + DLLLocation + " " + PDFLocation);
        }

        //HttpContext.Current.Response.Write(" :: " + PDFLocation + " :: ");
        //HttpContext.Current.Response.Write( this.LoadFromFile(PDFLocation, ""));
        this.LoadFromFile(PDFLocation, "");
    }


    public SitusPDFCreator()
        : base(DLLLocation)
    {
        if (this.LibraryLoaded())
        {
            //HttpResponse.Response.Write(DLLLocation);
            // Create an instance of the class and give it the path to the DLL
            this.UnlockKey("jw98e6du3q76so9t48dc7ht4y");

            if (this.Unlocked() == 0)
                throw new Exception("License unlock failed.  Please update your product key.");
        }
        else
        {

            throw new Exception("Could not locate the Debenu PDF Library DLL, please check the path");
        }

        if (this.Unlocked() == 1)
        {
            SetNeedAppearances(0);
            SetOrigin(1);
        }
        else
        {
            throw new Exception("License unlock failed.  Please update your product key.");
        }
    }

    public int SetFormField(string fieldName, string value)
    {
        try
        {
            //if (value == "Yes" || value == "Male")
            //{
            //    return this.SetFormFieldValueByTitle(fieldName, "X");
            //}
            //else if (value == "No" || value == "Female")
            //{
            //    return this.SetFormFieldValueByTitle(fieldName + "_N", "X");
            //}

            return this.SetFormFieldValueByTitle(fieldName, value);
        }
        catch (Exception error)
        {
            throw new Exception("Value could not be set for form field " + fieldName + ".");
        }

    }

    public int insertLine(double X, double Y, string value)
    {
        try
        {
            return this.DrawText(X, Y, value);
        }
        catch (Exception error)
        {
            throw new Exception("Data could not be inserted at:: X:" + X + ",  Y:" + Y + ".");
        }
    }

    public int insertHTMLLine(double X, double Y, double width, string value)
    {
        try
        {
            return this.DrawHTMLText(X, Y, width, value);
        }
        catch (Exception error)
        {
            throw new Exception("Data could not be inserted at:: X:" + X + ",  Y:" + Y + ".");
        }
    }

    public void checkForNewPage()
    {
        string totalText = this.GetPageText(3);
        int numLines = this.GetHTMLTextLineCount(500, this.GetPageText(3));
        if (numLines >= 80)
        {
            int n = this.NewPage();
            this.SelectPage(n);
            lineCount = 0;
        }
    }

    public void resetPageCount()
    {
        this.SelectPage(this.PageCount());
        lineCount = 0;
    }

    public int FlattenAndSave(string filelocation)
    {
        int total = this.FormFieldCount();
        while (total > 0)
        {
            string fieldName = GetFormFieldTitle(total);
            if (!fieldName.Contains("EE_Sign") && !fieldName.Contains("Agent_Sign"))
            {
                //HttpContext.Current.Response.Write(GetFormFieldTitle(total) + "this NOT is it. <BR/>");
                this.UpdateAndFlattenFormField(total);
                total--;
            }
            else
            {
                //HttpContext.Current.Response.Write(GetFormFieldTitle(total) + "this is it: " + total + " <BR/>");
                total--;
            }
        }

        int returnValue = this.SaveToFile(filelocation);

        this.RemoveDocument(this.SelectedDocument());

        return returnValue;

    }

    public static int SignApplication(string filelocation, string name, string agentName)
    {
        SitusPDFCreator signPDF = new SitusPDFCreator(filelocation);
        HttpContext.Current.Response.Write("<p>First: " + signPDF.LoadFromFile(filelocation, "") + ":: " + filelocation + "</p> ");
        int returnValue = 0;
        try
        {
            string signatureText = name + " [Electronic Signature]";
            if (!String.IsNullOrWhiteSpace((string)HttpContext.Current.Session["signatureText"]))
                signatureText = (string)HttpContext.Current.Session["signatureText"];

            //*** FIND, AND SIGN FIRST EMPLOYEE SIGNATURE FIELD ***
            int fieldID = signPDF.FindFormFieldByTitle("EE_Sign");
            HttpContext.Current.Response.Write(name + "=1=" + fieldID + "First EE signature. <BR/>");

            if (fieldID != 0)
            {
                signPDF.SetFormFieldValue(fieldID, signatureText);
                signPDF.UpdateAndFlattenFormField(fieldID);
            }
            else
                return returnValue; // RETURN IF NO EMPLOYEE SIGNATURE FIELD FOUND

            //*** FIND, AND SIGN FIRST AGENT SIGNATURE FIELD ***
            fieldID = signPDF.FindFormFieldByTitle("Agent_Sign");
            HttpContext.Current.Response.Write(agentName + "=A=" + fieldID + " First Agent signature. <BR/>");

            if (fieldID != 0)
            {
                signPDF.SetFormFieldValue(fieldID, agentName + " [Electronic Signature]");
                signPDF.UpdateAndFlattenFormField(fieldID);
            }

            //*** SEARCH FOR ADDITIONAL EMPLOYEE SIGNATURE FIELDS ***
            int signCount = 2;

            fieldID = signPDF.FindFormFieldByTitle("EE_Sign_" + signCount++);



            while (fieldID != 0)
            {
                if (signCount == 10)
                    throw new Exception(signCount.ToString());
                HttpContext.Current.Response.Write(name + "=" + signCount + "=" + fieldID + " next EE signature. <BR/>");
                signPDF.SetFormFieldValue(fieldID, signatureText);
                signPDF.UpdateAndFlattenFormField(fieldID);
                fieldID = signPDF.FindFormFieldByTitle("EE_Sign_" + signCount++);
            }

            //*** SEARCH FOR ADDITIONAL AGENT SIGNATURE FIELDS ***
            signCount = 2;
            fieldID = signPDF.FindFormFieldByTitle("Agent_Sign_" + signCount++);



            while (fieldID != 0)
            {
                if (signCount == 10)
                    throw new Exception(signCount.ToString());
                HttpContext.Current.Response.Write(agentName + "=A" + signCount + "=" + fieldID + " next Agent signature. <BR/>");
                signPDF.SetFormFieldValue(fieldID, agentName + " [Electronic Signature]");
                signPDF.UpdateAndFlattenFormField(fieldID);
                fieldID = signPDF.FindFormFieldByTitle("Agent_Sign_" + signCount++);

            }
            //filelocation = @"C:\inetpub\wwwroot\SitusNetwrk\" + filelocation.Substring(filelocation.LastIndexOf('/') + 1);
            //if (File.Exists(filelocation))
            //    File.Delete(filelocation);
            returnValue = signPDF.SaveToFile(filelocation);

            HttpContext.Current.Response.Write("file--" + filelocation + ":" + name + ":" + agentName + ":: " + returnValue + "<br/>");
            if (returnValue == 0)
                throw new Exception(returnValue.ToString() + " for:" + filelocation);
            return returnValue;
        }
        catch (Exception error)
        {
            throw new Exception("This application could not be signed." + error);
        }
    }

}