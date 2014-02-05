module TVTropes.Test.Parsing

open TVTropes
open NUnit.Framework
open FsUnit

let testToWikiTitle =
  List.iter (fun (a, b) ->
    Parsing.toWikiTitle a |> should equal b)

let shouldSubstringsEqual (l1 : string list) (l2 : Parsing.Substring list) =
  let f (a, _, _) = a
  let l2 = List.map f l2
  sprintf "%A" l1 |> should equal <| sprintf "%A" l2 
  
// see also: http://www.pmwiki.org/wiki/PmWiki/Links
// http://tvtropes.org/pmwiki/pmwiki.php/Administrivia/TextFormattingRules?from=Main.TextFormattingRules

[<Test>]
let ``toWikiTitle converts a string into its wiki title form``() =
  [ "WikiSandbox", "WikiSandbox"
    "wiki sandbox", "WikiSandbox"
    "Wiki Sandbox", "WikiSandbox"
    "wiki sandbox", "WikiSandbox"
    "installation", "Installation" ]
  |> testToWikiTitle

[<Test>]
let ``toWikiTitle should remove bars``() =
  [ "Renamed Trope|s", "RenamedTropes" ]
  |> testToWikiTitle

[<Test>]
let ``toWikiTitle should preserve namespaces``() =
  [ "Main.WikiSandbox", "Main.WikiSandbox"
    "Main/WikiSandbox", "Main.WikiSandbox" ]
  |> testToWikiTitle

[<Test>]
let ``toWikiTitle should remove #anchors``() =
  [ "PageName#name", "PageName" ]
  |> testToWikiTitle

[<Test>]
let ``getInternalLinks should detect CamelCase WikiWord links``() =
  "getInternalLinks finds WikiWord links. CamelCase is one, and Wiki and W3C are not. "
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "WikiWord"; "CamelCase" ]

[<Test>]
let ``getInternalLinks should properly handle words with double uppercase letters and words containing numbers``() =
  "A AB AbC AbCd ABc ABcD ABcDEF A2BcDe"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "AbC"; "AbCd"; "ABcD"; "ABcDEF"; "A2BcDe" ]

[<Test>]
let ``getInternalLinks should find links in brackets``() =
  "[[WikiWord text]] [[Test test link]]"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "WikiWord"; "Test" ]

[<Test>]
let ``getInternalLinks should process links in brackets in braces``() =
  "[[{{article}} phrase text]] [[{{Renamed Tropes}} test link]]
   [[{{Chuck}} The Bartowskis]] {{Title with Spaces}} {{Test}} {{MacGuffin}}"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "Article"; "RenamedTropes"; "Chuck"
                             "TitleWithSpaces"; "Test"; "MacGuffin" ]

[<Test>]
let ``getInternalLinks should process bars braces``() =
  "{{Renamed Trope|s}}"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "RenamedTropes" ]


[<Test>]
let ``getInternalLinks should handle wikiword namespace links``() =
  "Namespace/PageName Test.TropeName"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "Namespace.PageName"; "Test.TropeName" ]


[<Test>]
let ``getInternalLinks should handle hybrid links``() =
  "Namespace/{{Page Name}} Test.{{Trope}}"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "Namespace.PageName"; "Test.Trope" ]

[<Test>]
let ``getInternalLinks should handle troper handles``() =
  "Tropers/{{TroperName}} @/{{TroperName}}"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "Tropers.TroperName"; "Tropers.TroperName" ]

[<Test>]
let ``getInternalLinks should remove anchors``() =
  "[[PageName0#name]] [[PageName1#name link text]] {{PageName2#name}}"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "PageName0"; "PageName1"; "PageName2" ]

[<Test>]
let ``getInternalLinks should ignore self-anchors``() =
  "[[#intermaps]] [[#intermaps]]"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual []

[<Test>]
let ``getInternalLinks should ignore external links``() =
  "[[https://google.com Google]] [[mailto:myaddress@example.com]]
   [[http://example.com/]] [[http://test.com/a/b.php?c=d%20&e=f_f%20#h complex URL]]
   [[http://www.youtube.com/watch?v=uSF2i0rU_Q8 Indestructible]]
   * [[http://www.youtube.com/watch?v=ILLT25wpHwU I Will Survive]] by Stephanie Bentley<br/>
   * [[http://www.youtube.com/watch?v=0NThznYcJ1Q Don't Give Up]] by Eagle Eye Cherry"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual []

[<Test>]
let ``getInternalLinks should ignore special markup (lowercase in [[]]), but not formatted links``() =
  "[[noreallife]] [[test]] [[hardline]] [[folder]] [[#AnchorPoint]]
   [[AATAFOVS:Sandworm]] [[spoiler:WikiWord {{Word}}]]
   [[quoteright:296:[[TheIncredibles http://static.tvtropes.org/pmwiki/pub/images/incredibles_badassfamily.jpg]]]]"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "WikiWord"; "Word"; "TheIncredibles" ]

[<Test>]
let ``getInternalLinks should ignore escaped [=WikiWords=]``() =
  "[[=WikiWord=]]"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual []
  
[<Test>]
let ``getInternalLinks should detect links in punctuations``() =
  "(WikiWord)
   (&quot;[[{{A}} quoted]]&quot;)"
  |> Parsing.getInternalLinks
  |> shouldSubstringsEqual [ "WikiWord"; "A" ]
