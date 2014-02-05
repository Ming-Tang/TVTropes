namespace TVTropes

  /// The Parsing module deals with the parsing of wikitext
  module Parsing =

    /// A string representing the wikitext contents of a page
    type Contents = string

    /// A substring of Contents, with the substring itself (converted using toWikiTitle),
    /// the start index and the end index
    type Substring = string * int * int

    /// Convert a string into a wiki title
    val toWikiTitle : string -> string

    /// Get all internal links in a page
    val getInternalLinks : Contents -> Substring list

    /// Parse a page name into namespace and page name
    val parsePageName : string -> Backup.PageName
