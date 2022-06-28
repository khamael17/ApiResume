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
using Microsoft.Extensions.Configuration;
using System.Configuration;

using System.Net.Mail;
using System.Configuration; // Namespace for ConfigurationManager
using System.Threading.Tasks; // Namespace for Task
//using Microsoft.Identity;
using Azure.Storage.Queues; // Namespace for Queue storage types
using Azure.Storage.Queues.Models;
using System.Net;
using System.Net.Mail;
namespace projectresumeapi
{
    public class Function
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

            string connectionString = "AccountEndpoint=https://resumedb29.documents.azure.com:443/;AccountKey=xjoMTB966cFHlEM6nF6foTlCUoRHZmcG898oAKziKF7wreKWZ2WImgs5OhKJ6fN7THTPL05bkWjUpPsUPuK6Ow==;";
            Container LocationContainer = new CosmosClient(connectionString).GetContainer("resumest", "info");

            foreach (var loc in LocationContainer.GetItemLinqQueryable<Idy>(true, null, null, null))
            {
                FinalResult.Add(loc);
            }
            return Task.FromResult(FinalResult);
        }


        public static class ConfigurationHelper
        {
            public static string GetByName(string configKeyName)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("local.settings.json")
                    .Build();

                IConfigurationSection section = config.GetSection(configKeyName);

                return section.Value;
            }
        }

    }

    public class Function1
    {
        [FunctionName("mailfunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];
            string message = req.Query["message"];
            string email = req.Query["email"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            email=email ?? data?.email;
            message = message ?? data?.message;
            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";


            // Get the connection string from app settings
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=storageresume1;AccountKey=Vw/0rvPnzFHJNjrYu9wH0IMMe+d8qWooGWqYqkCKefWRUTGXEagwhKezxBpJgJwHvZw2OfW3Epk7+AStR2FK8g==;EndpointSuffix=core.windows.net";
            string queueName = "messagequeue";
            // Instantiate a QueueClient which will be used to manipulate the queue
            QueueClient queueClient = new QueueClient(connectionString, queueName);

            // Create the queue if it doesn't already exist
            await queueClient.CreateIfNotExistsAsync();

            if (await queueClient.ExistsAsync())
            {
                Console.WriteLine($"Queue '{queueClient.Name}' created");
            }
            else
            {
                Console.WriteLine($"Queue '{queueClient.Name}' exists");
            }

            // Async enqueue the message
            await queueClient.SendMessageAsync($"{email}   {name}    {message}");
            Console.WriteLine($"Message added");

            // Async receive the message
            QueueMessage[] retrievedMessage = await queueClient.ReceiveMessagesAsync();
            Console.WriteLine($"Retrieved message with content '{retrievedMessage[0].Body}'");
            string messboday = retrievedMessage.ToString();
            messboday = getHtml(messboday, name);
            Email(messboday);

            //// Async delete the message
            //await queueClient.DeleteMessageAsync(retrievedMessage[0].MessageId, retrievedMessage[0].PopReceipt);
            //Console.WriteLine($"Deleted message: '{retrievedMessage[0].Body}'");

            //// Async delete the queue
            //await queueClient.DeleteAsync();
            //Console.WriteLine($"Deleted queue: '{queueClient.Name}'");





            //-------------------------------------------------
            // Peek at a message in the queue
            //-------------------------------------------------

            return new OkObjectResult(responseMessage);
        }





        //-------------------------------------------------
        // Create a message queue
        //-------------------------------------------------
        public bool CreateQueue(string queueName)
        {
            try
            {
                // Get the connection string from app settings
                string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

                // Instantiate a QueueClient which will be used to create and manipulate the queue
                QueueClient queueClient = new QueueClient(connectionString, queueName);

                // Create the queue
                queueClient.CreateIfNotExists();

                if (queueClient.Exists())
                {
                    Console.WriteLine($"Queue created: '{queueClient.Name}'");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Make sure the Azurite storage emulator running and try again.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}\n\n");
                Console.WriteLine($"Make sure the Azurite storage emulator running and try again.");
                return false;
            }
        }




        public void PeekMessage(string queueName)
        {
            // Get the connection string from app settings
            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

            // Instantiate a QueueClient which will be used to manipulate the queue
            QueueClient queueClient = new QueueClient(connectionString, queueName);

            if (queueClient.Exists())
            {
                // Peek at the next message
                PeekedMessage[] peekedMessage = queueClient.PeekMessages();

                // Display the message
                Console.WriteLine($"Peeked message: '{peekedMessage[0].Body}'");
            }
        }


        //-------------------------------------------------
        // Insert a message into a queue
        //-------------------------------------------------
        public void InsertMessage(string queueName, string message)
        {
            // Get the connection string from app settings
            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

            // Instantiate a QueueClient which will be used to create and manipulate the queue
            QueueClient queueClient = new QueueClient(connectionString, queueName);

            // Create the queue if it doesn't already exist
            queueClient.CreateIfNotExists();

            if (queueClient.Exists())
            {
                // Send a message to the queue
                queueClient.SendMessage(message);
            }

            Console.WriteLine($"Inserted: {message}");
        }

        public static string getHtml(string grid, string name)
        {
            try
            {
                string messageBody = "<font>The following are the records: </font><br><br>";
                if (grid.Length == 0) return messageBody;
                string htmlTableStart = "<table style=\"border-collapse:collapse; text-align:center;\" >";
                string htmlTableEnd = "</table>";
                string htmlHeaderRowStart = "<tr style=\"background-color:#6FA1D2; color:#ffffff;\">";
                string htmlHeaderRowEnd = "</tr>";
                string htmlTrStart = "<tr style=\"color:#555555;\">";
                string htmlTrEnd = "</tr>";
                string htmlTdStart = "<td style=\" border-color:#5c87b2; border-style:solid; border-width:thin; padding: 5px;\">";
                string htmlTdEnd = "</td>";
                messageBody += htmlTableStart;
                messageBody += htmlHeaderRowStart;
                messageBody += htmlTdStart + "Student Name" + name;
                messageBody += htmlTdStart + "DOB" + htmlTdEnd;
                messageBody += htmlTdStart + "Email" + htmlTdEnd;
                messageBody += htmlTdStart + "Mobile" + htmlTdEnd;
                messageBody += htmlHeaderRowEnd;
                //Loop all the rows from grid vew and added to html td  
                //for (int i = 0; i <= grid.RowCount - 1; i++)
                //{
                //    messageBody = messageBody + htmlTrStart;
                //    messageBody = messageBody + htmlTdStart + grid.Rows[i].Cells[0].Value + htmlTdEnd; //adding student name  
                //    messageBody = messageBody + htmlTdStart + grid.Rows[i].Cells[1].Value + htmlTdEnd; //adding DOB  
                //    messageBody = messageBody + htmlTdStart + grid.Rows[i].Cells[2].Value + htmlTdEnd; //adding Email  
                //    messageBody = messageBody + htmlTdStart + grid.Rows[i].Cells[3].Value + htmlTdEnd; //adding Mobile  
                //    messageBody = messageBody + htmlTrEnd;
                //}
                messageBody = messageBody + htmlTableEnd;
                return messageBody; // return HTML Table as string from this function  
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async static void Email(string htmlString)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.live.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("koproject@hotmail.com", "Travaille17$"),
                    EnableSsl = true,
                };

                smtpClient.Send("koproject@hotmail.com", "dieu1er@live.fr", "New recruiter", htmlString);
            }
            catch (Exception) { }
        }

        //-------------------------------------------------
        // Create the queue service client
        //-------------------------------------------------
        public void CreateQueueClient(string queueName)
        {
            // Get the connection string from app settings
            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

            // Instantiate a QueueClient which will be used to create and manipulate the queue
            QueueClient queueClient = new QueueClient(connectionString, queueName);
        }

    }

}