using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;
using Newtonsoft.Json;
// ✅ 使用 Newtonsoft.Json

namespace GlobalServiceCustom.WebApi
{
    /// <summary>
    /// WebAPI: Query recent electronic receipts within N days (with pagination)
    /// - Supports JSON input: { "days": 7, "pageIndex": 1, "pageSize": 50 }
    /// - Returns paginated receipt info
    /// </summary>
    public class QueryRecentReceiptTask : AbstractWebApiBusinessService
    {
        public QueryRecentReceiptTask(KDServiceContext context) : base(context)
        {
        }

        /// <summary>
        /// WebAPI Entry: Accepts JSON input with days, pageIndex, pageSize
        /// </summary>
        /// <param name="json">Example: {"days":7,"pageIndex":1,"pageSize":50}</param>
        public object ExecuteService(string json)
        {
            try
            {
                // ✅ 1. 解析 JSON 入参
                var input = JsonConvert.DeserializeObject<RequestParam>(json ?? "{}") ?? new RequestParam();

                if (input.Days <= 0) input.Days = 7;
                if (input.PageIndex <= 0) input.PageIndex = 1;
                if (input.PageSize <= 0) input.PageSize = 50;

                // ✅ 2. 查询总记录数
                int totalCount = GetTotalCount(input.Days);

                // ✅ 3. 查询分页数据
                var data = GetRecentReceipts(input.Days, input.PageIndex, input.PageSize);

                // ✅ 4. 返回结果
                return new
                {
                    IsSuccess = true,
                    QueryDays = input.Days,
                    DataPageIndex = input.PageIndex,
                    DataPageSize = input.PageSize,
                    TotalCount = totalCount,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new { IsSuccess = false, Message = $"Query failed: {ex.Message}" };
            }
        }

        /// <summary>
        /// 统计总记录数
        /// </summary>
        private int GetTotalCount(int days)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null) throw new Exception("AppContext is null");

            string countSql = @"
SELECT COUNT(1) 
FROM T_WB_RECEIPT r
JOIN T_AP_PAYBILL p ON r.FSrcBillNo = p.FBillNo
WHERE r.FDocumentStatus = 'C'
  AND r.FDate >= DATEADD(DAY, -@Days, GETDATE())
  AND p.FBusinessType IN (2, 5)";

            var param = new SqlParam("@Days", KDDbType.Int32, days);
            return Convert.ToInt32(DBUtils.ExecuteScalar(ctx, countSql, param));
        }

        /// <summary>
        /// 查询分页数据
        /// </summary>
        private List<ReceiptInfoDto> GetRecentReceipts(int days, int pageIndex, int pageSize)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null) throw new Exception("AppContext is null");

            int startRow = (pageIndex - 1) * pageSize + 1;
            int endRow = pageIndex * pageSize;

            string sql = $@"
WITH ReceiptCTE AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY r.FDate DESC) AS RowNum,
        r.FDATE AS ReceiptDate,
        r.FBillNo AS ReceiptNo,
        r.FSRCBILLNO AS SourceBillNo,
        p.F_TWUB_CreatorId_qtr AS PayBillCreatorId,
        u.FName AS PayBillCreatorName,
        u.Femail AS PayBillCreatorEmail,
        r.FDocumentStatus AS DocumentStatus
    FROM T_WB_RECEIPT r
    JOIN T_AP_PAYBILL p ON r.FSrcBillNo = p.FBillNo
    LEFT JOIN T_SEC_USER u ON CAST(p.F_TWUB_CreatorId_qtr AS nvarchar(50)) = u.FName
    WHERE r.FDocumentStatus = 'C'
      AND r.FDate >= DATEADD(DAY, -@Days, GETDATE())
      AND p.FBusinessType IN (2, 5)
)
SELECT * FROM ReceiptCTE WHERE RowNum BETWEEN {startRow} AND {endRow}";

            var sqlParams = new SqlParam[]
            {
                new SqlParam("@Days", KDDbType.Int32, days)
            };

            var rows = DBUtils.ExecuteEnumerable(ctx, sql, CommandType.Text, sqlParams);

            return rows.Select(row => new ReceiptInfoDto
            {
                ReceiptDate = row["ReceiptDate"]?.ToString(),
                ReceiptNo = row["ReceiptNo"]?.ToString(),
                SourceBillNo = row["SourceBillNo"]?.ToString(),
                PayBillCreatorId = row["PayBillCreatorId"]?.ToString(),
                PayBillCreatorName = row["PayBillCreatorName"]?.ToString(),
                PayBillCreatorEmail = row["PayBillCreatorEmail"]?.ToString(),
                DocumentStatus = row["DocumentStatus"]?.ToString()
            }).ToList();
        }

        /// <summary>
        /// JSON 入参 DTO
        /// </summary>
        private class RequestParam
        {
            public int Days { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
        }

        /// <summary>
        /// 查询结果 DTO
        /// </summary>
        private class ReceiptInfoDto
        {
            public string ReceiptDate { get; set; }
            public string ReceiptNo { get; set; }
            public string SourceBillNo { get; set; }
            public string PayBillCreatorId { get; set; }
            public string PayBillCreatorName { get; set; }
            public string PayBillCreatorEmail { get; set; }
            public string DocumentStatus { get; set; }
        }
    }
}