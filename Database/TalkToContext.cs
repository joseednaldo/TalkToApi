using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TalkToApi.V1.Models;

namespace TalkToApi.Database
{
    public class TalkToContext : IdentityDbContext<ApplicationUser> //DbContext
    {

        public TalkToContext(DbContextOptions<TalkToContext> optionsEdi) :base (optionsEdi)
        {

        }

        public DbSet<Mensagem> Mensagem { get; set; }
        public DbSet<Token> Tokens { get; set; }
    }
}
