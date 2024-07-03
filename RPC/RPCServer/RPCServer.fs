open System
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events

let rec Fib n =
    match n with
    | 0 | 1 -> n
    | _ -> Fib(n - 1) + Fib(n - 2)

let factory = ConnectionFactory(HostName = "localhost")

let connection = factory.CreateConnection()
let channel = connection.CreateModel()

channel.QueueDeclare(queue = "rpc_queue",
                    durable = false,
                    exclusive = false,
                    autoDelete = false,
                    arguments = null) |> ignore

channel.BasicQos(prefetchSize = uint32 0, prefetchCount = uint16 1, ``global`` = false)
let consumer = EventingBasicConsumer(channel)
channel.BasicConsume(queue = "rpc_queue",
                    autoAck = false,
                    consumer = consumer) |> ignore

printfn " [*] Awaiting for RPC requests."

consumer.Received.Add(fun ea ->
    let body = ea.Body.ToArray()
    let props = ea.BasicProperties
    let replyProps = channel.CreateBasicProperties()
    replyProps.CorrelationId <- props.CorrelationId

    let response =
        try
            let message = Encoding.UTF8.GetString body
            let n = message  |> int32
            printfn $" [.] Fib({n})"
            (Fib n).ToString()
        with
        | exn ->
            printfn " [.] %s" exn.Message
            ""

    let responseBytes = Encoding.UTF8.GetBytes response
    channel.BasicPublish(exchange = "",
                        routingKey = props.ReplyTo,
                        basicProperties = replyProps,
                        body = responseBytes)
    channel.BasicAck(ea.DeliveryTag, false)

)

printfn " Press [enter] to exit."
Console.ReadLine() |> ignore