using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Application.DTOs;

public class ProductQueryParameters
{
    public string? Search { get; set; }

    public string? SortBy { get; set; }

    public bool Desc { get; set; } = false;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 5;
}
