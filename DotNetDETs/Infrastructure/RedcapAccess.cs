using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Configuration;
using System.Diagnostics;
using log4net;
using Newtonsoft.Json;

namespace DotNetDETs.Infrastructure
{
    public class RedcapAccess
    {
        string uri;
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public RedcapAccess()
        {
            this.uri = WebConfigurationManager.AppSettings["REDCapURI"];
        }

        public RedcapAccess(string uri)
        {
            this.uri = uri;
        }

        public List<Dictionary<string, string>> GetRedcapData(string strPostToken, string strRecordsSelect,
                string strFields, string strEvents, string strForms, bool boolLabels, bool boolLabelHeaders, bool boolCheckboxLabel,
                bool boolSurveyFields, bool boolAccessGroups, string strFilterLogic, string strReturnFormat)
        {
            // Based on code from Chris Nefcy at University of Washington
            // https://github.com/redcap-tools/nef-c-sharp

            // Get any RC data and return as List of Dictionaries. 
            // Requires Token parameter . Other parameters are optional.
            // strRecordsSelect: any records you want seperated by ','; all records if ""
            // strFields: Particular fields you want, seperated by ','; all fields if ""
            // strEvents: Particular events you want, seperated by ','; all events if ""
            // strForms: Particular forms you want, seperated by ','; all forms if ""
            // boolLabels: false=raw; true=label
            // boolLabelHeaders: false=raw; true=label
            // boolCheckboxLabel: true or false
            // boolSurveyFields: true or false
            // boolAccessGroups: false=no access group returned; true=access group returned 
            // strFilterLogic: filter logic (e.g., [field]=value) or "" if none
            // strReturnFormat: csv, json, xml. If blank: json

            string strPostParameters = "";

            List<Dictionary<string, string>> redcapData = new List<Dictionary<string, string>>();

            strPostParameters = "&content=record&format=json&type=flat";

            if (strRecordsSelect != "")
            {
                strPostParameters += "&records=" + strRecordsSelect;
            }

            if (strFields != "")
            {
                strPostParameters += "&fields=" + strFields;
            }

            if (strEvents != "")
            {
                strPostParameters += "&events=" + strEvents;
            }

            if (strForms != "")
            {
                strPostParameters += "&forms=" + strForms;
            }

            if (boolLabels)
                strPostParameters += "&rawOrLabel=label";
            else
                strPostParameters += "&rawOrLabel=raw";

            if (boolLabelHeaders)
                strPostParameters += "&rawOrLabelHeaders=label";
            else
                strPostParameters += "&rawOrLabelHeaders=raw";

            strPostParameters += string.Format("&exportCheckboxLabel={0}", boolCheckboxLabel.ToString().ToLower());
            strPostParameters += string.Format("&exportSurveyFields={0}", boolSurveyFields.ToString().ToLower());
            strPostParameters += string.Format("&exportDataAccessGroups={0}", boolAccessGroups.ToString().ToLower());

            if (strFilterLogic != "")
            {
                strPostParameters += "&filterLogic=" + strFilterLogic;
            }

            if (strReturnFormat != "")
            {
                strPostParameters += "&returnFormat=" + strReturnFormat;
            }
            else
            {
                strPostParameters += "&returnFormat=json";
            }

            Log.DebugFormat("GetTableFromAnyRC calling API with these parameters : /token={0}{1}/", strPostToken, strPostParameters);

            byte[] bytePostData = Encoding.UTF8.GetBytes("token=" + strPostToken + strPostParameters);

            string strResponse = responseHTTP(bytePostData);

            redcapData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(strResponse);

            return redcapData;
        }

        public Dictionary<string, string> GetValueLabels(string strPostToken,
                string strForms, string strFields, string strFormat, string strReturnFormat)
        {
            // Get value labels from REDCap metadata.

            List<Metadata> metadataRows = GetRedcapMetaData(strPostToken,
                strForms, strFields, strFormat, strReturnFormat);

            Dictionary<string, string> valueLabels = new Dictionary<string, string>();

            if (metadataRows.Count == 0)
            {
                Log.Debug("Could not find any project metadata");
            }

            foreach (Metadata md in metadataRows)
            {
                if (md.field_type == "radio" || md.field_type == "dropdown")
                {
                    string[] choices = md.select_choices_or_calculations.Split('\n');
                    // Some mystery as to when/whether choices are separated by '|' or '\n'
                    if (choices.Length == 1)
                    {
                        choices = choices[0].Split('|');
                    }
                    foreach (string choice in choices)
                    {
                        string[] labels = choice.Split(',');
                        valueLabels.Add(md.field_name + ":" + labels[0].Trim(), labels[1].Trim());
                    }
                }
            }
            return valueLabels;
        }

        public List<Metadata> GetRedcapMetaData(string strPostToken,
                string strForms, string strFields, string strFormat, string strReturnFormat)
        {
            // Get any REDCap metadata (data dictionary) and return Dictionary. 

            // strFields: Particular fields you want, seperated by ','; all fields if ""
            // strForms: Particular forms you want, seperated by ','; all forms if ""
            // strFormat: csv, json, xml. If blank: json
            // strReturnFormat: csv, json, xml. If blank: json

            string strPostParameters = "";

            List<Metadata> redcapMetaData = new List<Metadata>();

            strPostParameters = "&content=metadata";

            if (strForms != "")
            {
                strPostParameters += "&forms=" + strForms;
            }

            if (strFields != "")
            {
                strPostParameters += "&fields=" + strFields;
            }

            if (strFormat != "")
            {
                strPostParameters += "&format=" + strFormat;
            }
            else
            {
                strPostParameters += "&format=json";
            }

            if (strReturnFormat != "")
            {
                strPostParameters += "&returnFormat=" + strReturnFormat;
            }
            else
            {
                strPostParameters += "&returnFormat=json";
            }

            Log.DebugFormat("GetRedcapMetaData calling API with these parameters : /token={0}{1}/", strPostToken, strPostParameters);

            byte[] bytePostData = Encoding.UTF8.GetBytes("token=" + strPostToken + strPostParameters);

            string strResponse = responseHTTP(bytePostData);

            Log.DebugFormat("Response: {0}", strResponse);
            redcapMetaData = JsonConvert.DeserializeObject<List<Metadata>>(strResponse);

            return redcapMetaData;
        }

        public string PostRedcapData(string strPostToken, string strJson, bool boolOverwrite, string dateFormat)
        {
            // Based on code from Chris Nefcy at University of Washington
            // https://github.com/redcap-tools/nef-c-sharp

            // Posts data to REDCap using Json string 
            string strPostParameters = "&content=record&format=json&type=flat";
            if (boolOverwrite)
                strPostParameters += "&overwriteBehavior=overwrite";
            else
                strPostParameters += "&overwriteBehavior=normal";

            strPostParameters += "&returnContent=count&returnFormat=csv";

            strPostParameters += "&data=" + strJson;

            strPostParameters += "&dateFormat=" + dateFormat;

            Log.DebugFormat("PostRedcapData calling API with these parameters : /token={0}{1}/", strPostToken, strPostParameters);
            byte[] bytePostData = Encoding.ASCII.GetBytes("token=" + strPostToken + strPostParameters);

            string strResponse = responseHTTP(bytePostData);

            // Error if more than 9999 records (this num could change, but is just what I tried),
            // or most likely, just has an error message.
            if (strResponse.Length > 4)
            {
                throw new Exception("RC PostRedcapData Error: " + strResponse);
            }
            else
            {
                Log.DebugFormat("Completed importing {0} rows into REDCap.", strResponse);
            }

            return (strResponse);
        }

        private string responseHTTP(byte[] bytePostData)
        {
            // Based on code from Chris Nefcy at University of Washington
            // https://github.com/redcap-tools/nef-c-sharp

            // Makes the API call and returns response from request
            Debug.WriteLine("responseHTTP()");
            string strResponse = "";

            try
            {
                HttpWebRequest webreqRedCap = (HttpWebRequest)WebRequest.Create(uri);

                webreqRedCap.Method = "POST";
                webreqRedCap.ContentType = "application/x-www-form-urlencoded";
                webreqRedCap.ContentLength = bytePostData.Length;

                // Get the request stream and read it
                Stream streamData = webreqRedCap.GetRequestStream();
                streamData.Write(bytePostData, 0, bytePostData.Length);
                streamData.Close();

                HttpWebResponse webrespRedCap = (HttpWebResponse)webreqRedCap.GetResponse();

                //Now, read the response (the string), and output it.
                Stream streamResponse = webrespRedCap.GetResponseStream();
                StreamReader readerResponse = new StreamReader(streamResponse);

                strResponse = readerResponse.ReadToEnd();
            }
            catch (WebException exWE)
            {
                Stream streamWE = exWE.Response.GetResponseStream();
                StringBuilder sbResponse = new StringBuilder("", 65536);

                try
                {
                    byte[] readBuffer = new byte[1000];
                    int intCnt = 0;

                    for (;;)
                    {
                        intCnt = streamWE.Read(readBuffer, 0, readBuffer.Length);

                        if (intCnt == 0)
                        {
                            // EOF
                            break;
                        }

                        sbResponse.Append(System.Text.Encoding.UTF8.GetString(readBuffer, 0, intCnt));
                    }

                }
                finally
                {
                    streamWE.Close();

                    strResponse = sbResponse.ToString();
                }
            }
            catch (Exception ex)
            {
                strResponse = ex.Message.ToString();
            }

            return (strResponse);
        }
    }
}