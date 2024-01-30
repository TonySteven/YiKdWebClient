using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YiKdWebClient.Model
{


    public class AppSettingsModel
    {

        public AppSettingsModel(string path="")
        {
            try
            {
                Dictionary<string, string> dic = CommonService.XmlConfigHelper.GetAllCfgDic(path);

                XKDApiAcctID =GetDicValue(dic, "X-KDApi-AcctID");
                XKDApiAppID = GetDicValue(dic, "X-KDApi-AppID"); 
                XKDApiAppSec = GetDicValue(dic, "X-KDApi-AppSec"); 
                XKDApiUserName = GetDicValue(dic, "X-KDApi-UserName");  
                XKDApiLCID = GetDicValue(dic, "X-KDApi-LCID");
                XKDApiServerUrl = GetDicValue(dic, "X-KDApi-ServerUrl");
                XKDApiOrgNum = GetDicValue(dic, "X-KDApi-OrgNum");
            }
            catch (Exception ex)

            {

            }
        }
        public string XKDApiAcctID { get; set; } = string.Empty;
        public string XKDApiAppID { get; set; } = string.Empty;
        public string XKDApiAppSec { get; set; } = string.Empty;
        public string XKDApiUserName { get; set; } = string.Empty;
        public string XKDApiLCID { get; set; } = string.Empty;

        private string _xkdaApiServerUrl;
        public string XKDApiServerUrl {

            get { return _xkdaApiServerUrl; }
            set
            {
                _xkdaApiServerUrl = GetServerUrl(value);
            }

        }

        public string XKDApiOrgNum{ get; set; } = string.Empty;

        public static string GetServerUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
               // return string.Empty;
               return "https://api.kingdee.com/galaxyapi/";
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

        private string GetDicValue(Dictionary<string, string> dic,string key) { 
            string resvalue = string.Empty;
            try
            {
                return dic[key];
            }
            catch (Exception)
            {

               // throw;
            }
            return resvalue;
        }

    }






}
