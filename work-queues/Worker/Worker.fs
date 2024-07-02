open System
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Threading

let factory = ConnectionFactory(HostName = "localhost")

let connection = factory.CreateConnection()
let channel = connection.CreateModel()

channel.QueueDeclare(queue = "task_queue",
                    durable = true,
                    exclusive = false,
                    autoDelete = false,
                    arguments = null) |> ignore

channel.BasicQos(prefetchSize = uint32 0, prefetchCount = uint16 1, ``global`` = false)

printfn " [*] Waiting for messages."

let consumer = EventingBasicConsumer(channel)
consumer.Received.Add(fun ea ->
    let body = ea.Body.ToArray()
    let message = Encoding.UTF8.GetString body
    printfn " [x] Received %s" message

    let dots = message.Split('.').Length - 1
    Thread.Sleep(dots * 1000)
    printfn " [x] Done"

    channel.BasicAck(ea.DeliveryTag, false)
)

channel.BasicConsume(queue = "task_queue",
                    autoAck = false,
                    consumer = consumer) |> ignore

printfn " Press [enter] to exit."
Console.ReadLine() |> ignore