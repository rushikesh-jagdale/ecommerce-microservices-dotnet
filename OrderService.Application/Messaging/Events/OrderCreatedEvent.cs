using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Messaging.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public Guid UserId { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
}
