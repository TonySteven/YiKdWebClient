using System;
using System.Collections.Generic;
using System.Globalization;

namespace GlobalServiceCustom.WebApi
{
    /// <summary>
    /// WebAPI: 查询近 N 天电子回单（分页）
    /// 入参 JSON: { "days": 7, "pageIndex": 1, "pageSize": 50 }
    /// </summary>
    public class QueryRecentReceiptTask : AbstractWebApiBusinessService
    {
        public QueryRecentReceiptTask(KDServiceContext context) : base(context) { }

        /// <summary>
        /// WebAPI 入口
        /// </summary>
        /// <param name="json">{"days":7,"pageIndex":1,"pageSize":50}</param>
        public object ExecuteService(string json)
        {
            try
            {
                // 1) 解析入参 + 兜底
                var input = JsonConvert.DeserializeObject<RequestParam>(json ?? "{}") ?? new RequestParam();
                int days = input.Days > 0 ? input.Days : 7;
                int pageIndex = input.PageIndex > 0 ? input.PageIndex : 1;
                int pageSize = input.PageSize > 0 ? input.PageSize : 50;

                // 限制 pageSize 上限（避免一次捞太多）
                if (pageSize > 200) pageSize = 200;

                // 2) 统一在 C# 里计算开始时间，避免 SQL 里的 -@Days
                DateTime startDate = DateTime.Now.AddDays(-days);

                var ctx = KDContext?.Session?.AppContext ?? this.Context?.Session?.AppContext;
                if (ctx == null) throw new Exception("AppContext is null（未获取到金蝶应用上下文）");

                // 3) 统计总数
                int totalCount = GetTotalCount(ctx, startDate);

                // 4) 分页数据
                var data = GetRecentReceipts(ctx, startDate, pageIndex, pageSize);

                // 5) 返回
                return new
                {
                    IsSuccess = true,
                    QueryDays = days,
                    DataPageIndex = pageIndex,
                    DataPageSize = pageSize,
                    TotalCount = totalCount,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                // 提供更可读的错误信息
                return new
                {
                    IsSuccess = false,
                    Message = $"Query failed: {ex.Message}",
                    Detail = ex.StackTrace
                };
            }
        }

        /// <summary>
        /// 统计总记录数（用 @StartDate）
        /// </summary>
        private int GetTotalCount(Context ctx, DateTime startDate)
        {
            const string countSql = @"
SELECT COUNT(1)
FROM T_WB_RECEIPT r
JOIN T_AP_PAYBILL p ON r.FSrcBillNo = p.FBillNo
WHERE r.FDocumentStatus = 'C'
  AND r.FDate >= @StartDate
  AND p.FBusinessType IN (2, 5);";

            var param = new SqlParam("@StartDate", KDDbType.DateTime, startDate);
            object scalar = DBUtils.ExecuteScalar(ctx, countSql, param);
            return Convert.ToInt32(scalar);
        }

        /// <summary>
        /// 查询分页数据（SQL Server: OFFSET/FETCH；全参数化）
        /// </summary>
        private List<ReceiptInfoDto> GetRecentReceipts(Context ctx, DateTime startDate, int pageIndex, int pageSize)
        {
            int offset = (pageIndex - 1) * pageSize;

            // 说明：
            // - 如果运行库不是 SQL Server，OFFSET/FETCH 可能不支持，可换回 CTE + ROW_NUMBER 的写法。
            // - 关键点是避免 -@Days，统一使用 @StartDate。
            const string sql = @"
SELECT
    r.FDATE                AS ReceiptDate,
    r.FBILLNO              AS ReceiptNo,
    r.FSRCBILLNO           AS SourceBillNo,
    p.F_TWUB_CreatorId_qtr AS PayBillCreatorId,  -- 这里根据你的字段实际意义再调整
    u.FName                AS PayBillCreatorName,
    u.Femail               AS PayBillCreatorEmail,
    r.FDocumentStatus      AS DocumentStatus
FROM T_WB_RECEIPT r
JOIN T_AP_PAYBILL p ON r.FSrcBillNo = p.FBillNo
LEFT JOIN T_SEC_USER u 
    ON CAST(p.F_TWUB_CreatorId_qtr AS NVARCHAR(50)) = u.FName -- ⚠️原表结构不详，谨慎核对！建议用 UserId 关联更稳
WHERE r.FDocumentStatus = 'C'
  AND r.FDate >= @StartDate
  AND p.FBusinessType IN (2, 5)
ORDER BY r.FDate DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            var sqlParams = new[]
            {
                new SqlParam("@StartDate", KDDbType.DateTime, startDate),
                new SqlParam("@Offset",    KDDbType.Int32,    offset),
                new SqlParam("@PageSize",  KDDbType.Int32,    pageSize)
            };

            var rows = DBUtils.ExecuteEnumerable(ctx, sql, CommandType.Text, sqlParams);

            // 输出做健壮转换：日期统一为 ISO 字符串，其他字段为 string
            var list = new List<ReceiptInfoDto>();
            foreach (var row in rows)
            {
                // FDATE 常见为 DateTime，这里统一转成 ISO8601 字符串（不带时区）
                string receiptDateStr = null;
                if (row["ReceiptDate"] != null && row["ReceiptDate"] != DBNull.Value)
                {
                    if (row["ReceiptDate"] is DateTime dt)
                        receiptDateStr = dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    else
                        receiptDateStr = row["ReceiptDate"].ToString();
                }

                list.Add(new ReceiptInfoDto
                {
                    ReceiptDate        = receiptDateStr,
                    ReceiptNo          = row["ReceiptNo"]?.ToString(),
                    SourceBillNo       = row["SourceBillNo"]?.ToString(),
                    PayBillCreatorId   = row["PayBillCreatorId"]?.ToString(),
                    PayBillCreatorName = row["PayBillCreatorName"]?.ToString(),
                    PayBillCreatorEmail= row["PayBillCreatorEmail"]?.ToString(),
                    DocumentStatus     = row["DocumentStatus"]?.ToString()
                });
            }

            return list;
        }

        /// <summary>
        /// 入参 DTO
        /// </summary>
        private class RequestParam
        {
            public int Days { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
        }

        /// <summary>
        /// 出参 DTO
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