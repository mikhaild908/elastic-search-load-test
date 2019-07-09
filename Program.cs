using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Linq;

namespace Test_Review
{
    class Program
    {
        const string POST_URL = "http://www.fabletics.com.mdumlao.fldev.techstyle.tech/test-SearchOrderLineGeo.cfm";
        static string _dateTimeSoldMin = "2019-01-01";
        static int _numberOfRuns = 0;

        static async Task Main(string[] args)
        {
            Console.Write("Number of runs: ");
            _numberOfRuns = Int32.Parse(Console.ReadLine());

            Console.Write("Enter datetime_sold_min(ex. 2019-01-01): ");
            _dateTimeSoldMin = Console.ReadLine();
            Console.Write("\n");

            var list = await GetAllDataAsync();

            Console.WriteLine("\nComparing data...");

            var dateComparisonResults = new List<bool>();

            foreach (var item in list)
            {
                dateComparisonResults.Add(CompareDates(item));
            }

            if (dateComparisonResults.All(x => x == true))
            {
                Console.WriteLine("\nAll dates(datetime_sold) on the built queries are correct.");
            }
            else
            {
                Console.WriteLine("\nThere's a problem with the returned date(s) on the built queries");
            }
        }

        static async Task<string> GetDataAsync(int id)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var json = "{\"page\":1,\"size\":1,\"store_group_id\":16,\"datetime_sold_min\":\"{_dateTimeSoldMin}\",\"latitude\":33.7866,\"longitude\":-118.299,\"radius\":500,\"radius_unit\":\"mi\",\"data_format\": \"array\",\"format\":\"json\",\"debug\":true}"
                    .Replace("{_dateTimeSoldMin}", _dateTimeSoldMin);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"Retrieving data for ID: {id}");

                HttpResponseMessage result = await client.PostAsync(POST_URL, content);

                if (result.StatusCode == HttpStatusCode.Unauthorized) { }

                var results = await result.Content.ReadAsStringAsync();

                Console.WriteLine($"Completed retrieval of response for ID: {id}");

                return results;
            }
        }

        static bool CompareDates(EsResult esResult)
        {
            //{"datetime_sold":{"gte":"2019-01-01T00:00:00"}
            //Console.WriteLine($"Request Body: {esResult.data.input.requestbody}");
            var dateTimeSoldIndex = esResult.data.input.requestbody.IndexOf("datetime_sold", StringComparison.CurrentCulture);
            var timeIndex = esResult.data.input.requestbody.IndexOf("T00:00:00", dateTimeSoldIndex, StringComparison.CurrentCulture);

            var dateTimeSold = esResult.data.input.requestbody.Substring(dateTimeSoldIndex, timeIndex - dateTimeSoldIndex);
            dateTimeSold = dateTimeSold.Substring(dateTimeSold.LastIndexOf('"') + 1);

            var comparisonOperator = dateTimeSold == _dateTimeSoldMin ? "==" : "<>";
            Console.WriteLine($"param {_dateTimeSoldMin} {comparisonOperator} parsed from query {dateTimeSold}");

            return dateTimeSold == _dateTimeSoldMin;
        }

        static async Task<List<EsResult>> GetAllDataAsync()
        {
            Console.WriteLine("Posting data to server...\n");

            var list = new List<EsResult>();
            var tasks = new List<Task<string>>();

            foreach (var i in Enumerable.Range(1, _numberOfRuns))
            {
                tasks.Add(GetDataAsync(i));
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                list.Add(JsonConvert.DeserializeObject<EsResult>(task.Result));
            }

            return list;
        }
    }
}
