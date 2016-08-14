// Learn more about F# at http://fsharp.org

open System
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Text
open System.IO

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

let readFromQueue queue (channel:IModel) =
    let result = channel.BasicGet(queue, false)
    if result <> null then
            let message = Encoding.UTF8.GetString(result.Body)
            Some {Message=message; DeliveryTag=result.DeliveryTag}
        else
            None

[<EntryPoint>]
let main argv = 

    let queue = "order_event_stored"
    let exchange = "order_event_stored"
    let config = { Host= "localhost"; Port= 5672; UserName= "guest"; Password= "guest"; VirtualHost= "tradsim" }

    use connection = createFactory config |> createConnection
    use channel = setupChannel exchange queue connection

    let readFromEventStoredQueue = readFromQueue queue channel

    while true do
        let message = readFromEventStoredQueue
        match message with
        | Some(s) -> 
            printfn "%s" s.Message
            channel.BasicAck(s.DeliveryTag,true)
        | _ -> ()

    0 // return an integer exit code
