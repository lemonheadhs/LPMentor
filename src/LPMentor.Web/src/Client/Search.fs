module Search

open Fulma
open Fulma.Common
open Fable.FontAwesome
open Fable.React
open Fable.React.Props
open Elmish
open Fable.Remoting.Client

open Shared
open Microsoft.FSharp.Control
open System

type Model = {
    SearchTerm: string option
    ContinuationToken: TableToken
}

type Msg =
| ConfirmSearch of string option
| RequestNext
| NextToken of TableToken

type Uplink = {
    LessonChannel: Lecture.Msg -> unit
}

let LessonAPIClient =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ILessonSearchApi>

let init (uplink: Uplink) () =
    { SearchTerm = None; ContinuationToken = TableToken.empty },
    Cmd.OfAsync.perform LessonAPIClient.init TableToken.empty 
        (fun (ls, t) -> 
            ls |> List.ofArray |> Lecture.Full |> uplink.LessonChannel
            NextToken t)

let update (uplink: Uplink) (msg: Msg) (currModel: Model) : Model * Cmd<Msg>=
    let chooseReqFn = function        
        | Some term -> LessonAPIClient.searchTopic term
        | None      -> LessonAPIClient.init
    match msg with
    | ConfirmSearch strOption ->
        let reqFn = chooseReqFn strOption
        { currModel with SearchTerm = strOption },
        Cmd.OfAsync.perform reqFn TableToken.empty
            (fun (ls, t) ->
                ls |> List.ofArray |> Lecture.Full |> uplink.LessonChannel
                NextToken t)
    | RequestNext ->
        let reqNextFn = chooseReqFn currModel.SearchTerm
        currModel,
        Cmd.OfAsync.perform reqNextFn currModel.ContinuationToken
            (fun (ls, t) ->
                ls |> List.ofArray |> Lecture.Append |> uplink.LessonChannel
                NextToken t)
    | NextToken t ->
        { currModel with ContinuationToken = t },
        Cmd.Empty

let view (model: Model) (dispatch: Msg -> unit) =
    // { new IObservable<string> with
    //     member _.Subscribe() }
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



