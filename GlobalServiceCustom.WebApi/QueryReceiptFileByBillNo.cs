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
            // 1. 获取当前会话上下文
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
            {
                throw new Exception("ctx = null");
            }

            // 2. 明确列出所需字段
            var sql = @"SELECT FID, FBillNo, FData, FName 
                FROM T_WB_ReceiptFile 
                WHERE FBillNo = @FBillNo";

            var sqlParams = new SqlParam("@FBillNo", KDDbType.String, billNo);

            // 3. 查询并映射到实体
            var data = DBUtils.ExecuteEnumerable(ctx, sql, CommandType.Text, sqlParams)
                .Select(row => new ReceiptFileDto
                {
                    Fid = row["FID"]?.ToString(),
                    FBillNo = row["FBillNo"]?.ToString(),
                    FName = row["FName"]?.ToString(),
                    FileBase64 = row["FData"] is byte[] bytes
                        ? Convert.ToBase64String(bytes)
                        : row["FData"]?.ToString() // 兼容字符串类型
                })
                .FirstOrDefault();

            // 4. 处理未查询到的情况
            if (data == null)
            {
                return new { Message = "未找到相关数据" };
            }

            return data;
        }


        private class ReceiptFileDto
        {
            public string Fid { get; set; } // 主键
            public string FBillNo { get; set; } // 单据编号
            public string FName { get; set; } // 文件名称
            public string FileBase64 { get; set; } // Base64 文件流
        }
    }
}