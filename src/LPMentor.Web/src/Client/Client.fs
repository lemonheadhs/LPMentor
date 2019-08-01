module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fulma
open Thoth.Json

open Shared
open Fable.FontAwesome

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

let view (model : Model) (dispatch : Msg -> unit) =
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
                                  Props [HTMLAttr.Data ("target", "navMenu") ]]
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

let temp =
    body [ ]
        [ appNav
          section [ Class "hero is-info" ]
            [ Hero.body []
                [ Container.container []
                    [ Card.card []
                        [ Card.content []
                            [ Content.content []
                                [ Control.div [ Control.HasIconLeft; Control.HasIconRight ]
                                    [ Input.input [ Input.Option.Size IsLarge
                                                    Input.Option.Type Input.IInputType.Search ]
                                      Icon.icon [ Icon.Option.IsLeft; Icon.Option.Size IsMedium ]
                                        [ Fa.i [ Fa.Solid.Search ] [ ] ]
                                      Icon.icon [ Icon.Option.IsRight; Icon.Option.Size IsMedium ]
                                        [ Fa.i [ Fa.Brand.Empire ] [ ] ] ] ] ] ] ] ] ]
          Box.box' [ CustomClass "cta" ]
            [ Columns.columns [ Columns.Option.IsCentered; Columns.Option.IsMobile ]
                [ Field.div [ Field.Option.IsGrouped; Field.Option.IsGroupedMultiline ]
                    [ Control.div []
                        [ Tag.tag [ Tag.Option.Size IsLarge; Tag.Option.Color IsLink ]
                            [ str "Link" ] ]
                      Control.div []
                        [ Tag.tag [ Tag.Option.Color IsSuccess; Tag.Option.Size IsLarge ]
                            [ str "Success" ] ]
                      Control.div []
                        [ Tag.tag [ Tag.Option.Color IsBlack; Tag.Option.Size IsLarge ]
                            [ str "Black" ] ]
                      Control.div []
                        [ Tag.tag [ Tag.Option.Color IsWarning; Tag.Option.Size IsLarge ]
                            [ str "Warning" ] ]
                      Control.div []
                        [ Tag.tag [ Tag.Option.Color IsDanger; Tag.Option.Size IsLarge ]
                            [ str "Danger" ] ]
                      Control.div []
                        [ Tag.tag [ Tag.Option.Color IsInfo; Tag.Option.Size IsLarge ]
                            [ str "Info" ] ] ] ] ]
          section [ Class "container" ]
            [ Level.item []
                [ Columns.columns [ Columns.Option.IsMultiline; Columns.Option.IsCentered ; Columns.Option.CustomClass "cards-container";
                                    Columns.Option.Props [Id "sectioncontainer" ]]
                    [ Column.column [ Column.Option.Width (Screen.All, Column.ISize.IsNarrow) ]
                        [ Message.message [ Message.Option.Color IsBlack ]
                            [ Message.header []
                                [ p [] [ str "Season 1" ]
                                  Notification.delete [ Props [HTMLAttr.Custom ("aria-label", "delete") ]] [] ]
                              Message.body []
                                [ div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "The Fort" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Fist Like a bullet" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "White Stork Spreads Wings" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Two Tigers Subdue Dragons" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Snake Creeps Down" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Hand of Five Poisons" ] ] ] ] ] ]
                      div [ Class "column is-narrow" ]
                        [ article [ Class "message is-primary" ]
                            [ div [ Class "message-header" ]
                                [ p [ ]
                                    [ str "Season 2" ]
                                  Standard.button [ Class "delete"
                                                    HTMLAttr.Custom ("aria-label", "delete") ]
                                    [ ] ]
                              div [ Class "message-body" ]
                                [ div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Tiger Pushes Mountain" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Force of Eagle's Claw" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Red Sun, Silver Moon" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Palm of the Iron Fox" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Monkey Leaps Through Mist" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Leopard Stalks in Snow" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Black Heart, White Mountain" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Sting of the Scorpion's Tail" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Nightingale Sings No More" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Wolf's Breath, Dragon Fire" ] ] ] ] ] ]
                      div [ Class "column is-narrow" ]
                        [ article [ Class "message is-link" ]
                            [ div [ Class "message-header" ]
                                [ p [ ]
                                    [ str "Season 3" ]
                                  Standard.button [ Class "delete"
                                                    HTMLAttr.Custom ("aria-label", "delete") ]
                                    [ ] ]
                              div [ Class "message-body" ]
                                [ div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Enter the Phoenix" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Moon Rises, Raven Seeks" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Leopard Snares Rabbit" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Blind Cannibal Assassins" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Carry Tiger to Mountain" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Black Wind Howls" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Dragonfly's Last Dance" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Leopard Catches Cloud" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Chamber of the Scorpion" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Raven's Feather, Phoenix Blood" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "The Boar And The Butterfly" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Cobra Fang, Panther Claw" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Black Lothus, White Rose" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Curse of the Red Rain" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Requiem for the Fallen" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Seven Strike as One" ] ] ] ] ] ]
                      div [ Class "column is-narrow" ]
                        [ article [ Class "message is-info" ]
                            [ div [ Class "message-header" ]
                                [ p [ ]
                                    [ str "Info" ]
                                  Standard.button [ Class "delete"
                                                    HTMLAttr.Custom ("aria-label", "delete") ]
                                    [ ] ]
                              div [ Class "message-body" ]
                                [ div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Bronchy" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Aorta" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Alveolae" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "TALISMAN" ] ] ] ] ] ]
                      div [ Class "column is-narrow" ]
                        [ article [ Class "message is-success" ]
                            [ div [ Class "message-header" ]
                                [ p [ ]
                                    [ str "Success" ]
                                  Standard.button [ Class "delete"
                                                    HTMLAttr.Custom ("aria-label", "delete") ]
                                    [ ] ]
                              div [ Class "message-body" ]
                                [ div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "signature" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "weasel" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "solana" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "hydro" ] ] ] ] ] ]
                      div [ Class "column is-narrow" ]
                        [ article [ Class "message is-warning" ]
                            [ div [ Class "message-header" ]
                                [ p [ ]
                                    [ str "Warning" ]
                                  Standard.button [ Class "delete"
                                                    HTMLAttr.Custom ("aria-label", "delete") ]
                                    [ ] ]
                              div [ Class "message-body" ]
                                [ div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Ganimede" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Europa" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Tycho" ] ] ]
                                  div [ Class "board-item" ]
                                    [ div [ Class "board-item-content" ]
                                        [ span [ ]
                                            [ str "Io" ] ] ] ] ] ] ] ] ]
          div [ Class "columns is-mobile is-centered" ]
            [ div [ Class "column is-half is-narrow" ]
                [ ] ]
          footer [ ]
            [ div [ Class "box cta" ]
                [ div [ Class "columns is-mobile is-centered" ]
                    [ div [ Class "field is-grouped is-grouped-multiline" ]
                        [ div [ Class "control" ]
                            [ div [ Class "tags has-addons" ]
                                [ a [ Class "tag is-link"
                                      Href "https://github.com/dansup/bulma-templates" ]
                                    [ str "Bulma Templates" ]
                                  span [ Class "tag is-light" ]
                                    [ str "Daniel Supernault" ] ] ]
                          div [ Class "control" ]
                            [ div [ Class "tags has-addons" ]
                                [ a [ Class "tag is-link" ]
                                    [ str "The source code is licensed" ]
                                  span [ Class "tag is-light" ]
                                    [ str "MIT"
                                      i [ Class "fa fa-github" ]
                                        [ ] ] ] ] ] ] ] ]
        ]


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
