using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.History;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Core.DomainModel;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Infrastructure.Data
{
    public class SampleContext : DbContext //IdentityDbContext<ApplicationUser>
    {
        public SampleContext()
            : base("DefaultConnection")
        {
            //Database.SetInitializer<SampleContext>(new SampleSeedInitializer());
        }

       public IDbSet<Serie> Series { get; set; }
       public IDbSet<Test> Tests { get; set; }
       public IDbSet<Manga> Mangas { get; set; }
       public IDbSet<Lists> Listses { get; set; } 
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
          
        }
    }
}
