open Servya
open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

module Async =
    let AwaitTaskVoid : (Task -> Async<unit>) =
        Async.AwaitIAsyncResult >> Async.Ignore

    let StartAsTask (work : Async<unit>) =
        Task.Factory.StartNew(fun () -> work |> Async.RunSynchronously)

let router = new Router()
let processor = AsyncHttpListener(router, 1337, 0)

let addRoute (verb : HttpVerb) path (action : IHttpContext -> IDictionary<string, string> -> Async<unit>) =
    router.AddRoute(verb, path, fun context args ->
        Async.StartAsTask(action context args))

// Raw HTTP routes
// http://host/test
let testRoute (context : IHttpContext) (args : IDictionary<string, string>) = async {
    let response = context.Response
    use writer = context.GetWriter()
    do! Async.Sleep(1000);
    do! writer.WriteLineAsync("F# success!") |> Async.AwaitTaskVoid
}

addRoute HttpVerb.Get "/test" testRoute |> ignore

// Create a strongly typed service
// http://host/functional/add?a=1&b=2
[<Service>]
type FunctionalService() =
    [<Route>]
    member x.Add a b =
        a + b

let autoRouter = AutoRouter(router, Parser(), DependencyResolver())
autoRouter.Discover()
autoRouter.CreateWebInterface()

let newContext = Func<_>(fun () -> EventLoopContext() :> SynchronizationContext)
processor.Start(newContext, Environment.ProcessorCount)

while true do
    Console.ReadLine() |> ignore
