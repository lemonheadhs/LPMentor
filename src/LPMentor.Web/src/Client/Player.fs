module Player

open Fable.Core
open Fable.Core.JsInterop
open Fable.Core.JS
open Fable.React
open Fable.React.ReactBindings
open Fable.React.Props
open Browser.Types
open Elmish
open Fulma

open PlayerInternal

type Model = {
    Playing: bool
    TrackUrl: string option
    TrackName: string option
    Progress: int
    Volume: float
    IsMuted: bool
    CurrentTime: float
    Duration: float
    Soundplayer: SoundCloudAudio
    DispatchFnRegister: (Msg -> unit) -> unit
    StartRefreshTimer: unit -> unit
    StopRefreshTimer: unit -> unit
}

and Msg = 
| Play
| Pause
| ChangeRecord of string * string
| ChangeVolume of float
| Mute
| Unmute
| Seek of float
| UpdateDuration of float
| UpdateCurrentTime of float

let init () : Model * Cmd<Msg> =
    let soundplayer = SoundCloudAudio.Create(null, "temp")
    let dispatchFns = ResizeArray<Msg -> unit>()
    let register fn =
        dispatchFns.Clear()
        dispatchFns.Add(fn)
    soundplayer.audio.addEventListener("loadedmetadata", 
        fun e -> dispatchFns |> Seq.iter (fun fn -> soundplayer.audio.duration |> UpdateDuration |> fn))
    let timer = 
        let timer = new System.Timers.Timer()
        timer.Interval <- 500.
        timer.AutoReset <- true
        timer.Elapsed.AddHandler(fun sender e -> dispatchFns |> Seq.iter (fun fn -> soundplayer.audio.currentTime |> UpdateDuration |> fn))
        timer
    let startTimer () = timer.Start()
    let stopTimer () = timer.Stop()

    { Playing = false
      TrackUrl = None
      TrackName = None
      Progress = 0
      Volume = 0.6
      IsMuted = false
      CurrentTime = 0.
      Duration = 0.
      Soundplayer = soundplayer
      DispatchFnRegister = register
      StartRefreshTimer = startTimer
      StopRefreshTimer = stopTimer },
    Cmd.Empty

let update (msg: Msg) (currModel: Model) : Model * Cmd<Msg> =
    match msg with
    | Play -> 
        currModel.StartRefreshTimer()
        { currModel with Playing = true }, Cmd.Empty
    | Pause -> 
        currModel.StopRefreshTimer()
        { currModel with Playing = false }, Cmd.Empty
    | ChangeRecord (url, name) ->
        if Option.isSome currModel.TrackUrl && url = Option.get(currModel.TrackUrl) then
            currModel, Cmd.Empty
        else
            currModel.Soundplayer.preload(url, "metadata")
            { currModel with
                TrackName = Some name
                TrackUrl = Some url
                CurrentTime = 0. },
            Msg.Pause |> Cmd.ofMsg
    | ChangeVolume newVolume ->
        { currModel with
            Volume = newVolume },
        Cmd.Empty
    | Mute -> { currModel with IsMuted = true }, Cmd.Empty
    | Unmute -> { currModel with IsMuted = false }, Cmd.Empty
    | Seek location ->
        { currModel with
            CurrentTime = location * currModel.Duration },
        Cmd.Empty
    | UpdateDuration duration ->
        { currModel with Duration = duration }, Cmd.Empty
    | UpdateCurrentTime currTime ->
        { currModel with CurrentTime = currTime }, Cmd.Empty

let private player =
    (fun props ->
        let soundplayer = props?soundCloudAudio :> SoundCloudAudio
        let playing : bool = props?playing
        let onTogglePlay: unit -> unit = props?onTogglePlay
        let volume: float = props?volume
        
        div [ Class "playerContainer flex" ] [
            PlayButton [ PlayButtonProp.SoundCloudAudio soundplayer ] []
            div [ Class "flex flexAuto"; 
                  Props.Style [ FlexDirection "column"; FlexWrap "nowrap";
                                MarginLeft "1rem"; MarginRight "1rem" ] ] [
                div [ Class "flex row flexAuto"; Props.Style [ JustifyContent "space-between"] ] [
                    h2 [] [ str "test" ]
                    Timer [ TimerProp.SoundCloudAudio soundplayer ] []
                ]
                div [ Class "flex row flexAuto"; Props.Style [ AlignItems AlignItemsOptions.Center] ] [
                    VolumeControl [ VolumeProp.SoundCloudAudio soundplayer ] []
                    Progress [ ProgressProp.ClassName "flexAuto"; ProgressProp.SoundCloudAudio soundplayer ] []
                ]
            ]
        ]
    )
    |> withCustomAudio 

let LPMemtorAudioPlayer (props: AudioComponentProp seq) =
    let soundplayer = SoundCloudAudio.Create(null, "temp")
    console.log(soundplayer)
    let props' = 
        seq [ yield AudioComponentProp.SoundCloudAudio soundplayer
              yield! props ]
    React.createElement(player, AudioComponentProp.ToPropObj(props'), [])


let view (model: Model) (dispatch: Msg -> unit) =
    model.DispatchFnRegister dispatch

    let togglePlayMsg () =
        if model.Playing then Msg.Pause else Msg.Play

    let toggleMuteMsg mute e =
        if model.IsMuted then Msg.Unmute else Msg.Mute
        |> dispatch

    let changeVolumeMsg xPos e = 
        xPos 
        |> ChangeVolume
        |> dispatch

    let seekTrackMsg xPos e = Msg.Seek xPos |> dispatch

    if Option.isSome model.TrackUrl then
        Container.container [] [
            div [ Class "playerContainer flex" ] [
                    PlayButton [ PlayButtonProp.SoundCloudAudio model.Soundplayer
                                 PlayButtonProp.Playing model.Playing
                                 PlayButtonProp.OnTogglePlay (togglePlayMsg >> dispatch) ] []
                    div [ Class "flex flexAuto"; 
                          Props.Style [ FlexDirection "column"; FlexWrap "nowrap";
                                        MarginLeft "1rem"; MarginRight "1rem" ] ] [
                        div [ Class "flex row flexAuto"; Props.Style [ JustifyContent "space-between"] ] [
                            h2 [] [ str (Option.defaultValue "" model.TrackName) ]
                            Timer [ TimerProp.SoundCloudAudio model.Soundplayer
                                    TimerProp.CurrentTime model.Soundplayer.audio.currentTime
                                    TimerProp.Duration model.Soundplayer.audio.duration ] []
                        ]
                        div [ Class "flex row flexAuto"; Props.Style [ AlignItems AlignItemsOptions.Center] ] [
                            VolumeControl [ VolumeProp.SoundCloudAudio model.Soundplayer
                                            VolumeProp.IsMuted model.IsMuted
                                            VolumeProp.Volume model.Volume
                                            VolumeProp.OnToggleMute (toggleMuteMsg)
                                            VolumeProp.OnVolumeChange (changeVolumeMsg) ] []
                            Progress [ ProgressProp.ClassName "flexAuto"; ProgressProp.SoundCloudAudio model.Soundplayer
                                       ProgressProp.CurrentTime model.Soundplayer.audio.currentTime
                                       ProgressProp.Duration model.Soundplayer.audio.duration
                                       ProgressProp.OnSeekTrack (seekTrackMsg) ] []
                        ]
                    ]
                ]
        ]
    else
        str ""