module Service

open Suave.Types
open Suave.Web
open System.Net
open System.Threading
open System

let start port =
  let port = port |> uint16 
  let bindings = 
    Dns.GetHostName()
    |> Dns.GetHostEntry
    |> fun ipHostEntry -> 
      ipHostEntry.AddressList
      |> Seq.filter (fun ipAddress -> ipAddress.AddressFamily = Sockets.AddressFamily.InterNetwork)
      |> Seq.map (fun ipAddress -> HttpBinding.mk HTTP ipAddress port)
      |> Seq.toList
  
  let listening, server = startWebServerAsync { defaultConfig with bindings = bindings } TodoBackend.routes
  let cts = new CancellationTokenSource()
  Async.Start(server, cts.Token)
  listening
  |> Async.RunSynchronously
  |> ignore
  { new IDisposable with
      member __.Dispose() = cts.Cancel() }