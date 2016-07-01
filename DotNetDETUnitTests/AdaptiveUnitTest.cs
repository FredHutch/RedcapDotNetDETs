using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetDETs.Infrastructure;
using DotNetDETs.Controllers;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DotNetDETUnitTests
{
    [TestClass]
    public class PalsControllerTest
    {
        [TestInitialize]
        private void InitTest()
        {

        }

        [TestMethod]
        public void CanIRandomizeABunchOfSubjects()
        {
            // Need the token and uri for the REDCap project.
            // Could have put this in the app.config.
            const string token = "xxxxxxxxxxxxxxxxxxxxxxxx";
            const string uri = "https://redcap-domain/redcap/api/";

            int errorCounter = 0;

            /* Randomize 500 subjects by calling RandomizeASubject repeatedly.
                RandomizeASubject randomizes a subject by... 
                1. Inserting the prerequiste data into the REDCap project.
                2. Fabricating the DET form data.
                3. Calling the AdaptiveController code.
             */

            for (int i = 1; i < 501; i++)
            {
                HttpResponseMessage response = RandomizeASubject(token, uri, i);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    errorCounter++;
                }
            }

            // Assert
            Assert.AreEqual<int>(errorCounter, 0);
        }

        private static HttpResponseMessage RandomizeASubject(string token, string uri, int id)
        {
            List<Dictionary<string, string>> newList = new List<Dictionary<string, string>>();
            Dictionary<string, string> form1 = new Dictionary<string, string>();
            Dictionary<string, string> form2 = new Dictionary<string, string>();

            // These next set of variables will change per project
            const string form1Name = "Baseline";
            const string form2Name = "Other";
            const string eventName = "event1_arm_1";
            const string randomizationFormName = "randomization";
            const string randomizationTimeName = "pc_rnd_date";
            const string recordIdName = "subject_id";
            const string projectId = "206";

            string recordId = id.ToString();

            RedcapAccess rc = new RedcapAccess(uri);
            Random rnd = new Random(id + DateTime.Now.Millisecond);

            // Prepare random pre-randomization data to be sent to REDCap
            form1.Add(recordIdName, recordId);
            form1.Add("redcap_event_name", eventName);
            form1.Add(form1Name + "_complete", "2");
            form2.Add(recordIdName, recordId);
            form2.Add("redcap_event_name", eventName);
            form2.Add(form2Name + "_complete", "2");
            form2.Add(randomizationTimeName, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
            form2.Add(randomizationFormName + "_complete", "2");
            newList.Add(form1);
            newList.Add(form2);

            // Create new record with pre-randomization data
            var insertResponse = rc.PostRedcapData(token, JsonConvert.SerializeObject(newList), false, "MDY");

            // Now call the controller method
            // Arrange
            var controller = new AdaptiveController(token, uri);
            controller.Request = new HttpRequestMessage { Method = HttpMethod.Post };
            controller.Configuration = new HttpConfiguration();

            // Act
            RedcapDET det = new RedcapDET();
            det.redcap_data_access_group = "";
            det.redcap_event_name = eventName;
            det.instrument = randomizationFormName;
            det.project_id = projectId;
            det.complete_flag = "1";
            det.record = recordId;

            var response = controller.Post(det);
            return response;
        }
    }
}
