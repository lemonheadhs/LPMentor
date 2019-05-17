module LPMentor.Storage

open System
open FSharp.Azure.StorageTypeProvider

[<CLIMutable>]
type NoteInfo = {
    Text: string
    Topic: string
    Section: string
    Order: int
    Lang: string
}
type NoteInfoEx = {
    Partition: string
    RowId: string
    BaseInfo: NoteInfo
}
[<CLIMutable>]
type AudioNoteInfo = {
    Text: string
    Topic: string
    Section: string
    Order: int
    Lang: string
    BlobUrl: string
}
type AudioNoteInfoEx = {
    Partition: string
    RowId: string
    BaseInfo: AudioNoteInfo
}

type Azure = AzureTypeProvider<"UseDevelopmentStorage=true">
let noteQueue = Azure.Queues.notes
let lpnote = Azure.Tables.LPNote
let lpaudio = Azure.Tables.LPAudio
let audiosContainer = Azure.Containers.audios

type NoteEntity = Azure.Domain.LPNoteEntity

let mapTo (entity: NoteEntity) : NoteInfoEx =
    {
        Partition = entity.PartitionKey
        RowId = entity.RowKey
        BaseInfo = {
            Text = entity.Text
            Topic = entity.Topic
            Section = entity.Section
            Order = entity.Order
            Lang = entity.Lang
        }
    }

let mapToAudioNote blobUrl (note: NoteInfoEx) : AudioNoteInfoEx =
    {
        Partition = note.Partition
        RowId = note.RowId
        BaseInfo = {
            Text = note.BaseInfo.Text
            Topic = note.BaseInfo.Topic
            Section = note.BaseInfo.Section
            Order = note.BaseInfo.Order
            Lang = note.BaseInfo.Lang
            BlobUrl = blobUrl
        }
    }
