module TVTropes.Backup

open System
open System.IO
open System.Text
open System.Diagnostics
open System.Collections.Generic

type Namespace = string
type PageTitle = string
type Path = string
type PageName = Namespace * PageTitle

let mutable private backupPath = @"C:\Users\AAA\Downloads\TVTropesBackup"

let setBackupPath(bu : string) =
  backupPath <- bu

let getBackupPath() = backupPath

let fullPath (path : Path) : string =
  Path.Combine(backupPath, path)

let relativePath (fullPath : string) : string =
  if fullPath.StartsWith(backupPath) then
    fullPath.Substring(backupPath.Length + 1)
  else
    failwith (sprintf "Not a path relative to backup path: %s" fullPath)

let fileExists (path : Path) : bool =
  File.Exists(fullPath path)

let dirExists (path : Path) : bool =
  Directory.Exists(fullPath path)

let filenameOf ((ns, title) : PageName) : string =
  sprintf "%s.%s@action=source" ns title

let getContentsFromPath (path : Path) : string =
  let fullPath = fullPath path
  use sr = new StreamReader(fullPath)
  for i = 1 to 7 do sr.ReadLine() |> ignore
  sr.ReadLine()

let pageName (path : Path) : PageName =
  match Path.GetFileName(path).Replace("@action=source", "").Split('.') with
  | [| a; b |] -> a, b
  | _ -> failwith "Not a valid path."

let enumerateFiles() : seq<Path> =
  let rec enumerateDirectoriesByPath path =
    Directory.EnumerateDirectories(path)
    |> Seq.map enumerateFilesByPath
    |> Seq.concat
  and enumerateFilesByPath path =
    Seq.append
    <| enumerateDirectoriesByPath path
    <| Directory.EnumerateFiles path

  enumerateDirectoriesByPath backupPath
  |> Seq.map (fun p -> p.Replace(backupPath + "\\", ""))

  