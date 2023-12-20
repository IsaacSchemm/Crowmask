namespace Crowmask.ActivityPub

open System.Threading.Tasks

type IPublicKey =
    abstract member Pem: string

type IPublicKeyProvider =
    abstract member GetPublicKeyAsync: unit -> Task<IPublicKey>
