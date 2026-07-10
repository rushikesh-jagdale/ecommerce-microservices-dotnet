using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Application.DTOs
{
    public class AuthResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;

        public string? Token { get; set; }
    }
}
