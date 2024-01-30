
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YiKdWebClient
{
    public class YiK3CloudClient
    {
        public YiK3CloudClient() { initialize(); }
        private void initialize()
        {

            //  RealRequestBody= Services.JsonHelperServices.getRequestBodystring(Formid,Jsoninput,UnsafeRelaxedJsonEscaping);

        }
        /// <summary>
        /// Cookie临时存放器
        /// </summary>
        /// 
        public CookieContainer Cookie { get; set; } = new CookieContainer();
        /// <summary>
        /// 请求头存放
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// 请求头字符串
        /// </summary>
        public string RequestHeadersString { get; set; } =string.Empty;
        /// <summary>
        /// 登录验证类型
        /// </summary>
        public Model.LoginType LoginType { get; set; } = Model.LoginType.LoginByAppSecret;
        /// <summary>
        /// AppSettings的设置
        /// </summary>
        public Model.AppSettingsModel AppSettingsModel { get; set; } = new Model.AppSettingsModel();
        /// <summary>
        /// 登录验证真正完整的请求和返回报文
        /// </summary>
        public Model.RequestWebModel ReturnLoginWebModel { get; set; } = new Model.RequestWebModel();
        /// <summary>
        /// 执行本次操作接口的真实请求和返回值
        /// </summary>
        public Model.RequestWebModel ReturnOperationWebModel { get; set; } = new Model.RequestWebModel();


        /// <summary>
        /// 获取对编码内容不太严格的内置 JavaScript 编码器实例 
        /// https://learn.microsoft.com/zh-cn/dotnet/api/system.text.encodings.web.javascriptencoder.unsaferelaxedjsonescaping?view=net-8.0#system-text-encodings-web-javascriptencoder-unsaferelaxedjsonescaping
        /// </summary>
        public bool UnsafeRelaxedJsonEscaping { get; set; } = true;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

        //TimeSpan timeSpan = new TimeSpan(0, 0, 10);
        //TimeSpan timeSpan = TimeSpan.FromSeconds(10);
        //   public TimeSpan Timeout = System.Threading.Timeout.InfiniteTimeSpan;

        private string ExecServicesStubByformid(string formid, string json, string ServicesStubpath, string opNumber = "")
        {
            string apiurl = this.AppSettingsModel.XKDApiServerUrl + ServicesStubpath;
            if (this.LoginType.Equals(Model.LoginType.LoginByApiSignHeaders))
            {
                Dictionary<string, string> ApiHeaders = AuthService.LoginByApiSignHeaders.GetApiHeaders(this.AppSettingsModel, new Uri(apiurl));
                this.RequestHeaders= ApiHeaders;
                this.RequestHeadersString = AuthService.LoginByApiSignHeaders.GetApiHeadersStr(ApiHeaders);
            }
            

            string resjson = string.Empty;
            #region 执行操作
            CommonService.WebHelperServices webHelperServices = new CommonService.WebHelperServices();
            webHelperServices.cookies = this.Cookie;
            webHelperServices.Timeout = this.Timeout;
            webHelperServices.RequestHeaders = this.RequestHeaders;
          
            this.ReturnOperationWebModel.RequestUrl = apiurl;
            string jsonString = CommonService.JsonHelperServices.getRequestBodystring(formid, json, UnsafeRelaxedJsonEscaping, opNumber);
            this.ReturnOperationWebModel.RealRequestBody = jsonString;


            Task<string> postTask = webHelperServices.SendHttpRequestAsync(apiurl, jsonString);
            postTask.Wait(); // 阻塞直到任务完成 
            resjson = postTask.Result;
            this.ReturnOperationWebModel.RealResponseBody = resjson;
            if (this.LoginType.Equals(Model.LoginType.LoginByApiSignHeaders))
            {
                this.Cookie = webHelperServices.cookies;
            }
           
            #endregion
            return resjson;
        }

        private string ExecApiDynamicFormService(string formid, string json, string ServicesStubpath, bool AutoLogin = true, bool AutoLogout = true)
        {
            string resjson = string.Empty;
            #region 校验登录是否成功

            if (this.LoginType.Equals(Model.LoginType.LoginByApiSignHeaders))
            {

            }
            else {
                string LoginResultType = string.Empty;
                try
                {
                    if (AutoLogin)
                    {
                        this.Login();
                    }

                    System.Text.Json.Nodes.JsonNode JsonNodes = System.Text.Json.Nodes.JsonNode.Parse(this.ReturnLoginWebModel.RealResponseBody)!;
                    LoginResultType = Convert.ToString(JsonNodes["LoginResultType"])!;

                }
                catch (Exception ex)
                {
                    return ex.Message;
                    // throw;
                }
                if (!"1".Equals(LoginResultType, StringComparison.OrdinalIgnoreCase))
                {
                    return this.ReturnLoginWebModel.RealResponseBody;
                }
            }
           
            #endregion
       

            try
            {
                resjson = ExecServicesStubByformid(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.View.common.kdsvc");
                if (AutoLogout) { Logout(); }
            }
            catch (Exception ex)
            {
                resjson = ex.Message;
                //  throw;
            }
            return resjson;
        }
        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        public Model.RequestWebModel Login()
        {
            Model.RequestWebModel requestWebModel = new Model.RequestWebModel();

            if (LoginType.Equals(Model.LoginType.LoginByAppSecret))
            {
                AuthService.LoginByAppSecret loginByAppSecret = new AuthService.LoginByAppSecret();
                loginByAppSecret.Timeout = this.Timeout;
                loginByAppSecret.RequestHeaders = this.RequestHeaders;
                string jsonString = loginByAppSecret.GetLoginJson(AppSettingsModel, UnsafeRelaxedJsonEscaping);
                ReturnLoginWebModel.RealRequestBody = jsonString;
                requestWebModel = loginByAppSecret.Login(AppSettingsModel.XKDApiServerUrl, jsonString, true);
            }
            if (LoginType.Equals(Model.LoginType.ValidateLogin))
            {
                if (validateLoginSettingsModel == null) 
                {
                    throw new Exception("LoginType使用ValidateLogin方式的时候，validateLoginSettingsModel 需要实例化赋值");
                }

                AuthService.ValidateLogin validateLogin=new AuthService.ValidateLogin();
                validateLogin.Timeout = this.Timeout;
                validateLogin.RequestHeaders = this.RequestHeaders;
                string jsonString = validateLogin.GetLoginJson(validateLoginSettingsModel, UnsafeRelaxedJsonEscaping);
                ReturnLoginWebModel.RealRequestBody = jsonString;
                requestWebModel = validateLogin.Login(AppSettingsModel.XKDApiServerUrl, jsonString, true);

            }

            if (LoginType.Equals(Model.LoginType.LoginByApiSignHeaders))
            {
                return requestWebModel;
            }

            Cookie = requestWebModel.Cookie;
            this.ReturnLoginWebModel = requestWebModel;


            return requestWebModel;

        }
        /// <summary>
        /// 登出
        /// </summary>
        public void Logout()

        {
            try
            {

                string resjson = string.Empty;

                CommonService.WebHelperServices webHelperServices = new CommonService.WebHelperServices();
                webHelperServices.cookies = this.Cookie;
                webHelperServices.Timeout = this.Timeout;
                webHelperServices.RequestHeaders = this.RequestHeaders;
                string apiurl = this.AppSettingsModel.XKDApiServerUrl + "Kingdee.BOS.WebApi.ServicesStub.AuthService.Logout.common.kdsvc";

                Task<string> postTask = webHelperServices.SendHttpRequestAsync(apiurl);
                postTask.Wait(); // 阻塞直到任务完成 
                resjson = postTask.Result;

            }
            catch (Exception ex)
            {

                //  throw;
            }
        }

        /// <summary>
        /// 通用操作接口
        /// </summary>
        /// <param name="opNumber"></param>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string ExecuteOperation(string opNumber, string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            string resjson = string.Empty;
            #region 校验登录是否成功
            string LoginResultType = string.Empty;
            try
            {
                if (AutoLogin)
                {
                    this.Login();
                }

                System.Text.Json.Nodes.JsonNode JsonNodes = System.Text.Json.Nodes.JsonNode.Parse(this.ReturnLoginWebModel.RealResponseBody)!;
                LoginResultType = Convert.ToString(JsonNodes["LoginResultType"])!;

            }
            catch (Exception ex)
            {
                return ex.Message;
                // throw;
            }
            #endregion
            if (!"1".Equals(LoginResultType, StringComparison.OrdinalIgnoreCase))
            {
                return this.ReturnLoginWebModel.RealResponseBody;
            }

            try
            {
                resjson = ExecServicesStubByformid(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteOperation.common.kdsvc", opNumber);
                if (AutoLogout) { Logout(); }
            }
            catch (Exception ex)
            {
                resjson = ex.Message;
                //  throw;
            }
            return resjson;

        }

        /// <summary>
        /// 查看表单数据接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string View(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.View.common.kdsvc", AutoLogin, AutoLogout);

        }

        /// <summary>
        ///保存表单数据接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string Save(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Save.common.kdsvc", AutoLogin, AutoLogout);

        }

        /// <summary>
        /// 批量保存表单数据接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string BatchSave(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.BatchSave.common.kdsvc", AutoLogin, AutoLogout);

        }

        /// <summary>
        /// 提交表单数据接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string Submit(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Submit.common.kdsvc", AutoLogin, AutoLogout);

        }

        /// <summary>
        /// 审核表单数据接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string Audit(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Audit.common.kdsvc", AutoLogin, AutoLogout);

        }
        /// <summary>
        /// 反审核表单数据接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string UnAudit(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"K3Cloud/Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.UnAudit.common.kdsvc", AutoLogin, AutoLogout);

        }
        /// <summary>
        /// 删除表单数据接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string Delete(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Delete.common.kdsvc", AutoLogin, AutoLogout);

        }

        /// <summary>
        /// 表单数据查询接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string ExecuteBillQuery(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteBillQuery.common.kdsvc", AutoLogin, AutoLogout);

        }
        /// <summary>
        ///自定义WebAPI接口
        /// </summary>
        /// <param name="json"></param>
        /// <param name="apiname">接口命名空间.接口实现类名.方法,组件名.common.kdsvc
        /// 例:"http://192.168.66.60/k3cloud/Kingdee.K3Erp.WebAPI.ServiceExtend.ServicesStub.CustomBusinessService.ExecuteService.common.kdsvc"
        /// </param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string CustomBusinessService(string json, string apiname, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService("",json, apiname, AutoLogin, AutoLogout);

        }
        /// <summary>
        /// 暂存表单数据接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string Draft(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Draft.common.kdsvc", AutoLogin, AutoLogout);

        }
        /// <summary>
        /// 下推接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string Push(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Push.common.kdsvc", AutoLogin, AutoLogout);

        }
        /// <summary>
        /// 分组保存接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string GroupSave(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.GroupSave.common.kdsvc", AutoLogin, AutoLogout);

        }
        /// <summary>
        /// 弹性域保存接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string FlexSave(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
         return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.FlexSave.common.kdsvc", AutoLogin, AutoLogout);
        
        }
        /// <summary>
        ///发送消息接口
        /// </summary>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string SendMsg(string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService("",json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.SendMsg.common.kdsvc", AutoLogin, AutoLogout);
           
        }
        /// <summary>
        /// 切换上下文默认组织接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string SwitchOrg(string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService("", json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.SwitchOrg.common.kdsvc", AutoLogin, AutoLogout);

        }
        /// <summary>
        /// 工作流审批接口
        /// </summary>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string WorkflowAudit(string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService("", json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.WorkflowAudit.common.kdsvc", AutoLogin, AutoLogout);
        }
        /// <summary>
        /// 简单账表查询接口
        /// </summary>
        /// <param name="formid"></param>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string GetSysReportData(string formid, string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService(formid, json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.GetSysReportData.common.kdsvc", AutoLogin, AutoLogout);

        }

        /// <summary>
        /// 附件上传接口(上传附件并绑定单据)
        /// </summary>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string AttachmentUpLoad(string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService("", json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.AttachmentUpLoad.common.kdsvc", AutoLogin, AutoLogout);
        }

        /// <summary>
        /// 附件下载接口
        /// </summary>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string AttachmentDownLoad(string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService("", json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.AttachmentDownLoad.common.kdsvc", AutoLogin, AutoLogout);
        }

        /// <summary>
        /// 文件上传接口(不绑定单据)
        /// </summary>
        /// <param name="json"></param>
        /// <param name="AutoLogin"></param>
        /// <param name="AutoLogout"></param>
        /// <returns></returns>
        public string UploadFile(string json, bool AutoLogin = true, bool AutoLogout = true)
        {
            return ExecApiDynamicFormService("", json, @"Kingdee.BOS.WebApi.ServicesStub.DynamicFormService. UploadFile.common.kdsvc", AutoLogin, AutoLogout);
        }
        /// <summary>
        /// 获取数据中心列表
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetDataCenterList(string url="") 
        {
            string resjson = string.Empty;

            try
            {



                CommonService.WebHelperServices webHelperServices = new CommonService.WebHelperServices();
                webHelperServices.cookies = this.Cookie;
                webHelperServices.Timeout = this.Timeout;
                webHelperServices.RequestHeaders = this.RequestHeaders;

                string apiurl = string.Empty;
                if (string.IsNullOrWhiteSpace(url))
                {
                     apiurl = this.AppSettingsModel.XKDApiServerUrl + "Kingdee.BOS.ServiceFacade.ServicesStub.Account.AccountService.GetDataCenterList.common.kdsvc";
                }
                else
                {
                    apiurl= GetServerUrl(url) + "Kingdee.BOS.ServiceFacade.ServicesStub.Account.AccountService.GetDataCenterList.common.kdsvc";
                }
                

                Task<string> postTask = webHelperServices.SendHttpRequestAsync(apiurl);
                postTask.Wait(); // 阻塞直到任务完成 
                resjson = postTask.Result;

            }
            catch (Exception ex)
            {
                resjson= ex.Message;

                //  throw;


            }
            return resjson;
        }


        public Model.ValidateLoginSettingsModel  validateLoginSettingsModel{ get; set; }=null;

        public static string GetServerUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            try
            {
                if (!url.EndsWith("/"))
                {
                    return url + "/";
                }
            }
            catch (Exception ex)
            {

                //throw;
            }

            return url;
        }
    }
}
