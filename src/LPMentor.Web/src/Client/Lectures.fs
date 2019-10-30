module Lecture
open Fulma
open Fable.React
open Fable.React.Props
open Elmish
open Elmish.React

open Shared

type Model = {
    Lessons: Lesson List

}

type Msg =
| Full of Lesson List
| Append of Lesson List


let init () : Model * Cmd<Msg>=
    let initialModel = { Lessons = List.empty }
    initialModel, Cmd.Empty

let update (msg: Msg) (currModel: Model) =
    match msg with
    | Full newLs -> { Lessons = newLs }, Cmd.Empty
    | Append subLs ->
        { Lessons = currModel.Lessons @ subLs }, Cmd.Empty

let LectureCard (c: IColor) topic (sections: string seq) =
    let boardItem txt =
        div [ Class "board-item" ]
            [ div [ Class "board-item-content" ]
                [ span [ ]
                    [ str txt ] ] ]
    Column.column [ Column.Option.Width (Screen.All, Column.ISize.IsNarrow) ]
        [ Message.message [ Message.Option.Color c ]
            [ Message.header []
                [ p [] [ str topic ]
                  Notification.delete [ Props [HTMLAttr.Custom ("aria-label", "delete") ]] [] ]
              Message.body []
                [ for sectionTxt in sections -> boardItem sectionTxt ] ] ]

let chooseColor (lesson: Lesson) =
    lesson.Topic.GetHashCode() % 9 |> function
    | 0 -> IColor.IsBlack
    | 1 -> IColor.IsDanger
    | 3 -> IColor.IsLight
    | 4 -> IColor.IsInfo
    | 5 -> IColor.IsPrimary    
    | 6 -> IColor.IsLink
    | 7 -> IColor.IsWarning
    | 8 -> IColor.IsSuccess
    | _ -> IColor.IsInfo

let LectureCard' (c: IColor) topic (sections: Section seq) =
    let boardItem (section: Section) =
        div [ Class "board-item" ]
            [ div [ Class "board-item-content" ]
                [ a [ Href (sprintf "/audios/%s" section.Url) ]
                    [ str section.Section ] ] ]
    Column.column [ Column.Option.Width (Screen.All, Column.ISize.IsNarrow) ]
        [ Message.message [ Message.Option.Color c ]
            [ Message.header []
                [ p [] [ str topic ]
                  Notification.delete [ Props [HTMLAttr.Custom ("aria-label", "delete") ]] [] ]
              Message.body []
                [ for section in sections -> boardItem section ] ] ]

let displayLesson (lesson: Lesson) =
    LectureCard' (chooseColor lesson) lesson.Topic lesson.Sections

let view (model: Model) (dispatch: Msg -> unit) =
    section [ Class "container" ]
        [ Level.item []
            [ Columns.columns [ Columns.Option.IsMultiline; Columns.Option.IsCentered ; Columns.Option.CustomClass "cards-container";
                                Columns.Option.Props [Id "sectioncontainer" ]]
                [ for lesson in model.Lessons -> displayLesson lesson ] ] ]


