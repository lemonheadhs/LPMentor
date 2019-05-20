module LPMentor.Storage

open System
open FSharp.Azure.StorageTypeProvider

open LPMentor.Core.Models

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
