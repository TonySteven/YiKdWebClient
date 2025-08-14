using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;
using Newtonsoft.Json;

namespace GlobalServiceCustom.WebApi
{
    /// <summary>
    /// WebAPI: 查询近 N 天电子回单（分页）
    /// 入参 JSON: { "days": 7, "pageIndex": 1, "pageSize": 50 }
    /// </summary>
    public class QueryRecentReceiptTask : AbstractWebApiBusinessService
    {
        // -------------------- 常量与配置 --------------------
        private const int DEFAULT_DAYS = 7;
        private const int DEFAULT_PAGE_INDEX = 1;
        private const int DEFAULT_PAGE_SIZE = 50;
        private const int MAX_PAGE_SIZE = 200;
        private const int MIN_PAGE_SIZE = 1;

        public QueryRecentReceiptTask(KDServiceContext context) : base(context)
        {
        }

        /// <summary>
        /// WebAPI 入口
        /// </summary>
        /// <param name="json">{"days":7,"pageIndex":1,"pageSize":50}</param>
        public object ExecuteService(string json)
        {
            try
            {
                // 1) 解析入参
                var input = SafeDeserialize<RequestParam>(json) ?? new RequestParam();

                // 2) 归一化入参（兜底+边界）
                int days = input.Days > 0 ? input.Days : DEFAULT_DAYS;
                int pageIndex = input.PageIndex > 0 ? input.PageIndex : DEFAULT_PAGE_INDEX;
                int pageSize = input.PageSize > 0 ? input.PageSize : DEFAULT_PAGE_SIZE;

                if (pageSize > MAX_PAGE_SIZE) pageSize = MAX_PAGE_SIZE;
                if (pageSize < MIN_PAGE_SIZE) pageSize = MIN_PAGE_SIZE;

                // 3) 时间边界（是否包含当天可按需切换：含当天可用 Now.Date.AddDays(-(days-1))）
                // 含当天：var startDate = DateTime.Now.Date.AddDays(-(days - 1));
                // 不含当天（近 N*24h）： 
                var startDate = DateTime.Now.AddDays(-days);

                // 4) 获取上下文
                var ctx = EnsureAppContext();
                // 5) 查询
                int totalCount = GetTotalCount(ctx, startDate);
                var data = GetRecentReceipts(ctx, startDate, pageIndex, pageSize);

                // 6) 返回
                return ResponseDto.Success(days, pageIndex, pageSize, totalCount, data);
            }
            catch (Exception ex)
            {
                // 生产可去掉 Detail 以避免泄露实现细节
                return ResponseDto.Fail($"Query failed: {ex.Message}", ex.StackTrace);
            }
        }

        // -------------------- 数据访问 --------------------

        /// <summary>
        /// 统计总记录数（使用 @StartDate，避免 -@Days）
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

            try
            {
                object scalar = DBUtils.ExecuteScalar(ctx, countSql, param);
                return Convert.ToInt32(scalar);
            }
            catch (Exception e)
            {
                throw new Exception("Execute count sql failed.", e);
            }
        }

        /// <summary>
        /// 查询分页数据（SQL Server: OFFSET/FETCH；如不支持请改用下方注释的 CTE+ROW_NUMBER 写法）
        /// </summary>
        private List<ReceiptInfoDto> GetRecentReceipts(Context ctx, DateTime startDate, int pageIndex, int pageSize)
        {
            int offset = (pageIndex - 1) * pageSize;

            // 重要说明：
            // - 该 SQL 使用 SQL Server 的 OFFSET/FETCH 语法；
            // - 如果是其他数据库或旧版本不支持，请使用下方注释的 CTE + ROW_NUMBER 版本。
            const string sql = @"
SELECT
    r.FDATE                AS ReceiptDate,
    r.FBILLNO              AS ReceiptNo,
    r.FSRCBILLNO           AS SourceBillNo,
    p.F_TWUB_CreatorId_qtr AS PayBillCreatorId,
    u.FName                AS PayBillCreatorName,
    u.Femail               AS PayBillCreatorEmail,
    r.FDocumentStatus      AS DocumentStatus
FROM T_WB_RECEIPT r
JOIN T_AP_PAYBILL p ON r.FSrcBillNo = p.FBillNo
LEFT JOIN T_SEC_USER u
    -- ⚠️ 建议按用户唯一标识关联（如 FUSERID）；当前以 FName 匹配风险较高，按你的库结构自行调整
    ON CAST(p.F_TWUB_CreatorId_qtr AS NVARCHAR(50)) = u.FName
WHERE r.FDocumentStatus = 'C'
  AND r.FDate >= @StartDate
  AND p.FBusinessType IN (2, 5)
ORDER BY r.FDate DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            var sqlParams = new[]
            {
                new SqlParam("@StartDate", KDDbType.DateTime, startDate),
                new SqlParam("@Offset", KDDbType.Int32, offset),
                new SqlParam("@PageSize", KDDbType.Int32, pageSize)
            };

            try
            {
                var rows = DBUtils.ExecuteEnumerable(ctx, sql, CommandType.Text, sqlParams);
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
                        ReceiptDate = receiptDateStr,
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
            catch (Exception e)
            {
                throw new Exception("Execute page sql failed.", e);
            }

            /*
            // ---------- 兼容旧库的 CTE + ROW_NUMBER 版本（如 OFFSET/FETCH 不可用时） ----------
            const string sqlRowNumber = @"
;WITH CTE AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY r.FDATE DESC) AS rn,
        r.FDATE                AS ReceiptDate,
        r.FBILLNO              AS ReceiptNo,
        r.FSRCBILLNO           AS SourceBillNo,
        p.F_TWUB_CreatorId_qtr AS PayBillCreatorId,
        u.FName                AS PayBillCreatorName,
        u.Femail               AS PayBillCreatorEmail,
        r.FDocumentStatus      AS DocumentStatus
    FROM T_WB_RECEIPT r
    JOIN T_AP_PAYBILL p ON r.FSrcBillNo = p.FBillNo
    LEFT JOIN T_SEC_USER u
        ON CAST(p.F_TWUB_CreatorId_qtr AS NVARCHAR(50)) = u.FName
    WHERE r.FDocumentStatus = 'C'
      AND r.FDate >= @StartDate
      AND p.FBusinessType IN (2, 5)
)
SELECT
    ReceiptDate, ReceiptNo, SourceBillNo, PayBillCreatorId,
    PayBillCreatorName, PayBillCreatorEmail, DocumentStatus
FROM CTE
WHERE rn BETWEEN @Begin AND @End
ORDER BY rn;";
            var begin = offset + 1;
            var end = offset + pageSize;
            var sqlParams2 = new[]
            {
                new SqlParam("@StartDate", KDDbType.DateTime, startDate),
                new SqlParam("@Begin",     KDDbType.Int32,    begin),
                new SqlParam("@End",       KDDbType.Int32,    end),
            };
            */
        }

        // -------------------- 工具方法 --------------------

        /// <summary>
        /// 兼容 K3 网关传入的 {parameters: ["json"]} 与直接 JSON 字符串
        /// </summary>
        private static T SafeDeserialize<T>(string json)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                // 兼容网关把参数包进 parameters 数组的格式
                // 例：{"parameters":["{\"days\":3,\"pageIndex\":1,\"pageSize\":10}"]}
                var wrapper = JsonConvert.DeserializeObject<ParametersWrapper>(json);
                if (wrapper?.Parameters != null && wrapper.Parameters.Length > 0)
                {
                    var inner = wrapper.Parameters[0];
                    if (!string.IsNullOrWhiteSpace(inner))
                    {
                        return JsonConvert.DeserializeObject<T>(inner);
                    }
                }
            }
            catch
            {
                // 不是 parameters 包装格式，走下一步
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return null;
            }
        }

        private Context EnsureAppContext()
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
                throw new Exception("AppContext is null（未获取到金蝶应用上下文）");
            return ctx;
        }


        // -------------------- DTO 定义 --------------------

        private class ParametersWrapper
        {
            [JsonProperty("parameters")] public string[] Parameters { get; set; }
        }

        private class RequestParam
        {
            [JsonProperty("days")] public int Days { get; set; }

            [JsonProperty("pageIndex")] public int PageIndex { get; set; }

            [JsonProperty("pageSize")] public int PageSize { get; set; }
        }

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

        private class ResponseDto
        {
            public bool IsSuccess { get; set; }
            public int QueryDays { get; set; }
            public int DataPageIndex { get; set; }
            public int DataPageSize { get; set; }
            public int TotalCount { get; set; }
            public List<ReceiptInfoDto> Data { get; set; }
            public string Message { get; set; }
            public string Detail { get; set; }

            public static ResponseDto Success(int days, int pageIndex, int pageSize, int totalCount,
                List<ReceiptInfoDto> data)
            {
                return new ResponseDto
                {
                    IsSuccess = true,
                    QueryDays = days,
                    DataPageIndex = pageIndex,
                    DataPageSize = pageSize,
                    TotalCount = totalCount,
                    Data = data
                };
            }

            public static ResponseDto Fail(string message, string detail = null)
            {
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = message,
                    Detail = detail
                };
            }
        }
    }
}