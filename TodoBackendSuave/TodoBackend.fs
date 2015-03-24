module TodoBackend

#nowarn "25"

open Microsoft.FSharp.Quotations.Patterns
open Newtonsoft.Json
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.RequestErrors
open Suave.Http.Successful
open Suave.Http.Writers
open Suave.Types
open Suave.Utils
open Suave.Web
open System
open System.Collections.Generic

[<CLIMutable>]
type Todo = 
  { title : string
    order : int
    completed : bool
    url : string }

let store = ResizeArray<Todo>()
let find id = store |> Seq.tryFind (fun t -> t.url.EndsWith id)
let serialize = JsonConvert.SerializeObject
let deserialize<'a> s = JsonConvert.DeserializeObject<'a> s
let CORS = 
  setHeader "Access-Control-Allow-Origin" "*" >>= setHeader "Access-Control-Allow-Headers" "content-type" 
  >>= setHeader "Access-Control-Allow-Methods" "POST, GET, OPTIONS, DELETE, PATCH"

let get _ = 
  store
  |> serialize
  |> OK

let urlWithHost (request : HttpRequest) = 
  let host = 
    request.headers
    |> List.find (fst >> (=) "host")
    |> snd
  sprintf "%s://%s%s" request.url.Scheme host request.url.PathAndQuery

let getTodo id = 
  match find id with
  | Some todo -> serialize todo |> OK
  | None -> NOT_FOUND id

let patchTodo request = 
  let json = request.rawForm |> UTF8.toString
  let patchedPropertiesContains property = 
    JsonConvert.DeserializeObject<Dictionary<string, string>> json |> Seq.exists (fun x -> x.Key = property)
  let patchedTodo = json |> deserialize<Todo>
  
  let getPatchedProperty (Lambda(_, PropertyGet(_, propertyInfo, _))) patched original = 
    match patchedPropertiesContains propertyInfo.Name with
    | true -> patched
    | false -> original
    |> propertyInfo.GetValue
    |> unbox
  match request
        |> urlWithHost
        |> find with
  | Some todo -> 
    store.Remove todo |> ignore
    let getPatchedProperty expr = getPatchedProperty expr patchedTodo todo
    
    let patched = 
      { todo with title = getPatchedProperty <@ fun t -> t.title @>
                  completed = getPatchedProperty <@ fun t -> t.completed @>
                  order = getPatchedProperty <@ fun t -> t.order @> }
    store.Add patched
    patched
    |> serialize
    |> OK
  | None -> 
    request
    |> urlWithHost
    |> NOT_FOUND

let post request = 
  let todo = 
    request.rawForm
    |> UTF8.toString
    |> deserialize<Todo>
  
  let todo = { todo with url = (request |> urlWithHost, Guid.NewGuid()) ||> sprintf "%s%O" }
  store.Add todo
  todo
  |> serialize
  |> OK

let deleteAll _ = 
  store.Clear()
  store
  |> serialize
  |> OK

let deleteTodo id = 
  match find id with
  | Some todo -> 
    store.Remove todo |> ignore
    OK id
  | None -> NOT_FOUND id

let routes = 
  choose [ OPTIONS >>= CORS >>= NO_CONTENT
           GET >>= CORS >>= path "/" >>= request get
           GET >>= CORS >>= pathScan "/%s" getTodo
           PATCH >>= CORS >>= request patchTodo
           POST >>= CORS >>= request post
           DELETE >>= CORS >>= path "/" >>= request deleteAll
           DELETE >>= CORS >>= pathScan "/%s" deleteTodo ]

let start _ = startWebServer defaultConfig routes
