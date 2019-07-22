using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AdfsSetbusinessUnit
{
    class Program
    {


        static async Task Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            //The SSL connection could not be established, see inner exception. The remote certificate is invalid according to the validation procedure.


            Console.WriteLine("Hello World!");
            var client = new RestClient("https://adfs.demo.local/adfs/oauth2/token");
            var request = new RestRequest(Method.POST);
         
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("undefined", "client_id=4d482433-0b9f-4082-9837-b745c8ce9e5d&client_secret=nETud9tWXmAno1q6_UOEKNOuQLwWk1etFJVTK3SW&resource=https%3A%2F%2Fcrm.demo.local%3A5555%2Fapi%2Fdata%2Fv9.0%2F&username=demo%5Cadministrator&password=&grant_type=password", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            var obj = JsonConvert.DeserializeObject<adfsToken>( response.Content);

            var access_token = obj.access_token;

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            // Pass the handler to httpclient(from you are calling api)
            //var client = new HttpClient(clientHandler)
            HttpClient _httpClient = new HttpClient(clientHandler);
            _httpClient.BaseAddress = new Uri("https://crm.demo.local:5555/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            _httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");

            var content = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string,string>("client_id","4d482433-0b9f-4082-9837-b745c8ce9e5d"),
                new KeyValuePair<string,string>("client_secret","nETud9tWXmAno1q6_UOEKNOuQLwWk1etFJVTK3SW"),
                new KeyValuePair<string,string>("resource","https://crm.demo.local:5555/api/data/v9.0"),
                new KeyValuePair<string,string>("username",@"demo\crmadmin"),
                new KeyValuePair<string,string>("password",""),
                new KeyValuePair<string,string>("grant_type","password"),
                         });
           
            var res = _httpClient.PostAsync("https://adfs.demo.local/adfs/oauth2/token", content);
            var respo = res.Result.Content.ReadAsStringAsync().Result;
            var accesstoken = JObject.Parse(respo).GetValue("access_token").ToString();

            var myUri = new Uri("https://crm.demo.local:5555/api/data/v9.0/accounts?$top=1");
            var myWebRequest = WebRequest.Create(myUri);
            var myHttpWebRequest = (HttpWebRequest)myWebRequest;
            myHttpWebRequest.PreAuthenticate = true;
            myHttpWebRequest.Headers.Add("Authorization", "Bearer " + accesstoken);
            myHttpWebRequest.Accept = "application/json";

            var myWebResponse = myWebRequest.GetResponse();
            var responseStream = myWebResponse.GetResponseStream();

            var myStreamReader = new StreamReader(responseStream, Encoding.Default);
            var json = myStreamReader.ReadToEnd();

            responseStream.Close();
            myWebResponse.Close();
          
          
        }
    }

    public class adfsToken {

        public string access_token { get; set; }
        public string token_type { get; set; }

        public string expires_in { get; set; }

        public string resource { get; set; }

        public string refresh_token { get; set; }

        public string refresh_token_expires_in { get; set; }

        public string id_token { get; set; }


    }


}
