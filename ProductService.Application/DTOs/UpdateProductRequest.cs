using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Application.DTOs
{
    namespace ProductService.Application.DTOs
    {
        public class UpdateProductRequest
        {
            public string Name { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public decimal Price { get; set; }

            public int StockQuantity { get; set; }
        }
    }
}
