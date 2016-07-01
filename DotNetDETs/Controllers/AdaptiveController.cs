using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using DotNetDETs.Infrastructure;
using System.Collections.Generic;
using log4net;
using System;
using Newtonsoft.Json;
using System.Net.Http;

namespace DotNetDETs.Controllers
{
    // This REDCap DET implements Adaptive Randomization per Smoak and Lin
    // from http://www2.sas.com/proceedings/sugi26/p242-26.pdf
    // One difference is there is no run-in of simple randomization as mentioned
    // in the paper. Instead, only the first assignment for each covariate group is
    // randomly assigned using simple randomization. Thereafter, all subjects in that 
    // group are randomized using adaptive randomization.

    public class AdaptiveController : ApiController
    {
        private string token;
        private string uri;
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AdaptiveController()
        {
            // Normal invocation which pulls token out of web.config.
            this.token = WebConfigurationManager.AppSettings["PalsToken"].ToString();
        }

        public AdaptiveController(string token, string uri)
        {
            // When called from a unit test, need to pass the URI and token since
            // web.config settings will not be available.
            this.token = token;
            this.uri = uri;
        }

        // POST api/pals
        public HttpResponseMessage Post([ModelBinder(typeof(RedcapDETModelBinderProvider))] RedcapDET redcapDet)
        {
            RedcapAccess rc;
            // This handles calling of RedcapAccess from a unit test where
            // the URI needs to be passed to the contructor.
            // Perhaps there is a more elegant way to do this but...
            if (string.IsNullOrEmpty(this.uri))
                rc = new RedcapAccess();
            else
                rc = new RedcapAccess(uri);

            HttpResponseMessage response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            Log.InfoFormat("DotNetDETs triggered for project {0}, instrument {1}", redcapDet.project_id, redcapDet.instrument);

            switch (redcapDet.instrument)
            {
                case "randomization":

                    // Assumes two covariates
                    List<Dictionary<string, string>> triggerList = new List<Dictionary<string,string>>();
                    List<Dictionary<string, string>> cov1List = new List<Dictionary<string, string>>();
                    List<Dictionary<string, string>> cov2List = new List<Dictionary<string, string>>();
                    List<Dictionary<string, string>> returnList = new List<Dictionary<string, string>>();
                    Dictionary<string, string> returnRecord = new Dictionary<string, string>();

                    // These next set of variables will change per project
                    const string cov1FieldName = "pc_rnd_bmi_cat";
                    const string cov2FieldName = "pc_rnd_age_cat";
                    const string assignmentFieldName = "pc_rnd_assignment";
                    const string assignmentDateFieldName = "pc_rnd_assignment_date";
                    const string eventName = "clinic_visit_1_arm_1";
                    const string formName = "randomization";
                    const string recordIdFieldName = "subject_id";
                    const string eligibleFieldName = "pc_rnd_eligible";

                    // Array of weights; size of array must correspond to number of arms.
                    // Here we have two arms with equal weights of 1 each.
                    double[] weights = new double[] { 1, 1 };

                    string errorMessage = string.Empty;
                    string triggerRecord = redcapDet.record;
                    string cov1Cat = string.Empty;
                    string cov2Cat = string.Empty;
                    string cov1CatFilter = string.Empty;
                    string cov2CatFilter = string.Empty;

                    // New randomization assignment
                    // Assumption: first arm = 0, second arm = 1, nth arm = n-1, etc.
                    int newAssignment;

                    Log.InfoFormat("Record {0}", triggerRecord);
                    Log.DebugFormat("Using GetRedcapData to retrieve record {0}", triggerRecord);
                    try
                    {
                        triggerList = rc.GetRedcapData(token, triggerRecord, "", eventName, formName, false, false, false, false, true, "", "");
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("GetRedcapData Exception: {0}", ex.Message);
                    }

                    List<List<Dictionary<string, string>>> covLists = new List<List<Dictionary<string, string>>>();

                    // *** This next section of code will repeat for each covariate ****

                    // Create filter string and grab existing randomized records for cov1 group
                    // *** You will need to adjust if more than 2 arms and/or arm values are other than 0 and 1. ***
                    cov1Cat = triggerList[0][cov1FieldName].ToString();
                    cov1CatFilter = string.Format("[{0}]='{1}' and ([{2}]='{3}' or [{2}]='{4}')", cov1FieldName, cov1Cat, assignmentFieldName, 0, 1);

                    Log.DebugFormat("Using GetRedcapData to retrieve record(s) using filter '{0}'", cov1CatFilter);
                    try
                    {
                        cov1List = rc.GetRedcapData(token, "", assignmentFieldName, eventName, formName, false, false, false, false, true, cov1CatFilter, "");
                        covLists.Add(cov1List);
                   }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat(string.Format("GetRedcapData Exception: {0}", ex.Message));
                    }

                    // Prevent randomizing an ineligble subject
                    if (string.IsNullOrEmpty(triggerList[0][eligibleFieldName].ToString()) ||
                        !(triggerList[0][eligibleFieldName].ToString()=="1") )
                    {
                        Log.InfoFormat("Exiting because pt {0} not eligible to be randomized.", triggerRecord);
                        return response;
                    }

                    // Prevent re-randomizing a subject
                    if (!string.IsNullOrEmpty(triggerList[0][assignmentFieldName].ToString()))
                    {
                        Log.InfoFormat("Exiting because pt {0} already randomized.", triggerRecord);
                        return response;
                    }

                    Log.InfoFormat("{1} existing rows with '{0}.'", cov1CatFilter, cov1List.Count);

                    // Create filter string and grab existing randomized records for cov2 group
                    // *** You will need to adjust if more than 2 arms and/or arm values are other than 0 and 1. ***
                    cov2Cat = triggerList[0][cov2FieldName].ToString();
                    cov2CatFilter = string.Format("[{0}]='{1}' and ([{2}]='{3}' or [{2}]='{4}')", cov2FieldName, cov2Cat, assignmentFieldName, 0, 1);

                    Log.DebugFormat("Using GetRedcapData to retrieve record(s) using filter '{0}'", cov2CatFilter);
                    try
                    {
                        cov2List = rc.GetRedcapData(token, "", assignmentFieldName, eventName, formName, false, false, false, false, true, cov2CatFilter, "");
                        covLists.Add(cov2List);
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("GetRedcapData Exception: {0}", ex.Message);
                    }

                    Log.InfoFormat("{1} existing rows with '{0}'", cov2CatFilter, cov2List.Count);

                    // Use Adaptive randomization when at least one other subject with 
                    // this combination of covariates.
                    // *** If more than 2 covariates, this condition will need to be changed. ***
                    if (cov1List.Count > 0 && cov2List.Count > 0)
                    {
                        // Get adaptive randomization assignment
                        Log.InfoFormat("Need to use adaptive randomization for subject {0}.", triggerRecord);
                        newAssignment = GetAdaptiveAssignment(covLists, assignmentFieldName, weights);
                        Log.InfoFormat("Adaptively assigned participant {0} to arm {1}.", triggerRecord, newAssignment);
                    }
                    else
                    {
                        // Special case of no previous records for this combination of covariates
                        Random rnd = new Random(int.Parse(triggerRecord) + DateTime.Now.Millisecond);
                        Log.Info("Zero prior subjects with this set of covariates.");
                        newAssignment = rnd.Next(0, weights.GetLength(0));
                        Log.InfoFormat("Randomly assigned participant {0} to arm {1}.", triggerRecord,  newAssignment);
                    }

                    // Prepare randomization record to be sent to REDCap
                    returnRecord.Add(recordIdFieldName, triggerRecord);
                    returnRecord.Add("redcap_event_name", eventName);
                    returnRecord.Add(assignmentFieldName, newAssignment.ToString());
                    returnRecord.Add(assignmentDateFieldName, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
                    returnList.Add(returnRecord);

                    Log.DebugFormat("Attempting PostRedcapData");
                    try
                    {
                        rc.PostRedcapData(token, JsonConvert.SerializeObject(returnList), false, "MDY");
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("PostRedcapData Exception: {0}", ex.Message);
                    }

                    break;
                default:
                    break;
            }

            return response;
        }

        private int GetAdaptiveAssignment(List<List<Dictionary<string, string>>> covLists,
                                            string assignmentFieldName,
                                            double[] weights)
        {
            int numberCovariates = covLists.Count;
            int numberArms = weights.GetLength(0);
            // There will always be one scenario per arm but it's  
            // easier to follow keeping these values distinct
            int numberScenarios = numberArms;

            double[,,] obsFreq = new double[numberScenarios, numberCovariates, numberArms];
            double[,,] expFreq = new double[numberScenarios, numberCovariates, numberArms];
            double[,,] testStat = new double[numberScenarios, numberCovariates, numberArms];
            double[] subjects = new double[numberCovariates];

            double[,] totalTestStat = new double[numberScenarios, numberCovariates];
            double[] maxTestStat = new double[numberScenarios];
            double leastScenarioStat = double.MaxValue;
            int leastScenario = 0;

            int r;

            // Total up the weights
            double weightsTotal = 0;
            Log.Debug("WEIGHTS");
            for (int k = 0; k < numberArms; k++)
            {
                Log.DebugFormat("weights[{0}]:{1}", k, weights[k]);
                weightsTotal += weights[k];
            }
            Log.DebugFormat("weightsTotal {0}", weightsTotal);

            Log.DebugFormat("covLists.Count {0}", covLists.Count);

            // Count up assignments per covariate.
            // All scenarios will be the same at this point.
            // Each covariate 
            for (int c = 0; c < covLists.Count; c++)
            {
                // Each record
                foreach (Dictionary<string, string> dict in covLists[c])
                {
                    // Randomization assignment for record
                    r = int.Parse(dict[assignmentFieldName]);
                    subjects[c]++;

                    // Each scenario
                    for (int s = 0; s < numberScenarios; s++)
                    {
                        obsFreq[s, c, r]++;
                    }
                }
            }

            // Display subject counts for debugging purposes
            if (Log.IsDebugEnabled)
            {
                Log.Debug("Subject counts...");
                for (int c = 0; c < covLists.Count; c++)
                {
                    Log.DebugFormat("subjects[{0}]:{1}", c, subjects[c]);
                }
            }

            // Now assign new subject temporarily to scenario x's arm x.
            // Each scenario
            Log.Debug("OBSERVED AND EXPECTED FREQUENCIES");
            for (int s = 0; s < numberScenarios; s++)
            {
                Log.DebugFormat("Scenario {0}", s);
                // Each covariate
                for (int c = 0; c < numberCovariates; c++)
                {
                    for (int a = 0; a < numberArms; a++)
                    {
                        if (s == a)
                        {
                            // Increment observed frequencies by 1
                            // when the arm is the same as the scenario number
                            obsFreq[s, c, a]++;
                        }
                        // Increment count of subjects by 1 for each arm because of the new assignment
                        // and calculate the expected frequencies based on marginal weights
                        expFreq[s, c, a] = (subjects[c] + 1) * (weights[a] / weightsTotal);
                        Log.DebugFormat("Covariate {0}, Arm {1}, obsFreq {2}, expFreq {3}", c, a, obsFreq[s, c, a], expFreq[s, c, a]);
                    }
                }
            }

            // Compute ChiSquare Test Statistics for each cell/scenario
            // Each scenario
            Log.Debug("CHI SQUARE TEST VALULES");
            for (int s = 0; s < numberScenarios; s++)
            {
                Log.DebugFormat("Scenario {0}", s);
                // Each covariate
                for (int c = 0; c < numberCovariates; c++)
                {
                    // Each arm
                    for (int a = 0; a < numberArms; a++)
                    {
                        // Calculate test statistic for each cell
                        testStat[s, c, a] = Math.Pow((obsFreq[s, c, a] - expFreq[s, c, a]), 2) / expFreq[s, c, a];
                        Log.DebugFormat("Covarariate {0}, Arm {1}, Statistic {2}", c, a, testStat[s, c, a]);
                        totalTestStat[s, c] += testStat[s, c, a];
                    }
                    maxTestStat[s] = (totalTestStat[s, c] > maxTestStat[s]) ? totalTestStat[s, c] : maxTestStat[s];
                }
                Log.DebugFormat("Max total test statistic for scenario {0} is {1}.", s, maxTestStat[s]);
                // Check if we are the least of the totalTestStat for each scenario
                if (maxTestStat[s] < leastScenarioStat)
                {
                    leastScenarioStat = maxTestStat[s];
                    leastScenario = s;
                }
            }
            Log.DebugFormat("Least max total statistic is {0} so scenario {1} wins.", leastScenarioStat, leastScenario);

            // The assignment that results in the least maximum test statistic is the winner!
            return leastScenario;

        } // method
    } // class
} // namespace
