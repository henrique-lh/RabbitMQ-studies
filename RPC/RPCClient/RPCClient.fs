open System
open System.Collections.Concurrent
open System.Text
open System.Threading
open System.Threading.Tasks
open RabbitMQ.Client
open RabbitMQ.Client.Events

type RpcClient() =
    let QUEUE_NAME = "rpc_queue"

    let factory = ConnectionFactory(HostName = "localhost")
    let connection = factory.CreateConnection()
    let channel = connection.CreateModel()
    let replyQueueName = channel.QueueDeclare().QueueName
    let callbackMapper = ConcurrentDictionary<string, TaskCompletionSource<string>>()
    let consumer = EventingBasicConsumer(channel)

    do
        consumer.Received.Add(fun ea ->
            let correlationId = ea.BasicProperties.CorrelationId
            match callbackMapper.TryRemove(correlationId) with
            | true, tcs ->
                let body = ea.Body.ToArray()
                let response = Encoding.UTF8.GetString body
                tcs.TrySetResult(response) |> ignore
            | false, _ -> ()
        )
        channel.BasicConsume(consumer = consumer, queue = replyQueueName, autoAck = true) |> ignore

    member this.CallAsync(message: string, cancellationToken: CancellationToken) : Task<string> =
        let props = channel.CreateBasicProperties()
        let correlationId = Guid.NewGuid().ToString()
        props.CorrelationId <- correlationId
        props.ReplyTo <- replyQueueName

        let messageBytes = Encoding.UTF8.GetBytes message
        let tcs = TaskCompletionSource<string>()
        callbackMapper.TryAdd(correlationId, tcs) |> ignore

        channel.BasicPublish(exchange = "", routingKey = QUEUE_NAME, basicProperties = props, body = messageBytes)

        cancellationToken.Register(fun () -> callbackMapper.TryRemove(correlationId) |> ignore) |> ignore
        tcs.Task

    interface IDisposable with
        member this.Dispose() =
            connection.Close()

module Rpc =
    [<EntryPoint>]
    let main args =
        let invokeAsync (n: string) = async {
            use rpcClient = new RpcClient()
            printfn " [x] Requesting fib(%s)" n
            let! response = rpcClient.CallAsync(n, CancellationToken.None) |> Async.AwaitTask
            printfn " [.] Got '%s'" response
        }

        printfn "RPC Client"
        let n = if args.Length > 0 then args.[0] else "30"
        invokeAsync n |> Async.RunSynchronously

        printfn " Press [enter] to exit."
        Console.ReadLine() |> ignore
        0
