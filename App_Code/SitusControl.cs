using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for SitusControl
/// </summary>
public class SitusControl : System.Web.UI.UserControl
{
    protected List<Dictionary<string, string>> EEData = null;

    //need access to EEData in ConfirmationStatement.aspx and hence added the property below :-
    public List<Dictionary<string, string>> EED { get { return EEData; } set { EEData = value; } }

}