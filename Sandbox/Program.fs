open Crowmask

Requests.FetchActorAsync("https://microblog.lakora.us/")
|> Async.AwaitTask
|> Async.RunSynchronously
|> printfn "%A"