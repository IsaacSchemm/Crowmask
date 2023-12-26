namespace Crowmask.ActivityPub

type IRemoteActorDisplay =
    abstract member Id: string
    abstract member DisplayName: string
