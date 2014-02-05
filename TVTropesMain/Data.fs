module TVTropes.Data
open TVTropes

let mutable private selState = null
let mutable private updState = null
let mutable private insState = null
let mutable private delState = null
let mutable private getIDCmd = null

let onConnect() =
  selState <- DB.prepare "SELECT Value FROM State WHERE Key=? LIMIT 1" 1
  updState <- DB.prepare "UPDATE State SET Value=? WHERE Key=?" 2
  insState <- DB.prepare "INSERT INTO State (Key, Value) VALUES (?, ?)" 2
  delState <- DB.prepare "DELETE FROM State WHERE Key=?" 2
  getIDCmd <- DB.prepare "   
    SELECT Page.rowid FROM Page
    INNER JOIN Namespace
    ON Namespace.rowid = Page.NamespaceID
    WHERE Namespace.Name = ? AND Page.Title = ?
    LIMIT 1" 2

let onDisconnect() =
  selState.Dispose()
  updState.Dispose()
  insState.Dispose()
  delState.Dispose()
  getIDCmd.Dispose()

/// Gets the rowid of a page
let getID ((ns, title) : Backup.PageName) =
  DB.execReadFirst getIDCmd [ ns; title ] (fun reader -> reader.GetInt32(0))
