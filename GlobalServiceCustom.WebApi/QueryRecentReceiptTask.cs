using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;

namespace GlobalServiceCustom.WebApi
{
    /// <summary>
    /// WebAPI: 查询近 3 天电子回单（TOP 5000，已确认 SQL 可执行）
    /// </summary>
    public class QueryRecentReceiptTask : AbstractWebApiBusinessService
    {
        public QueryRecentReceiptTask(KDServiceContext context) : base(context)
        {
        }

        /// <summary>
        /// WebAPI 入口（无参数）
        /// </summary>
        public object ExecuteService()
        {
            try
            {
                var ctx = EnsureAppContext();
                var data = QueryRecentReceipts(ctx);

                return new
                {
                    IsSuccess = true,
                    QueryDays = 3,
                    TotalCount = data?.Count ?? 0,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    IsSuccess = false,
                    Message = "Query failed",
                    Detail = GetDeepErrorMessage(ex)
                };
            }
        }

        /// <summary>
        /// 查询近 3 天内电子回单（已验证 SQL 逻辑，TOP 5000）
        /// </summary>
        private List<ReceiptInfoDto> QueryRecentReceipts(Context ctx)
        {
            string sql = @"SELECT TOP 5000
    r.FDATE                  AS ReceiptDate,
    r.FBILLNO                AS ReceiptNo,
    r.FSRCBILLNO             AS SourceBillNo,
    p.F_TWUB_CreatorId_qtr   AS PayBillCreatorId,
    u.FName                  AS PayBillCreatorName,
    u.FEmail                 AS PayBillCreatorEmail,
    r.FDocumentStatus        AS DocumentStatus
FROM T_WB_RECEIPT r WITH (NOLOCK)
JOIN T_AP_PAYBILL p WITH (NOLOCK)
    ON r.FSrcBillNo = p.FBillNo
LEFT JOIN T_SEC_USER u WITH (NOLOCK)
    ON p.F_TWUB_CreatorId_qtr = u.FName
WHERE r.FDocumentStatus = 'C'
  AND r.FDate >= DATEADD(DAY, -3, GETDATE())
  AND p.FBusinessType IN (2, 5)
ORDER BY r.FDate DESC;";

            var rows = DBUtils.ExecuteEnumerable(ctx, sql, CommandType.Text);
            var list = new List<ReceiptInfoDto>();

            foreach (var row in rows)
            {
                list.Add(new ReceiptInfoDto
                {
                    ReceiptDate = row["ReceiptDate"] is DateTime dt
                        ? dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                        : row["ReceiptDate"]?.ToString(),
                    ReceiptNo = row["ReceiptNo"]?.ToString(),
                    SourceBillNo = row["SourceBillNo"]?.ToString(),
                    PayBillCreatorId = row["PayBillCreatorId"]?.ToString(),
                    PayBillCreatorName = row["PayBillCreatorName"]?.ToString(),
                    PayBillCreatorEmail = row["PayBillCreatorEmail"]?.ToString(),
                    DocumentStatus = row["DocumentStatus"]?.ToString()
                });
            }

            return list;
        }

        private Context EnsureAppContext()
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
                throw new Exception("AppContext is null（未获取到金蝶上下文）");
            return ctx;
        }

        private static string GetDeepErrorMessage(Exception ex)
        {
            var msgs = new List<string>();
            int depth = 0;
            while (ex != null && depth++ < 10)
            {
                msgs.Add($"[{ex.GetType().FullName}] {ex.Message}");
                ex = ex.InnerException;
            }

            return string.Join(" -> ", msgs);
        }

        /// <summary>
        /// 回单信息 DTO
        /// </summary>
        private class ReceiptInfoDto
        {
            public string ReceiptDate { get; set; }
            public string ReceiptNo { get; set; }
            public string SourceBillNo { get; set; }
            public string PayBillCreatorId { get; set; }
            public string PayBillCreatorName { get; set; }
            public string PayBillCreatorEmail { get; set; } // ✅ 新增字段
            public string DocumentStatus { get; set; }
        }
    }
}