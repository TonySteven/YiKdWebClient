using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YiKdWebClient.Model
{
    /// <summary>
    /// 自定义webapi路由地址模型
    /// </summary>
    public class CustomServicesStubpath
    {
        /// <summary>
        /// dll项目工程的命名空间(必须要以.WebApi结尾，否则系统可能拦截或者不识别)
        /// https://vip.kingdee.com/article/602100140303705088?lang=zh-CN&productLineId=1
        ///
        /// </summary>
        public string ProjetNamespace {  get; set; }=string.Empty;

        /// <summary>
        /// dll项目工程的类名
        /// </summary>
        public string ProjetClassName { get; set; } = string.Empty;

        /// <summary>
        ///  dll项目工程的类中的方法名
        /// </summary>
        public string ProjetClassMethod { get; set; } = string.Empty;

        /// <summary>
        /// 获取ServicesStubpath
        /// </summary>
        /// <returns></returns>
        public string GetCustomServicesStubpathUrl() 
        {
            return ProjetNamespace+ ProjetClassName+ ProjetClassMethod+","+ProjetNamespace + ProjetClassName+ ".common.kdsvc";
        }
    }
}
