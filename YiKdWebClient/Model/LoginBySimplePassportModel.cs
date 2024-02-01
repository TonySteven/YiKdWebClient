using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace YiKdWebClient.Model
{
    public class LoginBySimplePassportModel
    {
        /// <summary>
        /// 集成密钥文件路径
        /// </summary>
        public string CnfFilePath { get; set; } = string.Empty;
        /// <summary>
        /// 集成密钥Base64密码
        /// </summary>
        public string SimplePassportForBase64 { get; set; } = string.Empty;
        /// <summary>
        /// 账套语言
        /// </summary>
        public int Lcid { get; set; }
        /// <summary>
        /// 使用密钥方式(文件/Base64)
        /// </summary>

        public BySimplePassportType bySimplePassportType { get; set; } = BySimplePassportType.CnfFile;
    }

    public enum BySimplePassportType
    {
        /// <summary>
        ///CNF文件
        /// </summary>
        [EnumMember(Value = "CnfFile")]
        [Description("CnfFile")]
        CnfFile,

        /// <summary>
        ///CNF对应的Base64字符串
        /// </summary>
        [EnumMember(Value = "ForBase64")]
        [Description("ForBase64")]
        ForBase64,
    }
}
