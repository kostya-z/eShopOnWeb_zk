using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Xml.Linq;
using Azure;
using Azure.Storage.Blobs.Models;
using System.Reflection.PortableExecutable;
using System.Web.Http;
using Azure.Core;
using Microsoft.eShopWeb.ApplicationCore.Services;
using Microsoft.Azure.Cosmos;

namespace AzureFunctions
{
    public static class OrderItemsReserverService
    {
        private static int maxAttempts = 3;

        [FunctionName("OrderItemsReserverService")]
        //[FixedDelayRetry(3, "00:00:30")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            //log.LogInformation("C# HTTP trigger function processed a request.");

            string blobName = req.Query["orderId"];

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

            //return new OkObjectResult(responseMessage);

            #region Retry approach 1

            //var attempt = 0;

            //var shouldContinue = true;

            //while (shouldContinue)
            //{
            //    try
            //    {
            //        OrderService.ReservedItem item;
            //        using (var reader = new StreamReader(req.Body))
            //        {
            //            var body = await reader.ReadToEndAsync();

            //            item = JsonSerializer.Deserialize<OrderService.ReservedItem>(body);
            //        }


            //        await Save(blobName, item);

            //        shouldContinue = false;

            //    }
            //    catch (Exception ex)
            //    {
            //        attempt++;

            //        shouldContinue = attempt < maxAttempts;

            //        if (shouldContinue) continue;

            //        SentEmail(ex);

            //        return new BadRequestErrorMessageResult($"{ex.Message}. StackTrace: {ex.StackTrace}. Inner: {ex.InnerException}");
            //    }
            //}

            //return new OkObjectResult("This HTTP triggered function executed successfully: item is reserved");
            #endregion

            #region without retries

            try
            {
                OrderService.ReservedItem item;
                using (var reader = new StreamReader(req.Body))
                {
                    var body = await reader.ReadToEndAsync();

                    item = JsonSerializer.Deserialize<OrderService.ReservedItem>(body);
                }

                await Save(blobName, item);

            }
            catch (Exception ex)
            {
                return new BadRequestErrorMessageResult($"{ex.Message}. StackTrace: {ex.StackTrace}. Inner: {ex.InnerException}");
            }

            return new OkObjectResult("This HTTP triggered function executed successfully: item is reserved");
            #endregion

        }

        private static void SentEmail(Exception exception)
        {
            throw new NotImplementedException();
        }

        private static async Task Save(string blobName, OrderService.ReservedItem item)
        {
            var blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=storageaccounteshop;AccountKey=xIKoULSg2ijExYugFvxuiwuLuaqKQrjBdA2Dkdrh7qAXJZ5BNkD8wjSZTI+lsyX2H3yYBSFRIVqX+AStlFcz6w==;EndpointSuffix=core.windows.net");

            // Get the container (folder) the file will be saved in
            var containerClient = blobServiceClient.GetBlobContainerClient("blobcontainer");

            // Get the Blob Client used to interact with (including create) the blob
            var blobClient = containerClient.GetBlobClient($"{blobName}.json");

            var jsonstr = JsonSerializer.Serialize(item);
            byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(jsonstr);

            // Upload the blob
            await blobClient.UploadAsync(new MemoryStream(byteArray));
        }

        //private class ReservedItem
        //{
        //    public string ItemId { get; set; }
        //    public int Quantity { get; set; }
        //}
    }
}
