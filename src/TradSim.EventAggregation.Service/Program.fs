// Learn more about F# at http://fsharp.org

open System
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Text
open System.IO
open System.Linq
open System.Globalization
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open TradSim.EventAggregation
open Microsoft.EntityFrameworkCore;
open TradSim.EventAggregation.Data;

type Config = {
    Host: string
    Port: int
    UserName: string
    Password: string
    VirtualHost: string
}

type QueueMessage = {
    Message: string
    DeliveryTag: uint64
}

type EventEnvelope = {
    [<JsonProperty("event_type")>]
    EventType: string
    Payload:string
}

type OrderEventStored = {
    [<JsonProperty("event_type")>]
    EventType: string
    Id:    Guid
    Occured:   DateTimeOffset
    Version:   uint32
}

let rec exitLoop i = 
    let char = Console.ReadKey()
    match char.Key with
    | ConsoleKey.Escape -> i
    | _ -> exitLoop i

let createFactory config =
    let factory = ConnectionFactory(HostName = config.Host, Port = config.Port, UserName = config.UserName, Password = config.Password)
    factory.AutomaticRecoveryEnabled <- true
    factory.RequestedConnectionTimeout <- 2000
    factory.NetworkRecoveryInterval <- TimeSpan.FromSeconds(20.0) 
    factory.VirtualHost <- config.VirtualHost
    factory

let createConnection (factory:ConnectionFactory)=
    factory.CreateConnection()

let setupChannel exchange queue (connection: IConnection) =
    let channel = connection.CreateModel()
    channel.ExchangeDeclare(exchange, "fanout", true)
    channel.QueueDeclare(queue,true,false,false) |> ignore    
    channel.QueueBind(queue, exchange, "")
    channel

let createDbContextFactory connectionString=
    new EventContextFactory(connectionString)    

let createDbContext (factory:EventContextFactory)=
    factory.Create()

let getEvents sourceId (dbContext:EventContext)=
    let repo =new EventRepository(dbContext)
    repo.Get(sourceId)

let deserializeEnvelope (message:string) :EventEnvelope =
    JsonConvert.DeserializeObject<EventEnvelope>(message)

let deserializeStoredEvent envelope : OrderEventStored =
    JsonConvert.DeserializeObject<OrderEventStored>(envelope.Payload)

let toDirection value=
    match value with
    | "Buy" -> TradeDirection.Buy
    | "Sell" -> TradeDirection.Sell
    |  _ -> raise <| new ArgumentOutOfRangeException("value", value ,"Direction is invalid!")

let sanitizeJSONString (value:string)=
     value.Trim('"').Replace("\\\"","\"")

let toGuid value=
    Guid.Parse(value)

let toDateTimeOffset (value:DateTime) =
    DateTimeOffset(value,TimeSpan.Zero)

let getJSONObject payload=
    payload |> sanitizeJSONString |> JsonConvert.DeserializeObject :?> JObject

let deserializeOrderAccepted (dbOrderEvent:DbOrderEvent) =
    let event = getJSONObject dbOrderEvent.Payload
    let id = toGuid (event.Value<string> "id")
    let symbol = event.Value<string> "symbol"
    let price = event.Value<decimal> "price"
    let quantity = event.Value<uint32> "quantity"
    let direction = toDirection (event.Value<string> "direction")
    let occured = toDateTimeOffset (event.Value<DateTime> "occured")
    let version = event.Value<uint32> "version"
    OrderAccepted(id,symbol,price,quantity,direction,occured,version)
    

let deserializeOrderCancelled (dbOrderEvent:DbOrderEvent) =
    let event = getJSONObject dbOrderEvent.Payload
    let id = toGuid (event.Value<string> "id")
    let occured = toDateTimeOffset (event.Value<DateTime> "occured")
    let version = event.Value<uint32> "version"
    OrderCancelled(id,occured,version)

let deserializeOrderAmended (dbOrderEvent:DbOrderEvent) =
    let event = getJSONObject dbOrderEvent.Payload
    let id = toGuid (event.Value<string> "id")
    let quantity = event.Value<uint32> "quantity"
    let occured = toDateTimeOffset (event.Value<DateTime> "occured")
    let version = event.Value<uint32> "version"
    OrderAmended(id,quantity,occured,version)

let deserializeOrderTraded (dbOrderEvent:DbOrderEvent) =
    let event = getJSONObject dbOrderEvent.Payload
    let id = toGuid (event.Value<string> "id")
    let price = event.Value<decimal> "price"
    let quantity = event.Value<uint32> "quantity"
    let occured = toDateTimeOffset (event.Value<DateTime> "occured")
    let version = event.Value<uint32> "version"
    OrderTraded(id,price,quantity,occured,version)

let deserializeOrderEvent (dbOrderEvent:DbOrderEvent) =
    match dbOrderEvent.EventType with
    | "OrderAccepted"   -> deserializeOrderAccepted dbOrderEvent
    | "OrderAmended"    -> deserializeOrderAmended dbOrderEvent
    | "OrderCancelled"  -> deserializeOrderCancelled dbOrderEvent
    | "OrderTraded"     -> deserializeOrderTraded dbOrderEvent
    | _                 -> raise <| new ArgumentOutOfRangeException("dbOrderEvent.EventType", dbOrderEvent.EventType ,"EventType is invalid!")

let createConsumer channel dbFactory =
    let consumer = new EventingBasicConsumer(channel);
    consumer.Received.Add((fun result ->
            let event = deserializeEnvelope (Encoding.UTF8.GetString(result.Body))
                        |> deserializeStoredEvent
            use ctx = createDbContext dbFactory
            let order = getEvents event.Id ctx 
                         |> Seq.map deserializeOrderEvent
                         |> aggregate                        
            printfn "%A" order
            channel.BasicAck(result.DeliveryTag,false)      
    ))
    consumer    

[<EntryPoint>]
let main argv = 

    let queue = "order_event_stored"
    let exchange = "order_event_stored"
    let config = { Host= "localhost"; Port= 5672; UserName= "guest"; Password= "guest"; VirtualHost= "tradsim" }
    let connectionString = "User ID=postgres;Password=1234;Host=localhost;Port=5432;Database=orderevents;Pooling=true;"

    use connection = createFactory config |> createConnection
    use channel = setupChannel exchange queue connection

    let consumer = createConsumer channel (createDbContextFactory connectionString)
    channel.BasicConsume(queue,false,consumer) |> ignore

    printfn  "Press [ESC] to exit"
    exitLoop 0
