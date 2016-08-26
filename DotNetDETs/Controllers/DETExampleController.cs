using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using DotNetDETs.Infrastructure;
using System.Collections.Generic;
using log4net;
using System;
using Newtonsoft.Json;

namespace DotNetDETs.Controllers
{
    /*
     * EXAMPLE TEMPLATE DATA ENTRY TRIGGER
     * REDCAP PROJECT:  none
     * AUTHOR:          Paul Litwin
     * WHEN TRIGGERED:  Nothing. 
     * ACTION:          Nothing.
     * UPDATE HISTORY:  08/26/15 - Added this comment header.
     * 
    */
    public class DETExampleController : ApiController
    {
        private string token = WebConfigurationManager.AppSettings["DETExampleProjectToken"].ToString();
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // POST https://your-domain/api/DETExampleProject
        public void Post([ModelBinder(typeof(RedcapDETModelBinderProvider))] RedcapDET redcapDet)
        {
            RedcapAccess rc = new RedcapAccess();

            Log.InfoFormat("DotNetDETs triggered for project {0}, instrument {1}", redcapDet.project_id, redcapDet.instrument);

            switch (redcapDet.instrument)
            {
                case "form-name":
                    List<Dictionary<string,string>> recordList = new List<Dictionary<string,string>>();

                    string errorMessage = string.Empty;
                    string recordId = redcapDet.record;

                    Log.InfoFormat("Using GetRedcapData to retrieve record {0}", recordId);
                    try
                    {
                        recordList = rc.GetRedcapData(token, recordId, "", "", "", false, false, false, false, true, "", "");
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("GetRedcapData Exception: {0}", ex.Message);
                    }

                    // Some logic to evaluate. Made up for example.
                    if (recordList[0]["field1"].ToString() == "1")
                    {
                        // Set some field values. This is all made up for the sake of the example.
                        recordList[0]["field2"] = "foo";
                        recordList[0]["field3"] = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                        recordList[0]["form-name_complete"] = "2";

                        Log.InfoFormat("Attempting PostRedcapData");
                        try
                        { 
                            rc.PostRedcapData(token, JsonConvert.SerializeObject(recordList), true, "MDY");
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
    }
}
