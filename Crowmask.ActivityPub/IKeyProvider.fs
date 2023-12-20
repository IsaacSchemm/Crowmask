namespace Crowmask.ActivityPub

open System.Threading.Tasks

type IKeyProvider =
    abstract member GetPublicKeyAsync: unit -> Task<IPublicKey>
    abstract member SignRsaSha256Async: byte array -> Task<byte array>
