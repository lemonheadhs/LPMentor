open System


type Concept = Concept of string
type Design = {
    Topic: Concept
    Parts: Concept list
}

let EvernoteRaker = Concept ""

let AzureTTS = Concept ""

let AudioStoreOnAzure = Concept ""


let EvernoteRaker_ds = {
    Topic = EvernoteRaker
    Parts = [
        Concept "Evernote Web Clipper - gather materials online"
        Concept "Evernote - store text materials as notes"
        Concept "Tags? - a way to group a set of materials, so later we can stitch them together"
    ]
}
