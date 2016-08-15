using System;
using System.Collections.Generic;
using System.Linq;

namespace TradSim.EventAggregation.Data
{
    public class EventRepository    
    {
        private readonly EventContext _context;
        public EventRepository (EventContext context)
        {
            _context = context;
        }

        public List<DbOrderEvent> Get(Guid sourceId){
            return _context.Events.Where(p=>p.SourceId == sourceId).OrderBy(p=>p.Created).ToList();
        } 
    }
}
