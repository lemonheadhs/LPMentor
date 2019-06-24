namespace Shared

type Counter = { Value : int }

module Route =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

/// A type that specifies the communication protocol between client and server
/// to learn more, read the docs at https://zaid-ajaj.github.io/Fable.Remoting/src/basics.html
type ICounterApi =
    { initialCounter : unit -> Async<Counter> }

type Section = { Name: string; Url: string }
type Lesson = { Topic: string; Sections: Section array }

type ILessonSearchApi = 
    { init : unit -> Async<Lesson array> 
      searchTopic : string -> Async<Lesson array> }