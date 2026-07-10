using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.DTOs
{
    public class StockResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
