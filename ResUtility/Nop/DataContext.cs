using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResUtility.Nop
{
    public class NopDataContext : DbContext
    {
        public NopDataContext(string connectionString) : base(connectionString)
        {
            Configuration.AutoDetectChangesEnabled = false;
        }
        public DbSet<Language> Languages { get; set; }
        public DbSet<LocaleStringResource> LocaleStringResources { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)

        {
            Database.SetInitializer<NopDataContext>(null);
            base.OnModelCreating(modelBuilder);
            modelBuilder.Configurations.Add(new Map.LanguageMap());
            modelBuilder.Configurations.Add(new Map.LocaleStringResourceMap());
        }
    }
}
