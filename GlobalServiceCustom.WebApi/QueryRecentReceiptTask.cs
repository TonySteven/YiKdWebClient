using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.WebApi.ServicesStub;

namespace GlobalServiceCustom.WebApi
{
    /// <summary>
    /// WebAPI: Query recent electronic receipts within N days
    /// - Default: 7 days
    /// - Customizable days parameter
    /// - Returns receipt info with creator details
    /// </summary>
    public class QueryRecentReceiptTask : AbstractWebApiBusinessService
    {
        public QueryRecentReceiptTask(KDServiceContext context) : base(context)
        {
        }

        /// <summary>
        /// WebAPI Entry
        /// </summary>
        /// <param name="days">Number of days to query (default: 7)</param>
        public object ExecuteService(int days = 7)
        {
            try
            {
                if (days <= 0) days = 7;

                var result = GetRecentReceipts(days);

                return new
                {
                    IsSuccess = true,
                    QueryDays = days,
                    ResultCount = result.Count,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new { IsSuccess = false, Message = $"Query failed: {ex.Message}" };
            }
        }

        /// <summary>
        /// Query recent electronic receipts
        /// </summary>
        private List<ReceiptInfoDto> GetRecentReceipts(int days)
        {
            var ctx = KDContext.Session.AppContext;
            if (ctx == null)
            {
                throw new Exception("AppContext is null");
            }

            // ✅ Optimized field aliases to English & PascalCase
            var sql = @"
SELECT
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
  AND p.FBusinessType IN (2, 5)";

            var sqlParams = new SqlParam[]
            {
                new SqlParam("@Days", KDDbType.Int32, days)
            };

            var rows = DBUtils.ExecuteEnumerable(ctx, sql, CommandType.Text, sqlParams);

            // ✅ Map to C# standard property names
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
        /// DTO: Receipt information
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