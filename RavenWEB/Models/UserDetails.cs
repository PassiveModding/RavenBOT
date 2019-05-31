using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RavenWEB.Models
{
    public class UserDetails
    {
        [Required]
        [Display(Name = "Token")]
        [DataType(DataType.Password)]
        public string Token { get; set; }
    }
}
