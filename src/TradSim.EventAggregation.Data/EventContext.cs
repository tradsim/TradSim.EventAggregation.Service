using Microsoft.EntityFrameworkCore;

namespace TradSim.EventAggregation.Data
{
    public class EventContext : DbContext
    {
        public EventContext(DbContextOptions<EventContext> options)
            : base(options)
        {            
        }
        public DbSet<OrderEvent> Events { get; set; }         
    }
}
