using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AdfsSetbusinessUnit
{
    class Program
    {


       static string us;
        static string pwd;
        static async Task Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            //The SSL connection could not be established, see inner exception. The remote certificate is invalid according to the validation procedure.


            Console.WriteLine("Hello World!");

            Console.WriteLine("input username：");
             us = Console.ReadLine();
            Console.WriteLine("input password：");
             pwd = Console.ReadLine();

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            var token = await GetToken(us, pwd);
            HttpClient client2 =await InitHttpClient(token);
        
          

            var result = await client2.GetAsync("https://crm.demo.local:5555//api/data/v8.2/systemusers?$select=jobtitle,lf_id&$filter=isdisabled eq true").Result.Content.ReadAsStringAsync();


            var listUser = new List<systemuser>();
            var nextLink = JObject.Parse(result).GetValue("@odata.nextLink").ToString();

            var systemusers = JsonConvert.DeserializeObject<systemusers>(result);

            listUser.AddRange(systemusers.value);

            while (string.IsNullOrEmpty(nextLink) != true)
            {

                Console.WriteLine($"load user :{nextLink}");

                result = await client2.GetAsync(nextLink).Result.Content.ReadAsStringAsync();

                nextLink = JObject.Parse(result).GetValue("@odata.nextLink")?.ToString();

                systemusers = JsonConvert.DeserializeObject<systemusers>(result);

                listUser.AddRange(systemusers.value);

                Console.WriteLine($"load user count :{listUser.Count}");

            }

            var baseUrl = "https://crm.demo.local:5555//api/data/v9.0/";
            var i = 1;

            foreach (var item in listUser)
            {
                if (token.expires_in_date <= DateTime.Now) {
                    token = await GetToken(us,pwd);
                    client2 = await InitHttpClient(token);
                }
                

                Console.WriteLine($"set user userid:{item.systemuserid}, businessunitid:{item.jobtitle}");
                //查询buid
                var webURl = $"{baseUrl}businessunits?$select=lf_id&$filter=lf_id eq '{item.jobtitle}'";

                result = await client2.GetStringAsync(webURl);
                try
                {
                    var buid = JsonConvert.DeserializeObject<businessunits>(result).value[0]?.businessunitid;
                    Console.WriteLine($"businessunitid:{buid}");
                    //启用用户
                    var systemuserUpdate = new systemuserUpdate() { isdisabled = false };
                    var content2 = new StringContent(JsonConvert.SerializeObject(systemuserUpdate));
                    content2.Headers.ContentType = new MediaTypeHeaderValue(@"application/json");

                    webURl = $"{baseUrl}systemusers({item.systemuserid})";

                    var message = await client2.PatchAsync(webURl, content2);
                    Console.WriteLine($"Enable systemuser PatchAsync:{message.StatusCode}  IsSuccessStatusCode:{message.IsSuccessStatusCode}");

                    if (string.IsNullOrWhiteSpace(buid) == false)
                    {
                        //更改用户buid
                        webURl = $"{baseUrl}systemusers({item.systemuserid})/Microsoft.Dynamics.CRM.SetBusinessSystemUser()";
                        content2 = new StringContent("{\"BusinessUnit\":{\"businessunitid\":\"" + buid + "\",\"@odata.type\":\"Microsoft.Dynamics.CRM.businessunit\"},\"ReassignPrincipal\":{\"systemuserid\":\"" + item.systemuserid + "\",\"@odata.type\":\"Microsoft.Dynamics.CRM.systemuser\"}}");
                        content2.Headers.ContentType = new MediaTypeHeaderValue(@"application/json");


                        message = await client2.PostAsync(webURl, content2);

                        Console.WriteLine($"SetBusinessSystemUser PostAsync:{message.StatusCode} IsSuccessStatusCode:{message.IsSuccessStatusCode} ,count:{i}");

                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    //throw;
                }
                finally {
                    i++;
                }
                
               


            }
            Console.ReadKey();





        }


        static async Task<adfsToken> GetToken(string username,string passwork) {
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
                new KeyValuePair<string,string>("username",username),

                new KeyValuePair<string,string>("password",passwork),
                new KeyValuePair<string,string>("grant_type","password"),
                         }); ; ;

            var res = await _httpClient.PostAsync("https://adfs.demo.local/adfs/oauth2/token", content);

            var adfsToken = JsonConvert.DeserializeObject<adfsToken>(await res.Content.ReadAsStringAsync());

            return adfsToken;
        }

        static async Task<HttpClient> InitHttpClient(adfsToken token)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            HttpClient client2 = new HttpClient(clientHandler);
            
            if (token.expires_in_date > DateTime.Now)
            {
                client2.DefaultRequestHeaders.Add("Authorization", "Bearer " + token.access_token);
            }
            else
            {
                token = await GetToken(us,pwd);
                client2 = new HttpClient(clientHandler);
                client2.DefaultRequestHeaders.Add("Authorization", "Bearer " + token.access_token);
                
            }

            return client2;
        }

    }

    public class adfsToken
    {

        public string access_token { get; set; }
        public string token_type { get; set; }


        private string _expires_in;
        public string expires_in
        {
            get
            {
                return _expires_in;
            }
            set
            {
                _expires_in = value;
                this.expires_in_date = DateTime.Now.AddSeconds(int.Parse(value));

            }
        }

        public string resource { get; set; }

        public string refresh_token { get; set; }

        public string refresh_token_expires_in { get; set; }

        public string id_token { get; set; }


        public DateTime expires_in_date { set; get; }


    }


    public class systemuserUpdate
    {
        public bool isdisabled { get; set; }
    }
    public class systemuser
    {
        public string jobtitle { get; set; }
        public string lf_id { get; set; }

        public string systemuserid { get; set; }

        public string ownerid { get; set; }

    }
    public class systemusers
    {
        public List<systemuser> value { get; set; } = new List<systemuser>();

        public string nextLink { get; set; }

    }

    public class businessunit
    {
        public string businessunitid { get; set; }

    }

    public class businessunits
    {
        public List<businessunit> value { get; set; } = new List<businessunit>();
    }



    public class SetBusinessSystemUserModel
    {

    }


}
