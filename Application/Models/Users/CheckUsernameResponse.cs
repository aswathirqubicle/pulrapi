using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Models.Users
{
    public class CheckUsernameResponse
    {
        public bool Exists { get; set; }
        public string Message { get; set; }
    }
}
