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
    /// 入参示例: {"days":7,"pageIndex":1,"pageSize":50}
    /// 也兼容 {"parameters":["{\"days\":7,\"pageIndex\":1,\"pageSize\":50}"]}
    /// </summary>
    public class QueryRecentReceiptTaskPage : AbstractWebApiBusinessService
    {
        // -------------------- 常量与配置 --------------------
        private const int DEFAULT_DAYS = 7;
        private const int DEFAULT_PAGE_INDEX = 1;
        private const int DEFAULT_PAGE_SIZE = 50;
        private const int MAX_PAGE_SIZE = 200;
        private const int MIN_PAGE_SIZE = 1;

        public QueryRecentReceiptTaskPage(KDServiceContext context) : base(context)
        {
        }

        // -------------------- 入口 --------------------
        public object ExecuteService(string json)
        {
            try
            {
                // 1) 解析入参（兼容 parameters 包装）
                var input = SafeDeserialize<RequestParam>(json) ?? new RequestParam();

                // 2) 归一化入参
                int days = input.Days > 0 ? input.Days : DEFAULT_DAYS;
                int pageIndex = input.PageIndex > 0 ? input.PageIndex : DEFAULT_PAGE_INDEX;
                int pageSize = input.PageSize > 0 ? input.PageSize : DEFAULT_PAGE_SIZE;
                if (pageSize > MAX_PAGE_SIZE) pageSize = MAX_PAGE_SIZE;
                if (pageSize < MIN_PAGE_SIZE) pageSize = MIN_PAGE_SIZE;

                // 3) 时间窗口（近 N*24h）
                var startDate = DateTime.Now.AddDays(-days);

                // 4) 上下文
                var ctx = EnsureAppContext();

                // 5) 探测是否支持 OFFSET/FETCH（SQL Server 2012+）
                bool supportsOffsetFetch = SupportsOffsetFetch(ctx);

                // 6) 查询总数 & 分页数据
                int totalCount = GetTotalCount(ctx, startDate);
                var data = GetPage(ctx, startDate, pageIndex, pageSize, supportsOffsetFetch);

                // 7) 返回
                return ResponseDto.Success(days, pageIndex, pageSize, totalCount, data);
            }
            catch (Exception ex)
            {
                return ResponseDto.Fail("Query failed", GetDeepErrorMessage(ex));
            }
        }

        // -------------------- 数据访问 --------------------

        /// <summary>统计总记录数（仅做必要过滤；COUNT_BIG 更稳）</summary>
        private int GetTotalCount(Context ctx, DateTime startDate)
        {
            const string sql = @"
SELECT COUNT_BIG(1)
FROM T_WB_RECEIPT r WITH (NOLOCK)
JOIN T_AP_PAYBILL p WITH (NOLOCK) ON r.FSrcBillNo = p.FBillNo
WHERE r.FDocumentStatus = 'C'
  AND r.FDate >= @StartDate
  AND p.FBusinessType IN (2, 5);";

            var p = new SqlParam("@StartDate", KDDbType.DateTime, startDate);

            try
            {
                var o = DBUtils.ExecuteScalar(ctx, sql, p);
                // COUNT_BIG -> long；再安全转 int（数据极大时可改返回 long）
                return Convert.ToInt32(Convert.ToInt64(o));
            }
            catch (Exception e)
            {
                throw new Exception("Execute count sql failed", e);
            }
        }

        /// <summary>分页查询（自动适配 OFFSET/FETCH 或 ROW_NUMBER）</summary>
        private List<ReceiptInfoDto> GetPage(Context ctx, DateTime startDate, int pageIndex, int pageSize,
            bool useOffsetFetch)
        {
            if (useOffsetFetch)
            {
                int offset = (pageIndex - 1) * pageSize;
                const string sql = @"
SELECT
    r.FDATE                AS ReceiptDate,
    r.FBILLNO              AS ReceiptNo,
    r.FSRCBILLNO           AS SourceBillNo,
    p.F_TWUB_CreatorId_qtr AS PayBillCreatorId,
    u.FName                AS PayBillCreatorName,
    u.FEmail               AS PayBillCreatorEmail,
    r.FDocumentStatus      AS DocumentStatus
FROM T_WB_RECEIPT r WITH (NOLOCK)
JOIN T_AP_PAYBILL p WITH (NOLOCK) ON r.FSrcBillNo = p.FBillNo
LEFT JOIN T_SEC_USER u WITH (NOLOCK)
    -- 建议按唯一主键关联（如 FUSERID）；此处为示例，请按实际库结构替换
    ON CAST(p.F_TWUB_CreatorId_qtr AS NVARCHAR(50)) = u.FName
WHERE r.FDocumentStatus = 'C'
  AND r.FDate >= @StartDate
  AND p.FBusinessType IN (2, 5)
ORDER BY r.FDate DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                var ps = new[]
                {
                    new SqlParam("@StartDate", KDDbType.DateTime, startDate),
                    new SqlParam("@Offset", KDDbType.Int32, offset),
                    new SqlParam("@PageSize", KDDbType.Int32, pageSize)
                };
                return ReadRows(ctx, sql, ps);
            }
            else
            {
                // 兼容旧库
                int begin = (pageIndex - 1) * pageSize + 1;
                int end = begin + pageSize - 1;

                const string sql = @"
;WITH CTE AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY r.FDATE DESC) AS rn,
        r.FDATE                AS ReceiptDate,
        r.FBILLNO              AS ReceiptNo,
        r.FSRCBILLNO           AS SourceBillNo,
        p.F_TWUB_CreatorId_qtr AS PayBillCreatorId,
        u.FName                AS PayBillCreatorName,
        u.FEmail               AS PayBillCreatorEmail,
        r.FDocumentStatus      AS DocumentStatus
    FROM T_WB_RECEIPT r WITH (NOLOCK)
    JOIN T_AP_PAYBILL p WITH (NOLOCK) ON r.FSrcBillNo = p.FBillNo
    LEFT JOIN T_SEC_USER u WITH (NOLOCK)
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

                var ps = new[]
                {
                    new SqlParam("@StartDate", KDDbType.DateTime, startDate),
                    new SqlParam("@Begin", KDDbType.Int32, begin),
                    new SqlParam("@End", KDDbType.Int32, end)
                };
                return ReadRows(ctx, sql, ps);
            }
        }

        /// <summary>统一读取并做字段安全转换</summary>
        private List<ReceiptInfoDto> ReadRows(Context ctx, string sql, SqlParam[] sqlParams)
        {
            try
            {
                var rows = DBUtils.ExecuteEnumerable(ctx, sql, CommandType.Text, sqlParams);
                var list = new List<ReceiptInfoDto>(capacity: 64);

                foreach (var row in rows)
                {
                    // 日期统一格式化
                    string receiptDateStr = null;
                    var raw = row["ReceiptDate"];
                    if (raw != null && raw != DBNull.Value)
                    {
                        if (raw is DateTime dt)
                            receiptDateStr = dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        else
                            receiptDateStr = raw.ToString();
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
                throw new Exception("Execute page sql failed", e);
            }
        }

        // -------------------- 探测/工具 --------------------

        /// <summary>探测是否支持 OFFSET/FETCH（SQL Server 2012+）</summary>
        private bool SupportsOffsetFetch(Context ctx)
        {
            const string probe = "SELECT 1 AS x ORDER BY x OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";
            try
            {
                DBUtils.ExecuteScalar(ctx, probe, CommandType.Text);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>兼容 K3 网关：{parameters:["json"]} 或直接 JSON</summary>
        private static T SafeDeserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var wrapper = JsonConvert.DeserializeObject<ParametersWrapper>(json);
                if (wrapper?.Parameters != null && wrapper.Parameters.Length > 0)
                {
                    var inner = wrapper.Parameters[0];
                    if (!string.IsNullOrWhiteSpace(inner))
                        return JsonConvert.DeserializeObject<T>(inner);
                }
            }
            catch
            {
                /* 非包装格式，走下一步 */
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
            if (ctx == null) throw new Exception("AppContext is null（未获取到金蝶应用上下文）");
            return ctx;
        }

        private static string GetDeepErrorMessage(Exception ex)
        {
            var msgs = new List<string>();
            int i = 0;
            while (ex != null && i++ < 10)
            {
                msgs.Add($"[{ex.GetType().FullName}] {ex.Message}");
                ex = ex.InnerException;
            }

            return string.Join(" -> ", msgs);
        }

        // -------------------- DTO --------------------

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