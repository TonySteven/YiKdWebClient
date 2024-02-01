using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace YiKdWebClient.Model
{/// <summary>
/// 验证的类型
/// </summary>
    public  enum  LoginType
    {
        /// <summary>
        ///第三方登录授权
        /// </summary>
        [EnumMember(Value = "LoginByAppSecret")]
        [Description("LoginByAppSecret")]
        LoginByAppSecret,
        /// <summary>
        ///API签名认证
        /// </summary>
        [EnumMember(Value = "ApiSignHeaders")]
        [Description("ApiSignHeaders")]
        LoginByApiSignHeaders,

        /// <summary>
        /// 用户密码验证
        /// </summary>
        [EnumMember(Value = "ValidateLogin")]
        [Description("ValidateLogin")]
        ValidateLogin,

       ///集成密钥验证
       /// </summary>
       [EnumMember(Value = "LoginBySimplePassport")]
       [Description("LoginBySimplePassport")]
       LoginBySimplePassport,

    }
}
