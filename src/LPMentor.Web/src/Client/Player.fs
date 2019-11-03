module Player

open Fable.Core.JsInterop
open Fable.Core.JS
open Fable.React
open Fable.React.ReactBindings
// open Fable.React.Props

open PlayerInternal

let private player =
    (fun props ->
        div [] [
            PlayButton [] []
            h2 [] []
            Timer [] []
            Progress [] []
            VolumeControl [] []
        ]
    )
    |> withCustomAudio 

let LPMemtorAudioPlayer (props: AudioComponentProp seq) =
    React.createElement(player, AudioComponentProp.ToPropObj(props), [])
