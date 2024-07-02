open System
open System.Text
open RabbitMQ.Client

let factory = ConnectionFactory(HostName = "localhost")

let connection = factory.CreateConnection()
let channel = connection.CreateModel()

channel.QueueDeclare(queue = "hello",
                    durable = false,
                    exclusive = false,
                    autoDelete = false,
                    arguments = null) |> ignore

let message: string = "Hello World!"
let body = Encoding.UTF8.GetBytes message

channel.BasicPublish(exchange = "",
                    routingKey = "hello",
                    basicProperties = null,
                    body = body)

printfn "[x] Sent %s" message

printfn " Press [enter] to exit."
Console.ReadLine() |> ignore