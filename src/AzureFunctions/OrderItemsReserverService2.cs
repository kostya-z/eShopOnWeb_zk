using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Http;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AzureFunctions
{
    public class OrderItemsReserverService2
    {
        //const string ServiceBusConnectionString = "Endpoint=sb://nsservicebus002.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=/mZSD3EtSgmCqUGt2uaGIiUrlcDRM1B6yjhup4j1GsI=";
        const string QueueName = "warehouse";
        const string TopicName = "reserveorder";
        private const string SubscriptionName = "reserveorderSuscription";
        private const string BlobConnectionString = "DefaultEndpointsProtocol=https;AccountName=blobstorageaccount004;AccountKey=xTaTCTF+5bVSv7InMIkSZtd66TRbDLSHfLND4VM2+i7BWYyfwejupkHjoce2eFuq34smg3UK0gDQ+AStRiDzfQ==;EndpointSuffix=core.windows.net";
        private const string BlobContainerName = "reservedorders";

        private const int MAX_ATTEMPTS = 3;

        [FunctionName("OrderItemsReserverService2")]
        public static async Task Run([ServiceBusTrigger(topicName: TopicName, subscriptionName: SubscriptionName, Connection = "ServiceBusConnectionString")] string myQueueItem,
            ILogger log)
        //[ServiceBusTrigger(QueueName, Connection = ServiceBusConnectionString)]
        //[ServiceBusTrigger(topicName: TopicName, subscriptionName: SubscriptionName, Connection = ServiceBusConnectionString)]
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");

            #region Receive message not based on the event
            //const string ServiceBusConnectionString = "Endpoint=sb://nsservicebus001.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=unW6AMBtVFHLcPOb3vnqsF9LgrAZ4D4QsIGhBqp58R0=";
            //const string QueueName = "warehouse";

            //// Create a ServiceBusClient object using the connection string to the namespace.
            //await using var client = new ServiceBusClient(ServiceBusConnectionString);

            //var processorOptions = new ServiceBusProcessorOptions
            //{
            //    MaxConcurrentCalls = 1,
            //    AutoCompleteMessages = false
            //};

            //// Create a ServiceBusProcessor for the queue.
            //await using ServiceBusProcessor processor = client.CreateProcessor(QueueName, processorOptions);

            //// Specify handler methods for messages and errors.
            //processor.ProcessMessageAsync += MessageHandler;
            //processor.ProcessErrorAsync += ErrorHandler;

            //await processor.StartProcessingAsync();

            //await processor.CloseAsync();
            #endregion

            #region Receive message based on the event

            await ProcessReserveOrder(myQueueItem);

            #endregion
        }


        // handle received messages
        static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            Console.WriteLine($"Received: {body}");

            // complete the message. messages is deleted from the queue. 
            await args.CompleteMessageAsync(args.Message);
        }

        // handle any errors when receiving messages
        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        private static async Task<IActionResult> ProcessReserveOrder(string myQueueItem)
        {
            //var order = JsonConvert.DeserializeObject<List<OrderService.ReservedItem>>(myQueueItem);

            var blobName = Guid.NewGuid().ToString();

            #region Retry approach 1

            var attempt = 0;

            var shouldContinue = true;

            while (shouldContinue)
            {
                try
                {
                    //throw new Exception("test exception");

                    await SaveInBlob(blobName, myQueueItem);

                    shouldContinue = false;
                }
                catch (Exception ex)
                {
                    attempt++;

                    shouldContinue = attempt < MAX_ATTEMPTS;

                    if (shouldContinue) continue;

                    await SendEmail(myQueueItem, ex);

                    return new BadRequestErrorMessageResult($"{ex.Message}. StackTrace: {ex.StackTrace}. Inner: {ex.InnerException}");
                }
            }

            return new OkObjectResult("This HTTP triggered function executed successfully: item is reserved");
            #endregion

        }

        private static async Task SaveInBlob(string blobName, string order)
        {
            var blobServiceClient = new BlobServiceClient(BlobConnectionString);

            // Get the container (folder) the file will be saved in
            var containerClient = blobServiceClient.GetBlobContainerClient(BlobContainerName);

            // Get the Blob Client used to interact with (including create) the blob
            var blobClient = containerClient.GetBlobClient($"{blobName}.json");

            //var jsonstr = JsonSerializer.Serialize(item);
            byte[] byteArray = Encoding.ASCII.GetBytes(order);

            // Upload the blob
            await blobClient.UploadAsync(new MemoryStream(byteArray));
        }

        private static async Task SendEmail(string myQueueItem, Exception exception)
        {
            var logicAppUri = @"https://prod-31.centralus.logic.azure.com:443/workflows/fd0e86273b184cf3becc449e0edb5402/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=4wjsYvVr2mICQa8LYk83rn2dpr5Ty5RweDzqcVvQPRQ";
            var httpClient = new HttpClient();

            var response = await httpClient.PostAsync(logicAppUri, new StringContent(myQueueItem, Encoding.UTF8, "application/json"));

        }
    }
}
