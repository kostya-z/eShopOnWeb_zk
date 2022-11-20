using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Newtonsoft.Json;

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class DeliveryOrder 
{
    public DeliveryOrder(Order order)
    {
        OrderItems = order.OrderItems.ToList();
        Address = order.ShipToAddress;
        Sum = order.OrderItems.Sum(item => item.UnitPrice * item.Units);
        Id = Guid.NewGuid().ToString();
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public DeliveryOrder()
    {
        
    }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    //[JsonPropertyName("address")]
    public Address Address { get; set; }

    //[JsonPropertyName("sum")]
    public decimal Sum { get; set; }

    //[JsonPropertyName("items")]
    public List<OrderItem> OrderItems { get; set; }
}
