using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;

namespace projectresumeapi
{
    public static class Function1
    {

   

       public class Idy
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("name")]
            public string name { get; set; }

            [JsonProperty("image")]
            public string PartitionKey { get; set; }
            [JsonProperty("info")]
            public string Info { get; set; }
        }

        [FunctionName("ResumeInfoApi")]
        public static Task<List<Idy>> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
                    HttpRequest req,
          
           ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            List<Idy> FinalResult = new List<Idy>();        //creating a list for the branches
                                                            //passing the req Url parameters to variables
       

            Container LocationContainer = new CosmosClient("AccountEndpoint=https://resumeproject.documents.azure.com:443/;AccountKey=r7ycZk0G4y4Ri56MTaFeHjIkGBkUruYXwyDxa3VvGbSihBDUKuFtxgd6yXwJSW5NJ8STFIyyBLxJYx9I7ojOcw==;").GetContainer("resumest", "idy");

            foreach (var loc in LocationContainer.GetItemLinqQueryable<Idy>(true, null, null, null))
            {
                FinalResult.Add(loc);
            }
            return Task.FromResult(FinalResult);
        }
    }


}

