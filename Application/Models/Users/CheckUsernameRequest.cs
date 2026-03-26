using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Models.Users
{
    public class CheckUsernameRequest
    {
        [Required]
        [PulrUsernameValidation]
        public string Username { get; set; }
    }
}
