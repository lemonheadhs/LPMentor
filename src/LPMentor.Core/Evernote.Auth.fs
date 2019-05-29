module LPMentor.Core.Evernote.Auth

open System
open System.Text.RegularExpressions
open System.Globalization

open EvernoteSDK
open EvernoteSDK.Advanced
open Evernote.EDAM.UserStore
open Evernote.EDAM.NoteStore
open Thrift.Transport
open Thrift.Protocol
open EvernoteOAuthNet

let ENSessionBootstrapServerBaseURLStringCN = "app.yinxiang.com"
let ENSessionBootstrapServerBaseURLStringUS = "www.evernote.com"
let SessionHost = ENSessionBootstrapServerBaseURLStringCN

type LENCredentials = {
    Host: string
    EdamUserId: string
    NoteStoreUrl: string
    WebApiUrlPrefix: string
    AuthenticationToken: string
    ExpirationDate: DateTime
}
with
    member __.AreValid() =
        __.ExpirationDate = (DateTime())
        || DateTime.Now <= __.ExpirationDate


let userStoreUrl sessionHost =
    let matches = Regex.Matches(sessionHost, ".*:[0-9]+", RegexOptions.IgnoreCase)
    let scheme = 
        if matches.Count > 0 then "http" else "https"
    sprintf "%s://%s/edam/user" scheme sessionHost

let getBootstrapInfo (userStoreUrl: string) =
    let client = 
        userStoreUrl
        |> Uri
        |> THttpClient
        |> TBinaryProtocol
        |> UserStore.Client
    let locale = CultureInfo.CurrentCulture.ToString()
    client.getBootstrapInfo(locale)

let authenticateToEvernote () = 
    let info = SessionHost |> userStoreUrl |> getBootstrapInfo
    let profile = info.Profiles.[0]
    let host = profile.Settings.ServiceHost
    let service =
        match host with
        | s when s = ENSessionBootstrapServerBaseURLStringCN ->
            EvernoteOAuth.HostService.Yinxiang
        | s when s = ENSessionBootstrapServerBaseURLStringUS ->
            EvernoteOAuth.HostService.Production
        | _ ->
            EvernoteOAuth.HostService.Sandbox
    
    let consumerKey = Environment.GetEnvironmentVariable "consumerKey"
    let consumerSecret = Environment.GetEnvironmentVariable "consumerSecret"
    let oath = EvernoteOAuth(service, consumerKey, consumerSecret, false)
    let errResponse = oath.Authorize()
    errResponse.Length = 0 |> function
    | false -> None
    | true ->
        let credentials = {
            Host = SessionHost
            EdamUserId = oath.UserId
            NoteStoreUrl = oath.NoteStoreUrl
            WebApiUrlPrefix = oath.WebApiUrlPrefix
            AuthenticationToken = oath.Token
            ExpirationDate = oath.Expires.ToDateTime()
        }
        Some credentials

let noteStoreClient (noteStoreUrl:string) = 
    noteStoreUrl
    |> Uri
    |> THttpClient
    |> TBinaryProtocol
    |> fun p -> p, p
    |> NoteStore.Client

let getNoteContent (noteStore: NoteStore.Client) authToken guid = 
    noteStore.getNoteContent(authToken, guid)

let getNoteTagNames (noteStore: NoteStore.Client) authToken guid = 
    noteStore.getNoteTagNames(authToken, guid)