using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YiKdWebClient.AuthService
{
    public  class ValidateLogin
    {
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public Dictionary<string, string> RequestHeaders = new Dictionary<string, string>();


        public Model.RequestWebModel Login(string url, string json, bool UnsafeRelaxedJsonEscaping = true)
        {
            Model.RequestWebModel requestWebModel = new Model.RequestWebModel();

            url = Model.ValidateLoginSettingsModel.GetServerUrl(url);
            string loginapiurl = url + @"Kingdee.BOS.WebApi.ServicesStub.AuthService.ValidateUser.common.kdsvc";
            requestWebModel.RequestUrl = loginapiurl;
            //requestWebModel.RealRequestBody = CommonService.JsonHelperServices.getRequestBodystring(json, UnsafeRelaxedJsonEscaping);
            requestWebModel.RealRequestBody = json;

            try
            {
                CommonService.WebHelperServices webHelperServices = new CommonService.WebHelperServices();
                webHelperServices.Timeout = this.Timeout;
                webHelperServices.RequestHeaders = this.RequestHeaders;
                Task<string> postTask = webHelperServices.SendHttpRequestAsync(loginapiurl, requestWebModel.RealRequestBody);
                postTask.Wait(); // 阻塞直到任务完成 

                requestWebModel.RealResponseBody = postTask.Result;
                requestWebModel.Cookie = webHelperServices.cookies;



            }
            catch (Exception ex)
            {
                requestWebModel.RealResponseBody = ex.Message;
                // throw;
            }
            return requestWebModel;


        }

        public string GetLoginJson(Model.ValidateLoginSettingsModel validateLoginSettingsModel, bool UnsafeRelaxedJsonEscaping)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true; // 设置格式化为非缩进格式，即不保留换行符;
            if (UnsafeRelaxedJsonEscaping)
            {
                options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            }
            else
            {
                options.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default;
            }
            List<object> Parameters = new List<object>();
            Parameters.Add(validateLoginSettingsModel.DbId);//帐套Id
            Parameters.Add(validateLoginSettingsModel.UserName);//用户名
            Parameters.Add(validateLoginSettingsModel.Password);//密码
            Parameters.Add(validateLoginSettingsModel.lcid);//账套语系

            string KdContent = System.Text.Json.JsonSerializer.Serialize(Parameters, options);
            string RealContent = CommonService.JsonHelperServices.getLoginRequestBodystring(KdContent, UnsafeRelaxedJsonEscaping,true);

            return RealContent;
        }

    }
}
