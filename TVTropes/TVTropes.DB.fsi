namespace TVTropes

  /// The DB module handles database interaction
  module DB =

    open Mono.Data.Sqlite

    /// A database connection
    type Connection = SqliteConnection

    /// A database command string
    type CommandText = string

    /// A database command object
    type Command = SqliteCommand

    /// A database reader object
    type Reader = SqliteDataReader

    /// A database transaction object
    type Transaction = SqliteTransaction

    /// A table name
    type TableName = string

    /// A column name
    type ColumnName = string

    /// A WHERE clause for database commands
    type WhereClause = string

    /// A list of prepared statement parameters
    type Parameters = seq<obj>

    /// Connect to a specified database by URL and return the connection
    val connect : url : string -> unit

    /// Disconnect from a database
    val disconnect : unit -> unit

    /// Get the database connection handle    
    val getConnection : unit -> Connection

    /// Execute a non-query, and return rows affected
    val execute : CommandText -> int

    /// Perform a database transaction
    val transaction : (Transaction -> 'a) -> 'a

    /// Count the number of rows in a table
    val count : TableName -> int

    /// Empty a table and return the number of rows deleted
    val clear : TableName -> int

    /// Create a prepared command from text with a specified number of parameters
    val prepare : CommandText -> int -> Command

    /// Execute a command and return the number of rows affected
    val exec : Command -> Parameters -> int
    
    /// Execute a command and invoke the callback with reader
    val execRead : Command -> Parameters -> (Reader -> unit) -> unit

    /// Execute a command and return a sequence of the callback results
    val execReadSeq : Command -> Parameters -> (Reader -> 'a) -> seq<'a>

    /// Execute a command and return the first callback result, or None if no first
    val execReadFirst : Command -> Parameters -> (Reader -> 'a) -> 'a option

    /// Return true only if the table is empty
    val isEmpty : TableName -> bool
