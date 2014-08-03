module TVTropes.Main

open System
open Mono.Data.Sqlite
open System.Diagnostics
open TVTropes
open TVTropes.Timing
open TVTropes.Util
open TVTropes.Data

type Index = (Backup.Namespace * Backup.PageTitle * Backup.Path)[]
type NSMap = Map<Backup.Namespace, int>

let createQueries = @"

CREATE TABLE IF NOT EXISTS Page (
  NamespaceID INT NOT NULL,
  Title TEXT NOT NULL,
  Contents TEXT NOT NULL,
  Path STRING NOT NULL,
  ContentsAdded BOOL DEFAULT 0 NOT NULL,
  LinksIndexed BOOL DEFAULT 0 NOT NULL
);
                      
CREATE TABLE IF NOT EXISTS Namespace (
  Name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Link (
  FromPageID INT NOT NULL,
  ToPageID INT NOT NULL,
  StartIndex INT NOT NULL,
  EndIndex INT NOT NULL
);

CREATE INDEX IF NOT EXISTS index_Page_NamespaceID ON Page (NamespaceID ASC, Title ASC);
CREATE INDEX IF NOT EXISTS index_Page_Title ON Page (Title ASC, NamespaceID ASC);
CREATE INDEX IF NOT EXISTS index_Namespace_Name ON Namespace (Name ASC);

"

let parsePN = Parsing.parsePageName

let initializeDatabase dbPath =
  (* INIT *)
  
  use conn = logged (sprintf "Connect to database: %s" dbPath) (fun () ->
    let c = new SqliteConnection(sprintf "Data Source=%s" dbPath)
    c.Open(); c
  )

  Data.onConnect()
  
  (* PROCEDURES *)
  
  let createTables() = DB.execute createQueries |> ignore

  let indexFiles() =
    Backup.enumerateFiles()
    |> Seq.toArray
    |> (fun arr ->
      beginProgress arr.Length
      arr
    )
    |> Seq.map (fun path ->
        let ns, title = Backup.pageName path
        progress 1
        ns, title, path)
    |> Seq.toArray

  let indexNamespaces index () =    
    DB.transaction (fun tx ->
      use cmd = DB.prepare "INSERT INTO Namespace (Name) VALUES (?)" 1
      index
      |> Array.fold (fun set (ns, title, path) -> Set.add ns set) Set.empty
      |> Array.ofSeq
      |> (fun s -> beginProgress s.Length; s)
      |> Array.map (fun ns ->
        DB.exec cmd [ ns ] |> ignore
        progress 1
        ns)
    )
    |> Array.mapi (fun i x -> x, i + 1)
    |> Map.ofArray

  let loadNamespaces() =
    use cmd = DB.prepare "SELECT rowid, Name FROM Namespace ORDER BY rowid" 0
    DB.execReadSeq cmd [] (fun reader -> reader.GetString(1))
    |> Seq.toArray
    |> Array.mapi (fun i x -> x, i + 1)
    |> Map.ofArray

  let insertPageNames (index : Index) (nsMap : NSMap) () =
    DB.transaction (fun tx ->
      beginProgress index.Length
      use insPage = DB.prepare "INSERT INTO Page (NamespaceID, Title, Contents, Path) VALUES (?, ?, ?, ?)" 4

      for ns, title, path in index do
        DB.exec insPage [ nsMap.[ns]; title; ""; path ] |> ignore
        progress 1
    )
  
  let countInsertPageContents() = DB.count "Page WHERE ContentsAdded=0"
  let countIndexLinks() = DB.count "Page WHERE LinksIndexed=0"
  //let countIndexLinkPairs() = DB.count "Page WHERE LinkPairsIndexed=0"

  let insertPageContents() =
    use find = DB.prepare "SELECT rowid, Path FROM Page WHERE ContentsAdded=0" 0
    let todo =
      DB.execReadSeq find [] (fun reader -> reader.GetInt32(0), reader.GetString(1))
      |> Seq.toArray

    use upd = DB.prepare "UPDATE Page SET Contents=?, ContentsAdded=1 WHERE rowid=?" 2

    let insertPageContentsCount = countInsertPageContents()
    beginProgress insertPageContentsCount
    for todo' in chunk 200 todo do
      DB.transaction (fun tx ->
        for pageID, path in todo' do
          let contents =
            let contents = Backup.getContentsFromPath path
            if String.IsNullOrEmpty(contents) then "" else contents

          DB.exec upd [ contents; pageID ] |> ignore
          progress 1
      )
   
  let indexLinks() =
    use find = DB.prepare "SELECT rowid FROM Page WHERE LinksIndexed=0" 0
    let todo = DB.execReadSeq find [] (fun reader -> reader.GetInt32(0))
               |> Seq.toArray

    use ins = DB.prepare "INSERT INTO Link (FromPageID, ToPageID, StartIndex, EndIndex) VALUES (?, ?, ?, ?)" 4
    use upd = DB.prepare "UPDATE Page SET LinksIndexed=1 WHERE rowid=?" 1
    use sel = DB.prepare "SELECT Contents FROM Page WHERE rowid=? LIMIT 1" 1

    let inline (|>>) o f =
      o
      |> Option.bind (fun x -> f x |> ignore; None)
      |> ignore

    let indexLinksCount = countIndexLinks()
    beginProgress indexLinksCount
    for todo' in chunk 200 todo do
      DB.transaction (fun tx ->
        for pageID in todo' do
          DB.execReadFirst sel [ pageID ] (fun reader -> reader.GetString(0))
          |>> (fun contents ->
            Parsing.getInternalLinks contents
            |> List.iter (fun (link, startIndex, endIndex) ->
              getID (parsePN link)
              |>> (fun targetID -> DB.exec ins [ pageID; targetID; startIndex; endIndex ])
            )
          )
          DB.exec upd [ pageID ] |> ignore
          progress 1
      )

  let inline needToDo name decide task none =
    if decide then logged name task
    else printfn "No need to %s" name; none

  let inline needToBranch decide name1 task1 name2 task2 =
    if decide then logged name1 task1
    else logged name2 task2 

  (* BODY *)
  
  logged "create tables" createTables

  beginLog "determine tasks to be completed"
  let needToIndexNamespaces = DB.isEmpty "Namespace"
  let needToInsertPageNames = DB.isEmpty "Page"
  let needToInsertPageContents = needToInsertPageNames || 0 <> countInsertPageContents()
  let needToIndexLinks = needToInsertPageNames || 0 <> countIndexLinks()
  //let needToIndexLinkPairs = needToInsertPageNames || 0 <> countIndexLinkPairs()
  let needToIndexFiles = needToIndexNamespaces || needToInsertPageNames
  endLog()

  let index : Index = needToDo "index files" needToIndexFiles indexFiles null
  let nsMap : NSMap = needToBranch needToIndexNamespaces
                                   "index namespaces" (indexNamespaces index)
                                   "load namespaces" loadNamespaces
  needToDo "insert page names" needToInsertPageNames (insertPageNames index nsMap) ()
  needToDo "insert page contents" needToInsertPageContents insertPageContents ()
  needToDo "index links" needToIndexLinks indexLinks ()
  
  (* CLEANUP *)
  Data.onDisconnect()

let rec interactive() =
  printf "> "
  match Console.ReadLine().Split(' ') |> List.ofArray with
  | "l" :: str :: _ ->
    let sel = DB.prepare "   
      SELECT Namespace.Name, Title, Contents FROM Page
      INNER JOIN Namespace
      ON Namespace.rowid = Page.NamespaceID
      WHERE Namespace.Name = ? AND Page.Title = ?
      LIMIT 1" 2

    let ns, title = parsePN str

    match DB.execReadFirst sel [ ns; title ] (fun reader -> reader.GetString(2)) with
    | None -> printfn "Page not found: %s.%s" ns title
    | Some(contents) -> printfn "%s" <| contents.Replace("<br/>", "<br/>\n")
                        printfn "-----"
                        for link, _, _ in Parsing.getInternalLinks contents do
                          let id = parsePN link |> getID
                          printfn "{{%s}} : %A" link id
                        printfn ""
      
    interactive()

  | "x" :: _ -> ()
  | _ ->
    printfn "???"  
    interactive()
    


[<EntryPoint>]
let main (argv : string[]) =
  let err = System.Console.Error

  //let path = @"C:\Users\Ming\Documents\Visual Studio 2013\Projects\TVTropes\TVTropesTest\Testing Data"
  let path = @"/Users/mingtang/Downloads/TVTropesArticleBackupjune2012/TVTropesBackup"
  let db = @"database.sqlite"
  let dbFull = sprintf "%s/%s" path db
  
  Backup.setBackupPath path
  DB.connect dbFull
  initializeDatabase dbFull

  //interactive()

  printf "Press any key to exit."
  Console.ReadKey() |> ignore
  0
