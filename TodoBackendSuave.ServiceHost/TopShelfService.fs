module ServiceHost

open System
open System.Configuration
open Topshelf

type TopShelfService() = 
  let mutable disposable = Unchecked.defaultof<IDisposable>
  
  interface ServiceControl with
    
    member __.Start _ = 
      let port = ConfigurationManager.AppSettings.["port"] |> int
      disposable <- Service.start port
      true
    
    member __.Stop _ = true
  
  interface IDisposable with
    member __.Dispose() = disposable.Dispose()

[<EntryPoint>]
let main _ = 
  HostFactory.New(fun config -> 
    config.Service<TopShelfService>() |> ignore)
  |> fun x -> x.Run() |> ignore
  0
