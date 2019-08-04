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
type Model = { Counter: Counter option }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| Increment
| Decrement
| InitialCountLoaded of Counter

module Server =

    open Shared
    open Fable.Remoting.Client

    /// A proxy you can use to talk to server directly
    let api : ICounterApi =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<ICounterApi>
let initialCounter = Server.api.initialCounter

// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let initialModel = { Counter = None }
    let loadCountCmd =
        Cmd.OfAsync.perform initialCounter () InitialCountLoaded
    initialModel, loadCountCmd

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match currentModel.Counter, msg with
    | Some counter, Increment ->
        let nextModel = { currentModel with Counter = Some { Value = counter.Value + 1 } }
        nextModel, Cmd.none
    | Some counter, Decrement ->
        let nextModel = { currentModel with Counter = Some { Value = counter.Value - 1 } }
        nextModel, Cmd.none
    | _, InitialCountLoaded initialCount->
        let nextModel = { Counter = Some initialCount }
        nextModel, Cmd.none
    | _ -> currentModel, Cmd.none


let safeComponents =
    let components =
        span [ ]
           [ a [ Href "https://github.com/SAFE-Stack/SAFE-template" ]
               [ str "SAFE  "
                 str Version.template ]
             str ", "
             a [ Href "https://github.com/giraffe-fsharp/Giraffe" ] [ str "Giraffe" ]
             str ", "
             a [ Href "http://fable.io" ] [ str "Fable" ]
             str ", "
             a [ Href "https://elmish.github.io" ] [ str "Elmish" ]
             str ", "
             a [ Href "https://fulma.github.io/Fulma" ] [ str "Fulma" ]
             str ", "
             a [ Href "https://zaid-ajaj.github.io/Fable.Remoting/" ] [ str "Fable.Remoting" ]

           ]

    span [ ]
        [ str "Version "
          strong [ ] [ str Version.app ]
          str " powered by: "
          components ]

let show = function
| { Counter = Some counter } -> string counter.Value
| { Counter = None   } -> "Loading..."

let button txt onClick =
    Button.button
        [ Button.IsFullWidth
          Button.Color IsPrimary
          Button.OnClick onClick ]
        [ str txt ]

let oldview (model : Model) (dispatch : Msg -> unit) =
    div []
        [ Navbar.navbar [ Navbar.Color IsPrimary ]
            [ Navbar.Item.div [ ]
                [ Heading.h2 [ ]
                    [ str "SAFE Template" ] ] ]

          Container.container []
              [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ Heading.h3 [] [ str ("Press buttons to manipulate counter: " + show model) ] ]
                Columns.columns []
                    [ Column.column [] [ button "-" (fun _ -> dispatch Decrement) ]
                      Column.column [] [ button "+" (fun _ -> dispatch Increment) ] ] ]

          Footer.footer [ ]
                [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ safeComponents ] ] ]

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
