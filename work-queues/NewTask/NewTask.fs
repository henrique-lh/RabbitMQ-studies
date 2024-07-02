open System
open System.Text
open RabbitMQ.Client

let getMessage (args: string[]) =
    if args.Length > 0 then
        String.Join(" ", args)
    else
        "Hello World!"

[<EntryPoint>]
let main argv =
    let factory = ConnectionFactory(HostName = "localhost")

    let connection = factory.CreateConnection()
    let channel = connection.CreateModel()

    channel.QueueDeclare(queue = "task_queue",
                        durable = true,
                        exclusive = false,
                        autoDelete = false,
                        arguments = null) |> ignore

    let message: string = getMessage argv

    let body = Encoding.UTF8.GetBytes message
    let properties = channel.CreateBasicProperties()
    properties.Persistent <- true

    channel.BasicPublish(exchange = "",
                        routingKey = "task_queue",
                        basicProperties = null,
                        body = body)

    printfn "[x] Sent %s" message

    0
