using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Services;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Net;
using Microsoft.Azure.Cosmos;
using System.Net.Sockets;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;
using Azure;
using System.Text;
using System.Diagnostics.Metrics;
using System.Reflection.Emit;

namespace AzureFunctions
{
    public static class DeliveryOrderProcessor
    {
        [FunctionName("DeliveryOrderProcessor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            //[CosmosDB(
            //    databaseName: "DeliveryDb",
            //    collectionName: "ContainerDeliveryDb",
            //    ConnectionStringSetting = "AccountEndpoint=https://deliveryeshop.documents.azure.com:443/;AccountKey=4ZjKCVIW1QlUNS7VfqSqGIEZ3oCu9pJnEJOigGqSKbIbxTiIDEjp9KhQ6YJPJVzi5Ryq1j5tAxtnSXMDq4IrrQ==")]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {

            

            log.LogInformation("C# HTTP trigger function DeliveryOrderProcessor processed a request.");

            //string name = req.Query["name"];

            //var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            //var order = JsonSerializer.Deserialize<DeliveryOrder>(body);
            var order = JsonConvert.DeserializeObject<DeliveryOrder>(body);

            //dynamic order = JsonConvert.DeserializeObject(body);

            //var testOrder = new DeliveryOrder
            //{
            //    Address = new Address("123 Main St.", "Kent", "OH", "United States", "44240"),
            //    Id = Guid.NewGuid().ToString(),
            //    Sum = (decimal)36.50,
            //    OrderItems = new List<OrderItem>()
            //    {
            //        new OrderItem(new CatalogItemOrdered(2, ".NET Black \\u0026 White Mug", "/images/products/2.png"), (decimal)8.50, 2),
            //        new OrderItem(new CatalogItemOrdered(1, ".NET Bot Black Sweatshirt", "/images/products/1.png"), (decimal)19.50, 1)
            //    }
            //};

            //var testOrder2 = new TestOrder()
            //{
            //    Address = "address",
            //    Id = "id1",
            //    Sum = 100,
            //    Items = new List<TestOrderItem>()
            //    {
            //        new TestOrderItem() { Price = 10, Units = 5 },
            //        new TestOrderItem() { Price = 5, Units = 10 },
            //    }
            //};


            CosmosClient client = new CosmosClient("https://deliveryeshop.documents.azure.com:443/",
                "rYj8AZVFuWLiRXdzyrQQdtbXErPkgCDjgQcVLikTZr4jaPdmVSf0apvQ7MkAaJw8SnNC8qdRUtO1ACDbTxJsqg==");

            //

            //var database = client.GetDatabase("DeliveryDb");

            var container = client.GetContainer("DeliveryDb", "ContainerDeliveryDb");

            //await container.CreateItemAsync(order, new PartitionKey(Guid.NewGuid().ToString()));

            //var id = Guid.NewGuid().ToString();

            //await container.CreateItemAsync<DeliveryOrder>(order, new PartitionKey(order.Address.State));
            await container.CreateItemAsync<DeliveryOrder>(order);
            //await container.CreateItemAsync<DeliveryOrder>(testOrder, new PartitionKey(testOrder.Id));
            //await container.CreateItemAsync<TestOrder>(testOrder2, new PartitionKey(testOrder2.Id));



            //name = name ?? data?.name;

            //if (!string.IsNullOrEmpty(name))
            //{
            // Add a JSON document to the output container.
            //await documentsOut.AddAsync(new
            //    {
            //        // create a random ID
            //        id = Guid.NewGuid().ToString(),
            //        order = order
            //    });
            //}

            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function DeliveryOrderProcessor executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

            string responseMessage =
                "This HTTP triggered function DeliveryOrderProcessor executed successfully. Pass a name in the query string or in the request body for a personalized response.";

            return new OkObjectResult(responseMessage);
        }

        private class TestOrder
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public string Address { get; set; }
            public double Sum { get; set; }

            public List<TestOrderItem> Items { get; set; }
        }

        private class TestOrderItem
        {
            public double Price { get; set; }
            public int Units { get; set; }
        }
    }
}
