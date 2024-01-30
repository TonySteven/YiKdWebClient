using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YiKdWebClient.Model
{
    public class RequestWebModel
    {
        public CookieContainer Cookie { get; set; } = new CookieContainer();
        public string RequestUrl { get; set; } = string.Empty;

        public string RealRequestBody { get; set; } = string.Empty;     
        public string RealResponseBody { get; set; } = string.Empty;
    }
}
