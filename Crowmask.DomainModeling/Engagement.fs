namespace Crowmask.DomainModeling

type Engagement = Boost of Boost | Like of Like | Reply of Reply
with
    member this.Id =
        match this with
        | Boost b -> b.id
        | Like l -> l.id
        | Reply r -> r.id
    member this.AddedAt =
        match this with
        | Boost b -> b.added_at
        | Like l -> l.added_at
        | Reply r -> r.added_at

type PostEngagement = {
    post: Post
    engagement: Engagement
}

module PostEngagement =
    let GetAll (post: Post) =
        seq {
            for i in post.boosts do Boost i
            for i in post.likes do Like i
            for r in post.replies do Reply r
        }
        |> Seq.sortBy (fun e -> e.AddedAt)
        |> Seq.map (fun e -> { post = post; engagement = e })
        |> Seq.toList
