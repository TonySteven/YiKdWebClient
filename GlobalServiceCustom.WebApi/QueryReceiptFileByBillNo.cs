using System;
using System.Data;
using System.Linq;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;

namespace GlobalServiceCustom.WebApi
{
    public class QueryReceiptFileByBillNo : AbstractWebApiBusinessService
    {
        public QueryReceiptFileByBillNo(KDServiceContext context) : base(context)
        {
        }

        public object ExecuteService(string billNo)
        {
            if (string.IsNullOrWhiteSpace(billNo))
            {
                // 返回错误信息
                return new { IsSuccess = false, Message = "FBillNo 参数不能为空" };
            }

            try
            {
                // 执行 SQL 查询并返回查询结果
                var result = GetReceiptFileByBillNo(billNo);

                return new
                {
                    IsSuccess = true,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                // 返回错误信息
                return new { IsSuccess = false, Message = $"查询失败: {ex.Message}" };
            }
        }

        /// <summary>
        /// 根据账单号查询数据库
        /// </summary>
        /// <param name="billNo">账单号</param>
        /// <returns>查询结果</returns>
        private object GetReceiptFileByBillNo(string billNo)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
            {
                // 会话超时，需重新登录
                throw new Exception("ctx = null");
            }

            var sql = "SELECT * FROM T_WB_ReceiptFile WHERE FBillNo = @FBillNo";
            var sqlParams = new SqlParam("@FBillNo", KDDbType.String, billNo);

            // 使用 ExecuteEnumerable 代替 ExecuteDataSet
            var data = DBUtils.ExecuteEnumerable(ctx, sql, CommandType.Text, sqlParams)
                .Select(row => new
                {
                    FBillNo = row["FBillNo"],
                    // 根据实际需求返回其他字段
                })
                .FirstOrDefault(); // 获取第一条数据

            // 如果没有查询到数据，返回自定义消息
            if (data == null)
            {
                return new { Message = "未找到相关数据" };
            }

            return data;
        }
    }
}