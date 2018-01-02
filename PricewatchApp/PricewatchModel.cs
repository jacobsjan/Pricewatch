namespace PricewatchApp
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class PricewatchModel : DbContext
    {
        public PricewatchModel(string connectionString): base(connectionString)
        {
        }

        public virtual DbSet<App> Apps { get; set; }
        public virtual DbSet<Price> Prices { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<App>()
                .HasMany(e => e.Prices)
                .WithRequired(e => e.App1)
                .HasForeignKey(e => e.App)
                .WillCascadeOnDelete(false);
        }
    }
}
