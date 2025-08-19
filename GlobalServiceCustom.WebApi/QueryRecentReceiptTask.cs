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
    /// WebAPI: 查询近 N 天电子回单（不分页）
    /// 入参 JSON: { "days": 7, "maxRows": 1000 }
    /// </summary>
    public class QueryRecentReceiptTask : AbstractWebApiBusinessService
    {
        // -------------------- 常量与配置 --------------------
        private const int DEFAULT_DAYS = 7;
        private const int DEFAULT_MAX_ROWS = 1000;
        private const int MAX_ALLOWED_ROWS = 5000; // 兜底上限，避免一次返回过多

        public QueryRecentReceiptTask(KDServiceContext context) : base(context)
        {
        }

        /// <summary>
        /// WebAPI 入口（不分页）
        /// </summary>
        /// <param name="json">{"days":7,"maxRows":1000} 或 {"parameters":["{\"days\":7,\"maxRows\":1000}"]}</param>
        public object ExecuteService(string json)
        {
            try
            {
                // 1) 解析入参
                var input = SafeDeserialize<RequestParam>(json) ?? new RequestParam();

                // 2) 归一化入参（兜底 + 边界）
                int days = input.Days > 0 ? input.Days : DEFAULT_DAYS;

                int maxRows = input.MaxRows > 0 ? input.MaxRows : DEFAULT_MAX_ROWS;
                if (maxRows > MAX_ALLOWED_ROWS) maxRows = MAX_ALLOWED_ROWS;

                // 3) 时间窗口（近 N*24h；若要“包含当天自然日”可改为 Now.Date.AddDays(-(days-1))）
                var startDate = DateTime.Now.AddDays(-days);

                // 4) 获取上下文
                var ctx = EnsureAppContext();

                // 5) 查询数据（不分页，TOP @MaxRows）
                var data = GetRecentReceiptsNoPaging(ctx, startDate, maxRows);

                // 6) 返回
                return ResponseDto.Success(days, data?.Count ?? 0, data);
            }
            catch (Exception ex)
            {
                return ResponseDto.Fail("Query failed", GetDeepErrorMessage(ex));
            }
        }

        // -------------------- 数据访问（不分页） --------------------

        /// <summary>
        /// 查询近 N 天内的电子回单（不分页，TOP @MaxRows）
        /// </summary>
        private List<ReceiptInfoDto> GetRecentReceiptsNoPaging(Context ctx, DateTime startDate, int maxRows)
        {
            // 说明：
            // - 仍然关联 AP 付款单以过滤业务类型（2、5）；如果你们环境里没有该字段或表，请对应调整。
            // - 用户表关联建议按“主键或唯一ID”关联，这里沿用你的字段示例；实际以贵司库结构为准。
            const string sql = @"
SELECT TOP (@MaxRows)
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
    -- ⚠️ 建议按用户唯一标识关联（如 FUSERID）；当前以 FName 匹配风险较高，按你们库结构调整
    ON CAST(p.F_TWUB_CreatorId_qtr AS NVARCHAR(50)) = u.FName
WHERE r.FDocumentStatus = 'C'
  AND r.FDate >= @StartDate
  AND p.FBusinessType IN (2, 5)
ORDER BY r.FDate DESC;";

            var ps = new[]
            {
                new SqlParam("@StartDate", KDDbType.DateTime, startDate),
                new SqlParam("@MaxRows", KDDbType.Int32, maxRows)
            };

            try
            {
                var rows = DBUtils.ExecuteEnumerable(ctx, sql, CommandType.Text, ps);
                var list = new List<ReceiptInfoDto>();

                foreach (var row in rows)
                {
                    // 统一转换日期
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
                throw new Exception("Execute no-paging sql failed", e);
            }
        }

        // -------------------- 工具方法 --------------------

        /// <summary>
        /// 兼容 K3 网关传入的 {parameters: ["json"]} 与直接 JSON 字符串
        /// </summary>
        private static T SafeDeserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                // 兼容网关把参数包进 parameters 数组的格式
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
                // 忽略，继续尝试直接反序列化
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

        private static string GetDeepErrorMessage(Exception ex)
        {
            var msgs = new List<string>();
            int depth = 0;
            while (ex != null && depth < 10)
            {
                msgs.Add($"[{ex.GetType().FullName}] {ex.Message}");
                ex = ex.InnerException;
                depth++;
            }

            return string.Join(" -> ", msgs);
        }

        // -------------------- DTO 定义 --------------------

        private class ParametersWrapper
        {
            [JsonProperty("parameters")] public string[] Parameters { get; set; }
        }

        private class RequestParam
        {
            [JsonProperty("days")] public int Days { get; set; }

            /// <summary>
            /// 最大返回条数（默认 1000，上限 5000）
            /// </summary>
            [JsonProperty("maxRows")]
            public int MaxRows { get; set; }
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
            public int TotalCount { get; set; }
            public List<ReceiptInfoDto> Data { get; set; }
            public string Message { get; set; }
            public string Detail { get; set; }

            public static ResponseDto Success(int days, int totalCount, List<ReceiptInfoDto> data)
            {
                return new ResponseDto
                {
                    IsSuccess = true,
                    QueryDays = days,
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