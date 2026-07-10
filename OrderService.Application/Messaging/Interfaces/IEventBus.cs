using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Messaging.Interfaces;

public interface IEventBus
{
    Task PublishAsync<T>(string queueName, T message);
}
