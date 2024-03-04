namespace Crowmask.ATProto

open System
open System.Collections.Generic
open System.Net.Http
open System.Threading.Tasks
open System.Net.Http.Json
open System.Net.Http.Headers
open System.Net

type IAccessTokenCredentials =
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
        member this.AccessToken = this.accessJwt
        member this.RefreshToken = this.refreshJwt

type IAutomaticRefreshCredentials =
    inherit IRefreshTokenCredentials
    abstract member UpdateTokensAsync: newCredentials: IRefreshTokenCredentials -> Task

module Requester =
    type Request = {
        method: HttpMethod
        uri: Uri
        bearerToken: string option
        body: (string * obj) list option
    }

    let build (hostname: string) (method: HttpMethod) (procedureName: string) = {
        method = method
        uri = new Uri($"https://{Uri.EscapeDataString(hostname)}/xrpc/{Uri.EscapeDataString(procedureName)}")
        bearerToken = None
        body = None
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
        req with body = Some body
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
        | Some t -> req.Headers.Authorization <- new AuthenticationHeaderValue("Bearer", t)
        | None -> ()

        match request.body with
        | Some b -> req.Content <- JsonContent.Create(dict b)
        | None -> ()

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

module AutoRefresh =
    type Error = {
        error: string
        message: string
    }

    let sendAsync<'T> httpClient (credentials: IAccessTokenCredentials) req = task {
        use! initialResp =
            req
            |> Requester.addAccessToken credentials
            |> Requester.sendAsync httpClient

        use! finalResp = task {
            match credentials, initialResp.StatusCode with
            | :? IAutomaticRefreshCredentials as auto, HttpStatusCode.BadRequest ->
                let! newCredentials = Auth.refreshSessionAsync httpClient req.uri.Host auto
                do! auto.UpdateTokensAsync(newCredentials)
                return!
                    req
                    |> Requester.addAccessToken newCredentials
                    |> Requester.sendAsync httpClient
            | _ ->
                return initialResp
        }

        finalResp.EnsureSuccessStatusCode() |> ignore
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

    type NotificationParameters = {
        limit: Nullable<int>
        cursor: string
    }

    let listNotificationsAsync httpClient hostname credentials (ps: NotificationParameters) = task {
        return!
            Requester.build hostname HttpMethod.Get "app.bsky.notification.listNotifications"
            |> Requester.addQueryParameters [
                if ps.limit.HasValue then
                    "limit", $"{ps.limit.Value}"

                if not (isNull ps.cursor) then
                    "cursor", ps.cursor
            ]
            |> AutoRefresh.sendAsync<NotificationList> httpClient credentials
    }
