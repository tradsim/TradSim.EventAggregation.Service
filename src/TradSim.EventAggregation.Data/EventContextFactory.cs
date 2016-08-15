using Microsoft.EntityFrameworkCore;

namespace TradSim.EventAggregation.Data
{
    public class EventContextFactory
    {        
        private readonly DbContextOptions<EventContext> _options;
        public EventContextFactory (string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EventContext>();
            optionsBuilder.UseNpgsql(connectionString);
            _options = optionsBuilder.Options;
        }

        public EventContext Create() => new EventContext(_options);
    }
}