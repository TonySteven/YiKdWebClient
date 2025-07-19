using System;
using System.Reflection;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;

namespace GlobalServiceCustom.WebApi
{
    /// <summary>
    /// 【WebApi】自定义WebApi接口
    /// </summary>
    public class CustomWebApiDemoWebApiService : AbstractWebApiBusinessService
    {
        public CustomWebApiDemoWebApiService(KDServiceContext context)
            : base(context)
        {
            //
        }

        /// <summary>
        /// 测试接口
        /// 不需要使用上下文，不需要访问数据中心，则不需要客户端登录
        /// </summary>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <returns>返回请求方法和请求参数</returns>
        public object DoSth(string arg1, int arg2)
        {
            var responseDto = new
            {
                ApiName = MethodInfo.GetCurrentMethod().Name,
                Args = new object[] { arg1, arg2 }
            };
            return responseDto;
        }

        /// <summary>
        /// 测试接口2
        /// 需要使用上下文，需要访问数据中心，则需要客户端登录
        /// </summary>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <returns>返回登录用户名和请求方法和请求参数</returns>
        public object DoSth2(string arg1, int arg2)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
            {
                // 会话超时，需重新登录
                throw new Exception("ctx = null");
            }

            // 访问数据库获取用户名
            var sql = "SELECT FNAME FROM T_SEC_USER WHERE FUSERID=@FUSERID";
            var paramUserId = new SqlParam("@FUSERID", KDDbType.Int32, ctx.UserId);
            var userName = DBUtils.ExecuteScalar(ctx, sql, string.Empty, paramUserId);
            var responseDto = new
            {
                UserName = userName,
                ApiName = MethodInfo.GetCurrentMethod().Name,
                Args = new object[] { arg1, arg2 }
            };
            return responseDto;
        }
    }
}