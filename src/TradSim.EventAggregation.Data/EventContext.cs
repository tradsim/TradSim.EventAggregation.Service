using Microsoft.EntityFrameworkCore;

namespace TradSim.EventAggregation.Data
{
    public class EventContext : DbContext
    {
        public EventContext(DbContextOptions<EventContext> options)
            : base(options)
        {            
        }
        public DbSet<DbOrderEvent> Events { get; set; }         

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbOrderEvent>().ToTable("orderevents");
            modelBuilder.Entity<DbOrderEvent>().HasKey(p=>p.Id);
            modelBuilder.Entity<DbOrderEvent>().Property(b => b.Id).HasColumnName("id");
            modelBuilder.Entity<DbOrderEvent>().Property(b => b.SourceId).HasColumnName("source_id");
            modelBuilder.Entity<DbOrderEvent>().Property(b => b.Created).HasColumnName("created");
            modelBuilder.Entity<DbOrderEvent>().Property(b => b.EventType).HasColumnName("event_type");
            modelBuilder.Entity<DbOrderEvent>().Property(b => b.Version).HasColumnName("version");
            modelBuilder.Entity<DbOrderEvent>().Property(b => b.Payload).HasColumnName("payload");
        }
    }
}
