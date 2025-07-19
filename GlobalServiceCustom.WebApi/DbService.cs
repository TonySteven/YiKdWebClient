using System;
using Kingdee.BOS.Log;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.ServicesStub;

namespace GlobalServiceCustom.WebApi
{
    /// <summary>
    /// 【WebApi】访问数据库
    /// </summary>
    public class DbService : AbstractWebApiBusinessService
    {
        public DbService(KDServiceContext context)
            : base(context)
        {
            //
        }

        /// <summary>
        /// 验证数据合法性
        /// </summary>
        /// <param name="encryptSql">加密sql</param>
        /// <returns>解密后的sql</returns>
        private string VerifyData(string encryptSql)
        {
            if (KDContext.Session.AppContext == null)
            {
                // 会话超时，需重新登录
                throw new Exception("ctx = null");
            }

            var decryptSql = EncryptDecryptUtil.Decode2(encryptSql) as string;
            if (string.IsNullOrWhiteSpace(decryptSql))
            {
                throw new Exception("sql解密异常");
            }

            return decryptSql;
        }

        /// <summary>
        /// 执行SQL并返回受影响的行数
        /// </summary>
        /// <param name="encryptSql"></param>
        /// <returns></returns>
        public int DoSth(string encryptSql)
        {
            var decryptSql = VerifyData(encryptSql);
            Logger.Error("Jac", "WebApi.DbService.Execute.SQL：" + decryptSql, null);
            return DBServiceHelper.Execute(this.KDContext.Session.AppContext, decryptSql);
        }

        /// <summary>
        /// 执行SQL并返回查询结果
        /// </summary>
        /// <param name="encryptSql"></param>
        /// <returns></returns>
        public object GetDataSet(string encryptSql)
        {
            var decryptSql = VerifyData(encryptSql);
            Logger.Error("Jac", "WebApi.DbService.ExecuteDataSet.SQL：" + decryptSql, null);
            return DBServiceHelper.ExecuteDataSet(this.KDContext.Session.AppContext, decryptSql);
        }

        /// <summary>
        /// 执行SQL并返回查询结果
        /// </summary>
        /// <param name="encryptSql"></param>
        /// <returns></returns>
        public object GetDynamicObject(string encryptSql)
        {
            var decryptSql = VerifyData(encryptSql);
            Logger.Error("Jac", "WebApi.DbService.ExecuteDynamicObject.SQL：" + decryptSql, null);
            return null;
        }
    }
}