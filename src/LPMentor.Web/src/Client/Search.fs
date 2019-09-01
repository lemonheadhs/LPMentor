module Search

open Fulma
open Fulma.Common
open Fable.FontAwesome
open Fable.React
open Fable.React.Props
open Elmish
open Fable.Remoting.Client
open Fable.Core.JsInterop

open Shared
open Microsoft.FSharp.Control
open System
open Browser.Types

type Model = {
    SearchTerm: string option
}

type Msg =
| ConfirmSearch of string option
| RequestNext

type Uplink = {
    LessonChannel: Lecture.Msg -> unit
}

let LessonAPIClient =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ILessonSearchApi>

let init () =
    { SearchTerm = None },
    Cmd.none

let update (msg: Msg) (currModel: Model) : Model * Cmd<Msg>=    
    match msg with
    | ConfirmSearch strOption ->
        { currModel with SearchTerm = strOption },
        Cmd.none
    | RequestNext ->
        currModel, Cmd.none

let view (model: Model) (dispatch: Msg -> unit) =
    // { new IObservable<string> with
    //     member _.Subscribe() }
    let test (ke: KeyboardEvent) =
        Fable.Core.JsInterop.createObj
        Browser.Dom.console.log (toJson ke)
        
    let inputControl =
        Control.div [ Control.HasIconLeft; Control.HasIconRight ]
            [ Input.input [ Input.Option.Size IsLarge
                            Input.Option.Type Input.IInputType.Search
                            Input.Option.Props [ OnKeyUp (test) ] ]
              Icon.icon [ Icon.Option.IsLeft; Icon.Option.Size IsMedium ]
                [ Fa.i [ Fa.Solid.Search ] [ ] ]
              Icon.icon [ Icon.Option.IsRight; Icon.Option.Size IsMedium ]
                [ Fa.i [ Fa.Brand.Empire ] [ ] ] ] 
    section [ Class "hero is-info" ]
        [ Hero.body []
            [ Container.container []
                [ Card.card []
                    [ Card.content []
                        [ Content.content []
                            [ inputControl ] ] ] ] ] ]            
    

let searchBox =
    let inputControl =
        Control.div [ Control.HasIconLeft; Control.HasIconRight ]
            [ Input.input [ Input.Option.Size IsLarge
                            Input.Option.Type Input.IInputType.Search ]
              Icon.icon [ Icon.Option.IsLeft; Icon.Option.Size IsMedium ]
                [ Fa.i [ Fa.Solid.Search ] [ ] ]
              Icon.icon [ Icon.Option.IsRight; Icon.Option.Size IsMedium ]
                [ Fa.i [ Fa.Brand.Empire ] [ ] ] ] 
    section [ Class "hero is-info" ]
        [ Hero.body []
            [ Container.container []
                [ Card.card []
                    [ Card.content []
                        [ Content.content []
                            [ inputControl ] ] ] ] ] ]



