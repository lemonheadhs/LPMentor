module PlayerInternal

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Browser.Types
// open Fable.Helpers.React
// open Fable.Import.React

type SoundCloudAudio =    
    abstract play: unit -> unit
    [<Emit("$0.play({ streamUrl: $1 })")>]
    abstract playURI: string -> unit
    abstract next: unit -> unit
    abstract on: string * (unit -> unit) -> unit
    
    abstract playing: bool with get
    abstract duration: int with get
    abstract audio: HTMLMediaElement with get
    
type SoundCloudAudioStatic =
    [<Emit("new $0()")>]
    abstract Create: unit -> SoundCloudAudio

[<ImportDefault("soundcloud-audio")>]
let SoundCloudAudio : SoundCloudAudioStatic = jsNative

type PlayButtonProp =
| Playing of bool
| Seeking of bool
| ClassName of string
| SoundCloudAudio of SoundCloudAudio
| OnTogglePlay of (unit -> unit)

let PlayButton (props: PlayButtonProp seq) (elems: ReactElement seq) =
    ofImport "PlayButton" "react-soundplayer/components" (keyValueList CaseRules.LowerFirst props) elems

type TimerProp =
| CurrentTime of int
| ClassName of string
| Style of obj
| SoundCloudAudio of SoundCloudAudio
| Duration of int

let Timer (props: TimerProp seq) (elems: ReactElement seq) =
    ofImport "Timer" "react-soundplayer/components" (keyValueList CaseRules.LowerFirst props) elems

type ProgressProp =
| OnSeekTrack of (unit -> unit)
| ClassName of string
| InnerClassName of string
| Style of obj
| InnerStyle of obj
| CurrentTime of int
| Duration of int
| Value of int
| SoundCloudAudio of SoundCloudAudio

let Progress (props: ProgressProp seq) (elems: ReactElement seq) =
    ofImport "Progress" "react-soundplayer/components" (keyValueList CaseRules.LowerFirst props) elems

type VolumeProp =
| OnVolumeChange of (int * obj -> unit)
| OnToggleMute of (bool * obj -> unit)
| IsMuted of bool
| ClassName of string
| ButtonClassName of string
| RangeClassName of string
| Volume of int
| SoundCloudAudio of SoundCloudAudio

let VolumeControl (props: VolumeProp seq) (elems: ReactElement seq) =
    ofImport "VolumeControl" "react-soundplayer/components" (keyValueList CaseRules.LowerFirst props) elems

[<StringEnum>]
type PreloadType =
| None
| Metadata
| Auto

type AudioComponentProp =
| StreamUrl of string
| PreloadType of PreloadType
| SoundCloudAudio of SoundCloudAudio
| OnReady of (unit -> unit)
| OnStartTrack of (SoundCloudAudio * bool -> unit)
| OnPauseTrack of (SoundCloudAudio -> unit)
| OnStopTrack of (SoundCloudAudio -> unit)
| OnCanPlayTrack of (SoundCloudAudio -> unit)
| Custom of string * obj
with 
    static member ToPropObj (props: AudioComponentProp seq) =
        let nonCustoms, customs =
            props
            |> List.ofSeq
            |> List.partition (function | Custom _ -> false | _ -> true)
        JS.Object.assign(
            (keyValueList CaseRules.LowerFirst nonCustoms),
            customs 
            |> List.map (function 
                         | Custom (k,v) -> k, v
                         | _ -> failwith<string * obj> "") 
            |> createObj
        )
        
[<ImportMember("react-soundplayer/addons")>]        
let withCustomAudio : (obj -> ReactElement) -> obj = jsNative




