open System
open System.Text
open RabbitMQ.Client
open RabbitMQ.Client.Events

let factory = ConnectionFactory(HostName = "localhost")

let connection = factory.CreateConnection()
let channel = connection.CreateModel()

channel.QueueDeclare(queue = "hello",
                    durable = false,
                    exclusive = false,
                    autoDelete = false,
                    arguments = null) |> ignore

printfn " [*] Waiting for messages."

let consumer = EventingBasicConsumer(channel)
consumer.Received.Add(fun ea ->
    let body = ea.Body.ToArray()
    let message = Encoding.UTF8.GetString body
    printfn " [x] Received %s" message
)

channel.BasicConsume(queue = "hello",
                    autoAck = true,
                    consumer = consumer) |> ignore

printfn " Press [enter] to exit."
Console.ReadLine() |> ignore