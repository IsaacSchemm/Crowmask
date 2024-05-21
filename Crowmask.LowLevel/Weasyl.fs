namespace Crowmask.LowLevel

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open FSharp.Control

/// Provides an API key to access the Weasyl API and web interface.
type IWeasylApiKeyProvider =
    /// The Weasyl API key, created at https://www.weasyl.com/control/apikeys.
    abstract member ApiKey: string

/// .NET types that map to response types from the Weasyl API.
module Weasyl =
    type Whoami = {
        login: string
        userid: int
    }

    type MediaFile = {
        mediaid: Nullable<int>
        url: string
    }

    type UserMedia = {
        avatar: MediaFile list
    }

    type Statistics = {
        submissions: int
    }

    type UserInfo = {
        age: Nullable<int>
        gender: string
        location: string
        user_links: Map<string, string list>
    }

    type UserProfile = {
        username: string
        full_name: string
        profile_text: string
        media: UserMedia
        login_name: string
        statistics: Statistics
        user_info: UserInfo
        link: string
    }

    type SubmissionMedia = {
        submission: MediaFile list
        thumbnail: MediaFile list
    }

    type GallerySubmission = {
        posted_at: DateTimeOffset
        submitid: int
    }

    type SubmissionDetail = {
        link: string
        media: SubmissionMedia
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

    type JournalDetail = {
        journalid: int
        title: string
        owner: string
        content: string
        tags: string list
        link: string
        rating: string
        friends_only: bool
        posted_at: DateTimeOffset
    }

    type CharacterDetail = {
        charid: int
        owner: string
        posted_at: DateTimeOffset
        title: string
        age: string
        gender: string
        height: string
        weight: string
        species: string
        content: string
        rating: string
        media: SubmissionMedia
        tags: string list
        friends_only: bool
        link: string
    }

    type Gallery = {
        submissions: GallerySubmission list
        backid: int option
        nextid: int option
    }

/// The starting point of a Weasyl gallery request.
type WeasylGalleryPosition =
| Beginning
| Next of nextid: int

/// Indicates the maximum number of items to return for a Weasyl gallery request.
type WeasylGalleryCount =
| Count of int

/// An object that allows for communication with the Weasyl API.
type WeasylClient(appInfo: ApplicationInformation, httpClientFactory: IHttpClientFactory, apiKeyProvider: IWeasylApiKeyProvider) =
    /// Makes an HTTP GET request.
    let getAsync (uri: string) = task {
        use client = httpClientFactory.CreateClient()

        use req = new HttpRequestMessage(HttpMethod.Get, uri)
        req.Headers.UserAgent.ParseAdd(appInfo.UserAgent)
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"))
        req.Headers.Add("X-Weasyl-API-Key", apiKeyProvider.ApiKey)

        return! client.SendAsync(req)
    }

    /// Makes a user gallery request to Weasyl.
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
        return! resp.Content.ReadFromJsonAsync<Weasyl.Gallery>()
    }

    /// Gets information on a single Weasyl artwork submission.
    let getSubmissionAsync (submitid: int) = task {
        use! resp = getAsync $"https://www.weasyl.com/api/submissions/{submitid}/view"
        if resp.StatusCode = System.Net.HttpStatusCode.NotFound then
            return None
        else
            ignore (resp.EnsureSuccessStatusCode())
            let! object = resp.Content.ReadFromJsonAsync<Weasyl.SubmissionDetail>()
            return Some object
    }

    /// Gets information on a single Weasyl journal entry.
    let getJournalAsync (journalid: int) = task {
        use! resp = getAsync $"https://www.weasyl.com/api/journals/{journalid}/view"
        if resp.StatusCode = System.Net.HttpStatusCode.NotFound then
            return None
        else
            ignore (resp.EnsureSuccessStatusCode())
            let! object = resp.Content.ReadFromJsonAsync<Weasyl.JournalDetail>()
            return Some object
    }

    /// Gets information on a single Weasyl character post.
    let getCharacterAsync (charid: int) = task {
        use! resp = getAsync $"https://www.weasyl.com/api/characters/{charid}/view"
        if resp.StatusCode = System.Net.HttpStatusCode.NotFound then
            return None
        else
            ignore (resp.EnsureSuccessStatusCode())
            let! object = resp.Content.ReadFromJsonAsync<Weasyl.CharacterDetail>()
            return Some object
    }

    /// Gets information on a single Weasyl user.
    let getUserAsync (user: string) = task {
        use! resp = getAsync $"https://www.weasyl.com/api/users/{Uri.EscapeDataString(user)}/view"
        ignore (resp.EnsureSuccessStatusCode())
        return! resp.Content.ReadFromJsonAsync<Weasyl.UserProfile>()
    }

    /// Gets and stores the username and ID of the user who issued the Weasyl API key.
    let whoamiLazy = lazy task {
        use! resp = getAsync $"https://www.weasyl.com/api/whoami"
        ignore (resp.EnsureSuccessStatusCode())
        return! resp.Content.ReadFromJsonAsync<Weasyl.Whoami>()
    }

    /// Gets and stores the user profile information of the user who issued the Weasyl API key.
    let userProfileLazy = lazy task {
        let! w = whoamiLazy.Value
        return! getUserAsync w.login
    }

    /// Returns user profile information from the Weasyl API for the logged-in user.
    member _.GetMyUserAsync() = task {
        return! userProfileLazy.Value
    }

    /// Returns submission information for an ID, unless it doesn't exist, wasn't posted by the logged-in user, or is set to friends only.
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

    /// Returns journal information for an ID, unless it doesn't exist, wasn't posted by the logged-in user, or is set to friends only.
    member _.GetMyPublicJournalAsync(journalid) = task {
        let! whoami = whoamiLazy.Value
        match! getJournalAsync journalid with
        | None ->
            return None
        | Some s ->
            if s.owner = whoami.login && not s.friends_only
            then return Some s
            else return None
    }

    /// Returns character information for an ID, unless it doesn't exist, wasn't posted by the logged-in user, or is set to friends only.
    member _.GetMyPublicCharacterAsync(charid) = task {
        let! whoami = whoamiLazy.Value
        match! getCharacterAsync charid with
        | None ->
            return None
        | Some s ->
            if s.owner = whoami.login && not s.friends_only
            then return Some s
            else return None
    }

    /// Returns all submissions, with limited information, for the logged-in user (newest to oldest).
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
