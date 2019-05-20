module LPMentor.Core.Models


[<CLIMutable>]
type NoteInfo = {
    Text: string
    Topic: string
    Section: string
    Order: int
    Lang: string
}
type public NoteInfoEx = {
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


