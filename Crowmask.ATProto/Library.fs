namespace Crowmask.ATProto

open System
open System.Net.Http
open System.Threading.Tasks
open System.Net.Http.Json
open System.Net.Http.Headers
open System.Net
open Crowmask.Interfaces

type IAccessTokenCredentials =
    abstract member DID: string
    abstract member AccessToken: string

type IRefreshTokenCredentials =
    inherit IAccessTokenCredentials
    abstract member RefreshToken: string

type Tokens = {
    accessJwt: string
    refreshJwt: string
    handle: string
    did: string
} with
    interface IRefreshTokenCredentials with
        member this.DID = this.did
        member this.AccessToken = this.accessJwt
        member this.RefreshToken = this.refreshJwt

type IAutomaticRefreshCredentials =
    inherit IRefreshTokenCredentials
    abstract member UpdateTokensAsync: newCredentials: IRefreshTokenCredentials -> Task

type Limit =
| DefaultLimit
| Limit of int

type Cursor =
| FromStart
| FromCursor of string

module Modules =
    module Requester =
        type Body =
        | NoBody
        | JsonBody of (string * obj) list
        | RawBody of data: byte[] * contentType: string

        type Request = {
            method: HttpMethod
            uri: Uri
            bearerToken: string option
            body: Body
        }

        let build (hostname: string) (method: HttpMethod) (procedureName: string) = {
            method = method
            uri = new Uri($"https://{Uri.EscapeDataString(hostname)}/xrpc/{Uri.EscapeDataString(procedureName)}")
            bearerToken = None
            body = NoBody
        }

        let addQueryParameters (parameters: (string * string) list) (req: Request) =
            let qs = String.concat "&" [
                for key, value in parameters do
                    $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}"
            ]

            {
                req with uri = new Uri($"{req.uri.GetLeftPart(UriPartial.Path)}?{qs}")
            }

        let addJsonBody (body: (string * obj) list) (req: Request) = {
            req with body = JsonBody body
        }

        let addBody (body: byte[]) (contentType: string) (req: Request) = {
            req with body = RawBody (body, contentType)
        }

        let addAccessToken (credentials: IAccessTokenCredentials) (req: Request) = {
            req with bearerToken = Some credentials.AccessToken
        }

        let addRefreshToken (credentials: IRefreshTokenCredentials) (req: Request) = {
            req with bearerToken = Some credentials.RefreshToken
        }

        let sendAsync (httpClient: HttpClient) (request: Request) = task {
            use req = new HttpRequestMessage(request.method, request.uri)

            match request.bearerToken with
            | Some t ->
                req.Headers.Authorization <- new AuthenticationHeaderValue("Bearer", t)
            | None -> ()

            match request.body with
            | RawBody (data, contentType) ->
                let c = new ByteArrayContent(data)
                c.Headers.ContentType <- new MediaTypeHeaderValue(contentType)
                req.Content <- c
            | JsonBody b ->
                req.Content <- JsonContent.Create(dict b)
            | NoBody -> ()

            return! httpClient.SendAsync(req)
        }

    module Auth =
        let createSessionAsync httpClient hostname identifier password = task {
            use! resp =
                Requester.build hostname HttpMethod.Post "com.atproto.server.createSession"
                |> Requester.addJsonBody [
                    "identifier", identifier
                    "password", password
                ]
                |> Requester.sendAsync httpClient

            resp.EnsureSuccessStatusCode() |> ignore

            return! resp.Content.ReadFromJsonAsync<Tokens>()
        }

        let refreshSessionAsync httpClient hostname credentials = task {
            use! resp =
                Requester.build hostname HttpMethod.Post "com.atproto.server.refreshSession"
                |> Requester.addRefreshToken credentials
                |> Requester.sendAsync httpClient
            resp.EnsureSuccessStatusCode() |> ignore

            return! resp.Content.ReadFromJsonAsync<Tokens>()
        }

    module Reader =
        type Error = {
            error: string
            message: string
        }

        let readAsync<'T> httpClient (credentials: IAccessTokenCredentials) req = task {
            use! initialResp =
                req
                |> Requester.addAccessToken credentials
                |> Requester.sendAsync httpClient

            use! finalResp = task {
                match credentials, initialResp.StatusCode with
                | :? IAutomaticRefreshCredentials as auto, HttpStatusCode.BadRequest ->
                    let! err = initialResp.Content.ReadFromJsonAsync<Error>()
                    if err.error = "ExpiredToken" then
                        let! newCredentials = Auth.refreshSessionAsync httpClient req.uri.Host auto
                        do! auto.UpdateTokensAsync(newCredentials)
                        return!
                            req
                            |> Requester.addAccessToken newCredentials
                            |> Requester.sendAsync httpClient
                    else
                        return failwith err.message
                | _ ->
                    return initialResp
            }

            finalResp.EnsureSuccessStatusCode() |> ignore

            if typedefof<'T> = typedefof<unit> then
                return () :> obj :?> 'T
            else
                return! finalResp.Content.ReadFromJsonAsync<'T>()
        }

    module Notifications =
        type Author = {
            did: string
            handle: string
            displayName: string option
        }

        type Notification = {
            uri: string
            cid: string
            author: Author
            reason: string
            isRead: bool
            indexedAt: DateTimeOffset
        }

        type NotificationList = {
            cursor: string
            notifications: Notification list
        }

        let listNotificationsAsync httpClient hostname credentials limit cursor = task {
            return!
                Requester.build hostname HttpMethod.Get "app.bsky.notification.listNotifications"
                |> Requester.addQueryParameters [
                    match limit with
                    | Limit x -> "limit", $"{x}"
                    | DefaultLimit -> ()

                    match cursor with
                    | FromCursor c -> "cursor", c
                    | FromStart -> ()
                ]
                |> Reader.readAsync<NotificationList> httpClient credentials
        }

    module Repo =
        type BlobResponse = {
            blob: obj
        }

        let uploadBlobAsync httpClient hostname (credentials: IAccessTokenCredentials) (data: byte[]) (contentType: string) = task {
            return!
                Requester.build hostname HttpMethod.Post "com.atproto.repo.uploadBlob"
                |> Requester.addBody data contentType
                |> Reader.readAsync<BlobResponse> httpClient credentials
        }

        type Post = {
            text: string
            createdAt: DateTimeOffset
            images: BlobResponse seq
        }

        type NewRecord = {
            uri: string
            cid: string
        } with
            member this.RecordKey =
                this.uri.Split('/')
                |> Seq.last

        let createRecordAsync httpClient hostname (credentials: IAccessTokenCredentials) (post: Post) = task {
            return!
                Requester.build hostname HttpMethod.Post "com.atproto.repo.createRecord"
                |> Requester.addJsonBody [
                    "repo", "lizard-socks.lakora.us"
                    "collection", "app.bsky.feed.post"
                    "record", dict [
                        "$type", "app.bsky.feed.post" :> obj
                        "text", post.text
                        "createdAt", post.createdAt.ToString("o")

                        if not (Seq.isEmpty post.images) then
                            "embed", dict [
                                "$type", "app.bsky.embed.images" :> obj
                                "images", [
                                    for i in post.images do dict [
                                        "image", i.blob
                                        "alt", ""
                                    ]
                                ]
                            ]
                    ]
                ]
                |> Reader.readAsync<NewRecord> httpClient credentials
        }

        let deleteRecordAsync httpClient hostname (credentials: IAccessTokenCredentials) (rkey: string) = task {
            do!
                Requester.build hostname HttpMethod.Post "com.atproto.repo.deleteRecord"
                |> Requester.addJsonBody [
                    "repo", "lizard-socks.lakora.us"
                    "collection", "app.bsky.feed.post"
                    "rkey", rkey
                ]
                |> Reader.readAsync<unit> httpClient credentials
        }

type BlueskyClient(appInfo: IApplicationInformation, httpClientFactory: IHttpClientFactory) =
    let client = 
        let client = httpClientFactory.CreateClient()
        client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent)
        client

    let hostFor (credentials: IAccessTokenCredentials) =
        appInfo.BlueskyBotAccounts
        |> Seq.where (fun a -> a.DID = credentials.DID)
        |> Seq.map (fun a -> a.PDS)
        |> Seq.head

    member _.CreateSessionAsync (account: IBlueskyAccount) =
        Modules.Auth.createSessionAsync client account.PDS account.Identifier account.Password

    member _.ListNotificationsAsync credentials limit cursor =
        Modules.Notifications.listNotificationsAsync client (hostFor credentials) credentials limit cursor

    member _.UploadBlobAsync credentials data contentType =
        Modules.Repo.uploadBlobAsync client (hostFor credentials) credentials data contentType

    member _.CreateRecordAsync credentials post =
        Modules.Repo.createRecordAsync client (hostFor credentials) credentials post

    member _.DeleteRecordAsync credentials rkey =
        Modules.Repo.deleteRecordAsync client (hostFor credentials) credentials rkey
