using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TalkToApi.V1.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string Slogan { get; set; }

        //[ForeignKey("De")]
        //public virtual ICollection<Mensagem> Mensagem  { get; set; }

    }
}
