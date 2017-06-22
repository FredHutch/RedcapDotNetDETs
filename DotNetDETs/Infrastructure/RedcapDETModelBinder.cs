using System;
using System.Web;
using System.Collections.Specialized;
using System.Web.Http.ModelBinding;
using System.Web.Http.Controllers;
using log4net;
using System.Linq;

namespace DotNetDETs.Infrastructure
{
    // Class that will hold the posted DET data
    public class RedcapDET
    {
        public string project_id { get; set; }
        public string record { get; set; }
        public string redcap_event_name { get; set; }
        public string redcap_repeat_instrument { get; set; }
        public string redcap_repeat_instance { get; set; }
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

        private string GetValueOrEmpty(NameValueCollection collection, string key)
        {
            string retValue = string.Empty;
            if (collection.AllKeys.Contains(key))
                retValue = collection[key].ToString();

            Log.Debug($"redcapDet.{key}='{retValue}'");
            return retValue;
        }

        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            // Grab the data from the post and then parse it into name-value pairs.
            string formPost = actionContext.Request.Content.ReadAsStringAsync().Result;
            NameValueCollection controls = HttpUtility.ParseQueryString(formPost);

            RedcapDET redcapDet = new RedcapDET();

            for (int i = 0; i < controls.Count; i++)
            {
                Log.DebugFormat("REDCap DET Post {0}:{1}:{2}", i, controls.GetKey(i), controls.Get(i));
            }

            try
            {
                redcapDet.project_id = GetValueOrEmpty(controls, nameof(redcapDet.project_id));
                redcapDet.record = GetValueOrEmpty(controls, nameof(redcapDet.record));
                redcapDet.redcap_event_name = GetValueOrEmpty(controls, nameof(redcapDet.redcap_event_name));
                redcapDet.redcap_repeat_instrument = GetValueOrEmpty(controls, nameof(redcapDet.redcap_repeat_instrument));
                redcapDet.redcap_repeat_instance = GetValueOrEmpty(controls, nameof(redcapDet.redcap_repeat_instance));
                redcapDet.redcap_data_access_group = GetValueOrEmpty(controls, nameof(redcapDet.redcap_data_access_group));
                redcapDet.instrument = GetValueOrEmpty(controls, nameof(redcapDet.instrument));
                redcapDet.complete_flag = GetValueOrEmpty(controls, $"{redcapDet.instrument}_complete");

                Log.Debug($"That last item is actually named... redcapDet.complete_flag='{redcapDet.complete_flag}'");
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Model binder exception:{0}. Inner Exception:{1}", ex.Message, ex.InnerException.Message);
            }

            bindingContext.Model = redcapDet;
            return true;
        }
    }
}