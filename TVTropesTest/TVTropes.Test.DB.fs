module TVTropes.Test.DB

open TVTropes
open System.IO
open System.Data.SQLite
open NUnit.Framework
open FsUnit

let dbPath = @"testdb.sqlite"
let createCommands = @"

CREATE TABLE IF NOT EXISTS Test (
  Name TEXT DEFAULT 'EMPTY' NOT NULL,
  Value INT DEFAULT 0
);

"

[<SetUp>]
let setup() =
  DB.connect dbPath
  DB.execute createCommands |> ignore

[<TearDown>]
let tearDown() =
  DB.clear "Test" |> ignore
  DB.disconnect()

[<Test>]
let ``getConnection produces the database handle``() =
  DB.getConnection
  |> should not' (be Null)

[<Test>]
let ``the test fixture DB table should be empty: (count "Test") should produce 0``() =
  DB.count "Test"
  |> should equal 0

[<Test>]
let ``inserting, deleting a row with execute should make row count same before and after``() =
  let countBefore = DB.count "Test"

  DB.execute "INSERT INTO Test (Name, Value) VALUES ('hello', 1)"
  |> should equal 1

  DB.execute "DELETE FROM Test WHERE Name='hello'"
  |> should equal 1

  let countAfter = DB.count "Test"
  countAfter
  |> should equal countBefore

[<Test>]
let ``after inserting a row, the row should appear when SELECTing``() =
  
  DB.execute "INSERT INTO Test (Name, Value) VALUES ('hello', 1)"
  |> should equal 1

  use cmd = DB.prepare "SELECT Name, Value FROM Test WHERE Name='hello'" 0
  
  DB.execReadFirst cmd [] (fun reader ->
    reader.GetString(0), reader.GetInt32(1))
  |> should equal <| Some("hello", 1)

  DB.execute "DELETE FROM Test WHERE Name='hello'"
  |> should equal 1

[<Test>]
let ``setParams sets the parameters of a prepared query from prepare``() =
  DB.count "Test"
  |> should equal 0

  let values = [ "hello", 1
                 "world", 2
                 "test", 1 ]
  use ins = DB.prepare "INSERT INTO Test (Name, Value) VALUES (?, ?)" 2
  for n, v in values do
    DB.exec ins [ n; v ] |> ignore

  DB.count "Test"
  |> should equal 3

  use sel = DB.prepare "SELECT Value FROM Test WHERE Name=?" 1
  for n, v in values do
    DB.execReadFirst sel [ n ] (fun reader ->
      reader.GetInt32(0))
    |> should equal <| Some(v)

  DB.clear "Test"
  |> should equal 3
  
  DB.count "Test"
  |> should equal 0

[<Test>]
let ``isEmpty produces true if the table is empty``() =
  DB.execute "INSERT INTO Test (Name, Value) VALUES (\"hello\", 1)" |> ignore
  DB.isEmpty "Test" |> should be False
  DB.clear "Test" |> ignore
  DB.isEmpty "Test" |> should be True
