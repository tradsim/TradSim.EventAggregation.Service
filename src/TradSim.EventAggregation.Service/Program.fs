// Learn more about F# at http://fsharp.org

open System
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Text
open System.IO
open Newtonsoft.Json
open TradSim.EventAggregation

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

let deserializeEnvelope (message:string) :EventEnvelope =
    JsonConvert.DeserializeObject<EventEnvelope>(message)

let deserializeEvent envelope : OrderEventStored =
    JsonConvert.DeserializeObject<OrderEventStored>(envelope.Payload)

let createConsumer channel =
    let consumer = new EventingBasicConsumer(channel);
    consumer.Received.Add((fun result ->
            let event = deserializeEnvelope (Encoding.UTF8.GetString(result.Body))
                        |> deserializeEvent
            channel.BasicAck(result.DeliveryTag,false)      
    ))
    consumer

let rec exitLoop i = 
    let char = Console.ReadKey()
    match char.Key with
    | ConsoleKey.Escape -> i
    | _ -> exitLoop i 

[<EntryPoint>]
let main argv = 

    let queue = "order_event_stored"
    let exchange = "order_event_stored"
    let config = { Host= "localhost"; Port= 5672; UserName= "guest"; Password= "guest"; VirtualHost= "tradsim" }

    use connection = createFactory config |> createConnection
    use channel = setupChannel exchange queue connection

    let consumer = createConsumer channel
    channel.BasicConsume(queue,false,consumer) |> ignore
    
    printf "Press [ESC] to exit"
    exitLoop 0
