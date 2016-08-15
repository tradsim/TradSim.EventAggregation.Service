using System;

namespace  TradSim.EventAggregation.Data 
{
    public class OrderEvent
    {
        public long Id {get;set;} // id
        public Guid SourceId { get; set; } //  source_id
        public DateTimeOffset Created {get;set;} //created
        public string EventType { get; set; } //event_type
        public int Version { get; set; } //version
        public string Payload { get; set; } //payload
    }
}