namespace TVTropes
  
  /// This module handles the access to backup files.
  /// This module makes a lots of assumptions about how the backups are organized.
  module Backup =

    /// A namespace. For example, "Analysis"
    type Namespace = string

    /// A page title. For example, "MassEffect"
    type PageTitle = string
    
    /// A path relative to the <see cref="backupPath" />
    type Path = string
    
    /// A page name with its namespace and page title.
    /// For example, ("Administrivia", "TheGoalsOfTVTropes")
    type PageName = Namespace * PageTitle
    
    /// Set the backup path and initialize the page index
    val setBackupPath : string -> unit

    /// Get the path to the TV Tropes backup
    val getBackupPath : unit -> string

    /// Get the full path from a relative path
    val fullPath : Path -> string

    /// Get the relative path of a full path
    val relativePath : Path -> string
    
    /// Return true only if the specified backup file exists
    val fileExists : Path -> bool
    
    /// Return true only if the specified dir exists
    val dirExists : Path -> bool

    /// Return the page name (namespace and title) of a given path
    val pageName : Path -> PageName

    /// Return the corresponding filename of a page
    val filenameOf : PageName -> string

    /// Get the contents of a page from backup using a relative path.
    /// The HTML tags before and after are stripped.
    val getContentsFromPath : Path -> string

    /// Enumerate the paths of all backup files alphabetically
    val enumerateFiles : unit -> seq<Path>
