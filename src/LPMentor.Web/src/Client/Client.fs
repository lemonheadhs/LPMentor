module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Thoth.Json
open Fable.FontAwesome

open Shared
open Lecture
open Search
open PlayerInternal
open Player

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { 
    Lessons: Lecture.Model
    Search: Search.Model
    Player: Player.Model
    NextToken: TableToken
}

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| Lesson of Lecture.Msg * TableToken
| Search of Search.Msg
| Player of Player.Msg


// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    
    let lessonsModel, cmds1 = Lecture.init ()
    let searchModel, cmds2 = Search.init ()
    let playerModel, _ = Player.init ()
    { Lessons = lessonsModel
      Search = searchModel
      Player = playerModel
      NextToken = TableToken.empty },
    Cmd.map (fun l -> Msg.Lesson (l, TableToken.empty)) cmds1 @ Cmd.map Search cmds2

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currModel : Model) : Model * Cmd<Msg> =
    match msg with
    | Lesson (lessonMsg, token) ->
        let c1 =
            match lessonMsg with
            | SelectLesson (topic, section, url) ->
                Player.Msg.ChangeRecord (sprintf "/audios/%s" url, sprintf "%s - %s" topic section) 
                |> Msg.Player |> Cmd.ofMsg
            | _ -> Cmd.Empty
        let m, c = Lecture.update lessonMsg currModel.Lessons
        { currModel with 
            Lessons = m
            NextToken = token }, 
        (Cmd.map Msg.Lesson c) @ c1

    | Search searchMsg ->
        let cmd =
            let chooseReqFn = function        
                | Some term -> LessonAPIClient.searchTopic term
                | None      -> LessonAPIClient.init
            match searchMsg with
            | ConfirmSearch strOption ->
                let reqFn = chooseReqFn strOption
                Cmd.OfAsync.perform reqFn TableToken.empty
                    (fun (ls, t) ->
                        ( ls |> List.ofArray |> Lecture.Full,
                          t) |> Msg.Lesson)
            | RequestNext ->
                let reqNextFn = chooseReqFn currModel.Search.SearchTerm
                Cmd.OfAsync.perform reqNextFn currModel.NextToken
                    (fun (ls, t) ->
                        ( ls |> List.ofArray |> Lecture.Append,
                          t) |> Msg.Lesson)

        let m, c = Search.update searchMsg currModel.Search
        { currModel with Search = m },
        Cmd.map Msg.Search c @ cmd

    | Player playerMsg ->
        let m, c = Player.update playerMsg currModel.Player
        { currModel with Player = m },
        Cmd.map Msg.Player c

let appNav =
    Navbar.navbar [ Navbar.HasShadow ]
        [ Container.container []
            [ Navbar.Brand.div []
                [ Navbar.Item.a [ Navbar.Item.Option.Props [Href "../"] ]
                    [ img [ Src "http://bulma.io/images/bulma-logo.png"
                            Alt "Bulma: a modern CSS framework based on Flexbox" ] ]
                  Navbar.burger [ CustomClass "burger"
                                  Props [HTMLAttr.Custom ("data-target", "navMenu") ]]
                    [ span [ ] [ ]
                      span [ ] [ ]
                      span [ ] [ ] ] ]
              Navbar.menu [ Navbar.Menu.Props [ Id "navMenu" ]]
                [ Navbar.End.div []
                    [ Navbar.Item.div [ Navbar.Item.Option.IsHoverable; Navbar.Item.Option.HasDropdown ]
                        [ Navbar.Link.a []
                            [ str "Account" ]
                          Navbar.Dropdown.div []
                            [ Navbar.Item.a []
                                [ str "Dashboard" ]
                              Navbar.Item.a []
                                [ str "Profile" ]
                              Navbar.Item.a []
                                [ str "Settings" ]
                              hr [ Class "navbar-divider" ]
                              Navbar.Item.div []
                                [ str "Logout" ] ] ] ] ] ] ]

let quickFilters =
    let filter (c:IColor) title =
        Control.div []
            [ Tag.tag [ Tag.Option.Size IsLarge; Tag.Option.Color c ]
                [ str title ] ]
    Box.box' [ CustomClass "cta" ]
        [ Columns.columns [ Columns.Option.IsCentered; Columns.Option.IsMobile ]
            [ Field.div [ Field.Option.IsGrouped; Field.Option.IsGroupedMultiline ]
                [ // filter IsLink "Link"
                  // filter IsSuccess "Success"
                  // filter IsBlack "Black"
                  // filter IsWarning "Warning"
                  // filter IsDanger "Danger"
                  // filter IsInfo "Info" 
                  ] ] ]

let audioPlayer =
    Container.container [] [
        LPMemtorAudioPlayer [ AudioComponentProp.StreamUrl "http://localhost/2_GrowthOfFunctions.mp3" ]
    ]


let appFooter =
    footer [ ]
        [ Box.box' [ CustomClass "cta" ]
            [ Columns.columns [ Columns.Option.IsMobile; Columns.Option.IsCentered ]
                [ Field.div [ Field.Option.IsGrouped; Field.Option.IsGroupedMultiline ]
                    [ Control.div []
                        [ Tag.list [ Tag.List.Option.HasAddons ]
                            [ a [ Class "tag is-link"
                                  Href "https://github.com/dansup/bulma-templates" ]
                                [ str "Bulma Templates" ]
                              Tag.tag [ Tag.Option.Color IsLight ]
                                [ str "Daniel Supernault" ] ] ]
                      Control.div []
                        [ Tag.list [ Tag.List.Option.HasAddons ]
                            [ a [ Class "tag is-link" ]
                                [ str "The source code is licensed" ]
                              Tag.tag [ Tag.Option.Color IsLight ]
                                [ str "MIT"
                                  Fa.i [ Fa.Brand.Github ]
                                    [ ] ] ] ] ] ] ] ]

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        [ appNav
          Search.view model.Search (Msg.Search >> dispatch)
          // quickFilters
          // audioPlayer
          Player.view model.Player (Msg.Player >> dispatch)
          Lecture.view model.Lessons (fun ls -> Msg.Lesson (ls, model.NextToken) |> dispatch)
          Columns.columns [ Columns.Option.IsMobile; Columns.Option.IsCentered ]
            [ Column.column [ Column.Option.Width (Screen.All, Column.ISize.IsHalf)
                              Column.Option.Width (Screen.All, Column.ISize.IsNarrow) ]
                [ ] ]
          appFooter ]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
