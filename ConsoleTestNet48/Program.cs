using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using YiKdWebClient;

namespace ConsoleTestNet48
{
    internal class Program
    {
        static void Main(string[] args)
        {
           
            string Formid = "SEC_User";
            string Json = @"{""IsUserModelInit"":""true"",""Number"":""Administrator"",""IsSortBySeq"":""false""}";
            YiK3CloudClient yiK3CloudClient = new YiKdWebClient.YiK3CloudClient();
            var resultJson = yiK3CloudClient.View(Formid, Json);
            Console.WriteLine(resultJson);
            Console.ReadKey();

            
        }
    }
}
