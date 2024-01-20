namespace Crowmask.LowLevel

open System
open Crowmask.Interfaces
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open FSharp.Control

/// Provides an API key to access the Weasyl API and web interface.
type IWeasylApiKeyProvider =
    /// The Weasyl API key, created at https://www.weasyl.com/control/apikeys.
    abstract member ApiKey: string

type WeasylWhoami = {
    login: string
    userid: int
}

type WeasylMediaFile = {
    mediaid: Nullable<int>
    url: string
}

type WeasylUserMedia = {
    avatar: WeasylMediaFile list
}

type WeasylStatistics = {
    submissions: int
}

type WeasylUserInfo = {
    age: Nullable<int>
    gender: string
    location: string
    user_links: Map<string, string list>
}

type WeasylUserProfile = {
    username: string
    full_name: string
    profile_text: string
    media: WeasylUserMedia
    login_name: string
    statistics: WeasylStatistics
    user_info: WeasylUserInfo
    link: string
}

type WeasylSubmissionMedia = {
    submission: WeasylMediaFile list
    thumbnail: WeasylMediaFile list
}

type WeasylGallerySubmission = {
    posted_at: DateTimeOffset
    submitid: int
}

type WeasylSubmissionDetail = {
    link: string
    media: WeasylSubmissionMedia
    owner: string
    posted_at: DateTimeOffset
    rating: string
    title: string
    friends_only: bool
    tags: string list
    submitid: int
    subtype: string
    description: string
}

type WeasylGallery = {
    submissions: WeasylGallerySubmission list
    backid: int option
    nextid: int option
}

type WeasylGalleryPosition =
| Beginning
| Next of nextid: int

type WeasylGalleryCount =
| Count of int

type WeasylClient(appInfo: IApplicationInformation, httpClientFactory: IHttpClientFactory, apiKeyProvider: IWeasylApiKeyProvider) =
    let getAsync (uri: string) = task {
        use client = httpClientFactory.CreateClient()

        use req = new HttpRequestMessage(HttpMethod.Get, uri)
        req.Headers.UserAgent.ParseAdd(appInfo.UserAgent)
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"))
        req.Headers.Add("X-Weasyl-API-Key", apiKeyProvider.ApiKey)

        return! client.SendAsync(req)
    }

    let getUserGalleryAsync (username: string) (position: WeasylGalleryPosition) (count: WeasylGalleryCount) = task {
        let query = String.concat "&" [
            match position with
            | Beginning -> ()
            | Next x -> $"nextid={x}"

            match count with
            | Count x -> $"count={x}"
        ]
        use! resp = getAsync $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(username)}/gallery?{query}"
        ignore (resp.EnsureSuccessStatusCode())
        return! resp.Content.ReadFromJsonAsync<WeasylGallery>()
    }

    let getSubmissionAsync (submitid: int) = task {
        use! resp = getAsync $"https://www.weasyl.com/api/submissions/{submitid}/view"
        if resp.StatusCode = System.Net.HttpStatusCode.NotFound then
            return None
        else
            ignore (resp.EnsureSuccessStatusCode())
            let! object = resp.Content.ReadFromJsonAsync<WeasylSubmissionDetail>()
            return Some object
    }

    let getUserAsync (user: string) = task {
        use! resp = getAsync $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(user)}/view"
        ignore (resp.EnsureSuccessStatusCode())
        return! resp.Content.ReadFromJsonAsync<WeasylUserProfile>()
    }

    let whoamiAsync () = task {
        use! resp = getAsync $"https://www.weasyl.com/api/whoami"
        ignore (resp.EnsureSuccessStatusCode())
        return! resp.Content.ReadFromJsonAsync<WeasylWhoami>()
    }

    let whoamiLazy = lazy task {
        return! whoamiAsync ()
    }

    let userProfileLazy = lazy task {
        let! w = whoamiLazy.Value
        return! getUserAsync w.login
    }

    /// Returns user profile information from the Weasyl API for the logged-in user.
    member _.GetMyUserAsync() = task {
        return! userProfileLazy.Value
    }

    /// Returns submission information for an ID, unless it doesn't exist,
    /// wasn't posted by the logged-in user, or is set to friends only.
    member _.GetMyPublicSubmissionAsync(submitid) = task {
        let! whoami = whoamiLazy.Value
        match! getSubmissionAsync submitid with
        | None ->
            return None
        | Some s ->
            if s.owner = whoami.login && not s.friends_only
            then return Some s
            else return None
    }

    /// Returns all submissions, with limited information,
    /// for the logged-in user (newest to oldest).
    member _.GetMyGallerySubmissionsAsync() = taskSeq {
        let! whoami = whoamiLazy.Value
        let! firstPage = getUserGalleryAsync whoami.login Beginning (Count 5)

        let mutable page = firstPage
        let mutable finished = false
        while not finished do
            yield! page.submissions

            match page.nextid with
            | Some x ->
                let! nextPage = getUserGalleryAsync whoami.login (Next x) (Count 100)
                page <- nextPage
            | None ->
                finished <- true
    }
