using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using DotNetDETs.Infrastructure;
using System.Collections.Generic;
using log4net;
using System;
using Newtonsoft.Json;
using System.Text;

namespace DotNetDETs.Controllers
{
    /*
     * DATABASE NOTIFY PROJECT DATA ENTRY TRIGGER
     * REDCAP PROJECT:  DatabaseNotify project
     * AUTHOR:          Paul Litwin
     * WHEN TRIGGERED:  When redcapSurvey form is saved and marked as complete. 
     * ACTION:          Performs two actions based on the cityField value:
     *                  1. Adds form to appropriate data access group (DAG) based on city.
     *                  2. Notifies appropriate contact at the site for that city.
     * NOTE:            DatabasedNotifyEmailsTestMode config setting of true diverts all emails to 
     *                  test recipient. Need to set to false when in production.
     * UPDATE HISTORY:  08/26/15 - Initial version.
     * 
    */
    public class DatabasedNotifyController : ApiController
    {
        private string token = WebConfigurationManager.AppSettings["DatabasedNotifyProjectToken"].ToString();
        private string projectName = WebConfigurationManager.AppSettings["DatabasedNotifyProjectName"].ToString();
        private string redcapPid = WebConfigurationManager.AppSettings["DatabasedNotifyPid"].ToString();
        private const string redcapSurvey = "the survey name";
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private bool isTestModeForEmails = Convert.ToBoolean(WebConfigurationManager.AppSettings["DatabasedNotifyEmailsTestMode"].ToString());

        // POST https://your-domain/api/DatabasedNotify
        public void Post([ModelBinder(typeof(RedcapDETModelBinderProvider))] RedcapDET redcapDet)
        {
            RedcapAccess rc = new RedcapAccess();

            Log.InfoFormat("DotNetDETs triggered for project {0}, instrument {1}", redcapDet.project_id, redcapDet.instrument);

            switch (redcapDet.instrument)
            {
                case redcapSurvey:
                    List<Dictionary<string, string>> recordList = new List<Dictionary<string, string>>();

                    string errorMessage = string.Empty;
                    string recordId = redcapDet.record;
                    // This is the field for this example that we are using to determine
                    // which DAG to put the survey in AND which site coordinator to send an email notification.
                    string cityField = "cityfield";

                    if (redcapDet.complete_flag == "2")
                    {
                        Log.InfoFormat("Using GetRedcapData to retrieve record {0}", recordId);
                        try
                        {
                            recordList = rc.GetRedcapData(token, recordId, $"subject_id,{cityField}", "", "", false, false, false, false, true, "", "");
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("GetRedcapData Exception: {0}", ex.Message);
                        }

                        CityInfo ci = GetContactForCity(recordList[0][cityField], recordId);

                        if (ci.Dag != string.Empty)
                        {
                            // Remove cityField so as not to resave the value
                            // Set data access group
                            recordList[0]["redcap_data_access_group"] = ci.Dag;
                            recordList[0][cityField] = string.Empty; // to prevent overwriting with same value
                            Log.InfoFormat("Attempting PostRedcapData");
                            try
                            {
                                rc.PostRedcapData(token, JsonConvert.SerializeObject(recordList), false, "MDY");
                            }
                            catch (Exception ex)
                            {
                                Log.ErrorFormat("PostRedcapData Exception: {0}", ex.Message);
                            }
                        }

                        // Now email the contact
                        try
                        {
                            Messaging messaging = new Messaging();

                            StringBuilder sbMsg = new StringBuilder();
                            sbMsg.Append($"<div style='font-family: Sans-Serif;'>");
                            sbMsg.Append($"Dear {ci.ContactName},");
                            sbMsg.Append(Environment.NewLine + Environment.NewLine + "<br /><br />");
                            sbMsg.Append($"A new {projectName} response for the {ci.City} site has been received. ");
                            sbMsg.Append(Environment.NewLine + "<br />");
                            sbMsg.Append("Click on the following link to view the record (you may be required to log into REDCap first):");
                            sbMsg.Append(Environment.NewLine + Environment.NewLine + "<br /><br />");
                            sbMsg.Append($"<a href='https://redcap-address-goes-here/DataEntry/index.php?pid={redcapPid}&id={recordId}&page={redcapSurvey}'>New {ci.City} survey response</a>");
                            sbMsg.Append(Environment.NewLine + Environment.NewLine + "<br /><br />");
                            sbMsg.Append($"Alternately, login to the {projectName} project and view the response for subject with identification number of {recordId}.");
                            sbMsg.Append(Environment.NewLine + Environment.NewLine + "<br /><br />");
                            sbMsg.Append("Regards,");
                            sbMsg.Append(Environment.NewLine + "<br />");
                            sbMsg.Append("name of sender");
                            sbMsg.Append(Environment.NewLine + "<br />");
                            sbMsg.Append("organization");
                            sbMsg.Append(Environment.NewLine + "<br />");
                            sbMsg.Append("title");
                            sbMsg.Append("</div>");

                            string mailBody = sbMsg.ToString();
                            Log.DebugFormat("Body of email: {0}", mailBody);

                            messaging.SendEmail("Name<email@domain.org>", ci.ContactEmail, projectName, mailBody, "Name<email@domain.org>", null, recordId);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("PostRedcapData Exception: {0}", ex.Message);
                        }
                    }
                    break;

                default:
                    break;

            }
            return;
        }

        private class CityInfo
        {
            public string ContactEmail { get; set; }
            public string ContactName { get; set; }
            public string City { get; set; }
            public string Dag { get; set; }
        }

        private CityInfo GetContactForCity(string cityValue, string recordId)
        {
            CityInfo ci = new CityInfo();

            // These city field values are just meant as an example.
            switch (cityValue)
            {
                case "1":
                    ci.ContactEmail = "site contact name<name@site.org>";
                    ci.ContactName = "site contact";
                    ci.City = "Atlanta";
                    ci.Dag = "atlanta";
                    break;
                case "2":
                    ci.ContactEmail = "site contact name<name@site.org>";
                    ci.ContactName = "site contact";
                    ci.City = "Boston";
                    ci.Dag = "boston";
                    break;
                case "3":
                    ci.ContactEmail = "site contact name<name@site.org>";
                    ci.ContactName = "site contact";
                    ci.City = "Los Angeles";
                    ci.Dag = "los_angeles";
                    break;
                default:
                    ci.ContactEmail = "Error Recipient<email@domain.org>";
                    ci.ContactName = "Error Recipient";
                    ci.City = "{city was not selected}";
                    ci.Dag = string.Empty;
                    Log.ErrorFormat("Unexpected city value of '{0}' for recordId {1}.", cityValue, recordId);
                    break;
            }

            if (isTestModeForEmails)
            {
                ci.ContactEmail = "Test Recipient<email@domain.org>";
                ci.ContactName = "Test Recipient";
            }

            return ci;
        }

    }
}
