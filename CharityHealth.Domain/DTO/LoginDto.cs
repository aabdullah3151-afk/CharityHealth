using System;
using System.Collections.Generic;
using System.Text;

namespace CharityHealth.Domain.DTO
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; }= string.Empty;
        public bool RememberMe { get; set; }
    }
}
