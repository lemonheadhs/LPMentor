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
    let audiosContainer = Azure.Containers.audios
