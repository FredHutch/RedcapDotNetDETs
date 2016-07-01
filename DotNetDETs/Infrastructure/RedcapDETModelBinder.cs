using System;
using System.Web;
using System.Collections.Specialized;
using System.Web.Http.ModelBinding;
using System.Web.Http.Controllers;
using log4net;

namespace DotNetDETs.Infrastructure
{
    // Class that will hold the posted DET data
    public class RedcapDET
    {
        public string project_id { get; set; }
        public string record { get; set; }
        public string redcap_event_name { get; set; }
        public string redcap_data_access_group { get; set; }
        public string instrument { get; set; }
        public string complete_flag { get; set; }
    }

    // Factory object used to instantiate our custom model provider
    public class RedcapDETModelBinderProvider : ModelBinderProvider
    {
        public override IModelBinder GetBinder(System.Web.Http.HttpConfiguration configuration, Type modelType)
        {
            return new RedcapDETModelBinder();
        }
    }

    // Custom model binder that will be used to pluck the data
    // from the posted fields. 
    // A custom model binder is necessary because the name of the 
    // last parameter changes based on the form name so we cannot bind to it by its name.
    public class RedcapDETModelBinder : IModelBinder
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            // Grab the data from the post and then parse it into name-value pairs.
            string formPost = actionContext.Request.Content.ReadAsStringAsync().Result;
            NameValueCollection controls = HttpUtility.ParseQueryString(formPost);

            RedcapDET redcapDet = new RedcapDET();

            //for (int i = 0; i < controls.Count; i++)
            //{
            //    Log.DebugFormat("REDCap DET Post {0}:{1}:{2}", i, controls.GetKey(i), controls.Get(i));
            //}

            try {
                // Grab the values of the fields and populate the RedcapDET object.
                redcapDet.project_id = controls["project_id"].ToString();
                redcapDet.record = controls["record"].ToString();
                try
                {
                    redcapDet.redcap_event_name = controls["redcap_event_name"].ToString();
                }
                catch
                {
                    redcapDet.redcap_event_name = "";
                }
                try
                {
                    redcapDet.redcap_data_access_group = controls["redcap_data_access_group"].ToString();
                }
                catch
                {
                    redcapDet.redcap_data_access_group = "";
                }
                redcapDet.instrument = controls["instrument"].ToString();
                redcapDet.complete_flag = controls[redcapDet.instrument + "_complete"].ToString();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Model binder exception:{0};{1}", ex.Message, ex);
            }

            bindingContext.Model = redcapDet;
            return true;
        }
    }
}