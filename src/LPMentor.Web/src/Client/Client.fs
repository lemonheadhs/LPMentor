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

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { 
    Lessons: Lecture.Model
    Search: Search.Model
    NextToken: TableToken
}

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| Lesson of Lecture.Msg * TableToken option
| Search of Search.Msg


// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    
    let lessonsModel, cmds1 = Lecture.init ()
    let searchModel, cmds2 = Search.init ()
    { Lessons = lessonsModel
      Search = searchModel
      NextToken = TableToken.empty },
    Cmd.map (fun l -> Msg.Lesson (l, None)) cmds1 @ Cmd.map Search cmds2

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currModel : Model) : Model * Cmd<Msg> =
    match msg with
    | Lesson (lessonMsg, tokenOption) ->
        let m, c = Lecture.update lessonMsg currModel.Lessons
        { currModel with Lessons = m }, 
        Cmd.map Msg.Lesson c

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
                          Some t) |> Msg.Lesson)
            | RequestNext ->
                let reqNextFn = chooseReqFn currModel.Search.SearchTerm
                Cmd.OfAsync.perform reqNextFn currModel.NextToken
                    (fun (ls, t) ->
                        ( ls |> List.ofArray |> Lecture.Append,
                          Some t) |> Msg.Lesson)

        let m, c = Search.update searchMsg currModel.Search
        { currModel with Search = m },
        Cmd.map Msg.Search c @ cmd

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
                [ filter IsLink "Link"
                  filter IsSuccess "Success"
                  filter IsBlack "Black"
                  filter IsWarning "Warning"
                  filter IsDanger "Danger"
                  filter IsInfo "Info" ] ] ]

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

let temp =
    body [ ]
        [ appNav
          searchBox
          quickFilters
          section [ Class "container" ]
            [ Level.item []
                [ Columns.columns [ Columns.Option.IsMultiline; Columns.Option.IsCentered ; Columns.Option.CustomClass "cards-container";
                                    Columns.Option.Props [Id "sectioncontainer" ]]
                    [ LectureCard IsBlack "Season 1" 
                        ["The Fort"; "Fist Like a bullet"; "White Stork Spreads Wings"; "Two Tigers Subdue Dragons"; "Snake Creeps Down"; "Hand of Five Poisons"]
                      LectureCard IsPrimary "Season 2"
                        ["Tiger Pushes Mountain"; "Force of Eagle's Claw"; "Red Sun, Silver Moon"; "Palm of the Iron Fox"; "Monkey Leaps Through Mist" 
                         "Leopard Stalks in Snow"; "Black Heart, White Mountain"; "Sting of the Scorpion's Tail"; "Nightingale Sings No More"
                         "Wolf's Breath, Dragon Fire"] 
                      LectureCard IsLink "Season 3"
                        ["Enter the Phoenix"; "Moon Rises, Raven Seeks"; "Leopard Snares Rabbit"; "Blind Cannibal Assassins"; "Carry Tiger to Mountain";
                         "Black Wind Howls"; "Dragonfly's Last Dance"; "Leopard Catches Cloud"; "Chamber of the Scorpion"; "Raven's Feather, Phoenix Blood";
                         "The Boar And The Butterfly"; "Cobra Fang, Panther Claw"; "Black Lothus, White Rose"; "Curse of the Red Rain"; 
                         "Requiem for the Fallen"; "Seven Strike as One"]
                      LectureCard IsInfo "Info"
                        ["Bronchy"; "Aorta"; "Alveolae"; "TALISMAN"]
                      LectureCard IsSuccess "Success"
                        ["signature"; "weasel"; "solana"; "hydro"]
                      LectureCard IsWarning "Warning"
                        ["Ganimede"; "Europa"; "Tycho"; "Io"] ] ] ]
          Columns.columns [ Columns.Option.IsMobile; Columns.Option.IsCentered ]
            [ Column.column [ Column.Option.Width (Screen.All, Column.ISize.IsHalf)
                              Column.Option.Width (Screen.All, Column.ISize.IsNarrow) ]
                [ ] ]
          appFooter
        ]

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        [ temp ]

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
