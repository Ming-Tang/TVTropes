module TVTropes.DB

open System
open System.Data.SQLite

type Connection = SQLiteConnection
type CommandText = string
type Command = SQLiteCommand
type Reader = SQLiteDataReader
type Transaction = SQLiteTransaction
type TableName = string
type ColumnName = string
type WhereClause = string
type Parameters = seq<obj>

let mutable private conn : Connection = null

let connect path =
  conn <- (new SQLiteConnection(sprintf "Data Source=%s" path)).OpenAndReturn()

let disconnect() =
  if conn <> null then conn.Close()
  conn <- null

let getConnection() = conn

let execute (cmdText : CommandText) =
  let cmd = conn.CreateCommand()
  cmd.CommandText <- cmdText
  cmd.ExecuteNonQuery()

let transaction f =
  use tx = conn.BeginTransaction()
  let v = f tx
  tx.Commit()
  v

let count (table : TableName) =
  use cmd = conn.CreateCommand()
  cmd.CommandText <- sprintf "SELECT COUNT(rowid) FROM %s" table
  Convert.ToInt32(cmd.ExecuteScalar())

let clear (table : TableName) =
  use cmd = conn.CreateCommand()
  cmd.CommandText <- sprintf "DELETE FROM %s" table
  cmd.ExecuteNonQuery()

let prepare text n =
  let cmd = conn.CreateCommand()
  cmd.CommandText <- text
  for i in 1 .. n do
    let p = new SQLiteParameter()
    cmd.Parameters.Add(p) |> ignore
  cmd

let private setParams (cmd : Command) (ps : Parameters) =
  Seq.iteri (fun i v ->
    cmd.Parameters.[i].Value <- (v : obj)
  ) ps

let exec (cmd : Command) (ps : Parameters) =
  setParams cmd ps
  cmd.ExecuteNonQuery()

let execRead (cmd : Command) (ps : Parameters) (f : Reader -> unit) =
  setParams cmd ps
  use reader = cmd.ExecuteReader()
  while reader.Read() do
    f reader

let execReadSeq (cmd : Command) (ps : Parameters) (f : Reader -> 'a) =
  setParams cmd ps
  seq {
    use reader = cmd.ExecuteReader()
    while reader.Read() do
      yield f reader
  }

let execReadFirst cmd ps f =
  setParams cmd ps
  use reader = cmd.ExecuteReader()
  if reader.Read() then Some(f reader)
  else None

let isEmpty t =
  use sel = prepare (sprintf "SELECT * FROM %s LIMIT 1" t) 0
  execReadFirst sel [ ] ignore
  |> Option.isNone
