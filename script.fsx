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
        Concept "Tags or Commments? - a way to group a set of materials, so later we can stitch them together"
        Concept "Evernote Webhook - get notified once all materials around one topic is ready"
    ]
}

let AzureTTS_ds = {
    Topic = AzureTTS
    Parts = [
        Concept "Azure Function - get notified by Evernote Webhook, extract text contents from note, and feed them to TTL"
        Concept "Azure Speech Service in Cognitive Services as TTL"
    ]
}

let AudioStoreOnAzure_ds = {
    Topic = AudioStoreOnAzure
    Parts =[
        Concept "Azure Blob Storage - a convenient service to store files"
        Concept "How to retrieve audio files from Azure blobs?"
    ]
}


