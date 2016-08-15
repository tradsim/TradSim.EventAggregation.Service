// Learn more about F# at http://fsharp.org

open System
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Text
open System.IO
open Newtonsoft.Json
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

let createDbContext (factory:EventContextFactory)=
    factory.Create()

let deserializeEnvelope (message:string) :EventEnvelope =
    JsonConvert.DeserializeObject<EventEnvelope>(message)

let deserializeEvent envelope : OrderEventStored =
    JsonConvert.DeserializeObject<OrderEventStored>(envelope.Payload)

let createConsumer channel dbFactory =
    let consumer = new EventingBasicConsumer(channel);
    consumer.Received.Add((fun result ->
            let event = deserializeEnvelope (Encoding.UTF8.GetString(result.Body))
                        |> deserializeEvent
            use ctx = createDbContext dbFactory

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

    let dbContextFactory = new EventContextFactory(connectionString)
    let consumer = createConsumer channel dbContextFactory
    channel.BasicConsume(queue,false,consumer) |> ignore

    printf "Press [ESC] to exit"
    exitLoop 0
