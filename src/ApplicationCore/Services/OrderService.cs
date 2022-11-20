using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);

        //await ReserveOrderItems_using_Function(order);

        await ReserveOrderItems_using_ServiceBus(order);

        await DeliveryOrderProcess(order);
    }

    private async Task ReserveOrderItems_using_ServiceBus(Order order)
    {
        const string ServiceBusConnectionString = "Endpoint=sb://nsservicebus002.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=/mZSD3EtSgmCqUGt2uaGIiUrlcDRM1B6yjhup4j1GsI=";
        const string QueueName = "warehouse";
        const string TopicName = "reserveorder";

        // Create a ServiceBusClient object using the connection string to the namespace.
        await using var client = new ServiceBusClient(ServiceBusConnectionString);

        // Create a ServiceBusSender object by invoking the CreateSender method on the ServiceBusClient object, and specifying the queue name. 
        //ServiceBusSender sender = client.CreateSender(QueueName);
        ServiceBusSender sender = client.CreateSender(TopicName);

        try
        {
            var items = order.OrderItems.Select(x => new ReservedItem
                { ItemId = x.Id, Quantity = x.Units }).ToList();

            var jsonstr = JsonSerializer.Serialize(items);


            // Create a new message to send to the queue.
            //string messageContent = "Order new crankshaft for eBike.";
            var message = new ServiceBusMessage(jsonstr);

            // Send the message to the queue.
            await sender.SendMessageAsync(message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            throw;
        }
        finally
        {
            // Calling DisposeAsync on client types is required to ensure that network
            // resources and other unmanaged objects are properly cleaned up.
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }
    }


    private static async Task ReserveOrderItems_using_Function(Order order)
    {
        var url = $"https://azurefunctions20221012202942.azurewebsites.net/api/OrderItemsReserverService?orderid={order.Id}";

        var items = order.OrderItems.Select(x => new ReservedItem
        { ItemId = x.Id, Quantity = x.Units }).ToList();

        var jsonstr = JsonSerializer.Serialize(items);
        byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(jsonstr);

        using var client = new HttpClient();
        //await client.PostAsync(url, new StringContent(jsonstr, Encoding.UTF8, "application/json"));
        await client.PostAsync(url, new ByteArrayContent(byteArray));

        //foreach (var item in order.OrderItems)
        //{
        //    var jsonstr = JsonSerializer.Serialize(item.ItemOrdered);}

        //    using var client = new HttpClient();
        //    await client.PostAsync(url, new StringContent(jsonstr, Encoding.UTF8, "application/json"));

        //    //new HttpClient().PostAsync("http://...", new JsonContent(new { x = 1, y = 2 }));
        //}
    }

    private async Task DeliveryOrderProcess(Order order)
    {
        try
        {
            var url = $"https://azurefunctions20221031203809.azurewebsites.net/api/DeliveryOrderProcessor?orderid={order.Id}";

            //var deliveryOrder = order.OrderItems.Select(x => new DeliveryOrder(order));
            var deliveryOrder = new DeliveryOrder(order);

            var jsonstr = JsonSerializer.Serialize(deliveryOrder);
            //byte[] byteArray = Encoding.ASCII.GetBytes(jsonstr);

            var test = JsonSerializer.Deserialize<DeliveryOrder>(jsonstr);


            using var client = new HttpClient();
            var result = await client.PostAsync(url, new StringContent(jsonstr, Encoding.UTF8, "application/json"));
            //await client.PostAsync(url, new ByteArrayContent(byteArray));

            result.EnsureSuccessStatusCode();


            //using var client = new HttpClient();
            //var postResponse = await client.PostAsJsonAsync(url, deliveryOrder);
            //postResponse.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public class ReservedItem
    {
        //[JsonProperty(PropertyName = "id")]
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
