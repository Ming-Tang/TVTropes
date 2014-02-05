module TVTropes.Parsing

open System
open System.Text
open System.Text.RegularExpressions

type Contents = string
type Substring = string * int * int

let parsePageName (str : string) : Backup.PageName =
  match str.Split([| '/'; '.' |], 2) with
  | [| title |] -> "Main", title 
  | [| ns; title |] -> ns, title
  | _ -> "", ""

let toWikiTitle (text : string) =

  let inline removeAnchor (s : string) =
    match s.IndexOf('#') with
    | -1 -> s
    | i -> s.Substring(0, i)

  let inline titleCase (s : string) =
    match s with
    | "" -> ""
    | s1 ->
      Char.ToUpper(s1.[0]).ToString() + s1.Substring(1)

  let filterChars =
    String.collect (function
                    | '@' -> "Tropers"
                    | '/' -> "."
                    | '|' | '{' | '}' -> ""
                    | c -> c.ToString())

  (text |> removeAnchor
        |> filterChars).Split(' ')
  |> Array.map titleCase
  |> Array.reduce (+)

let getInternalLinks (contents : Contents) =
  let len = contents.Length

  // find the first char that satistifies a predicate since i
  let rec find i p =
    if i >= len then -1
    elif p contents.[i] then i
    else find (i + 1) p

  // find two consecutive chars since i
  let rec findc2 i x y =
    if i >= len - 1 then -1
    elif contents.[i] = x && contents.[i + 1] = y then i
    else findc2 (i + 1) x y
  
  // get the ith char of contents
  let inline get i =
    if i >= len || i < 0 then '\n' else contents.[i]
 
  // substring of contents by index
  let inline ss i j =
    contents.Substring(i, max 0 (j - i + 1)), i, j

  // find the wiki word starting from i
  // ASSUME: Char.IsUpper contents.[i]
  let inline findWikiWord i =
    // j: the current index the function is searching
    // found: true only if CamelCase has been found
    let rec findWikiWordAcc j found =
      if j >= len then found, len // reached end of string
      elif not <| Char.IsLetterOrDigit(contents.[j]) then // reached end of word
        if found
        then true, j  // actually a WikiWord
        else false, j // not a WikiWord
      elif j < len - 1 && Char.IsLower contents.[j] // found CamelCase pattern
                       && Char.IsUpper contents.[j + 1] then
        findWikiWordAcc (j + 1) true
      else // keep looking
        findWikiWordAcc (j + 1) found
    findWikiWordAcc i false

  // attempt to add a string to the results
  let inline add (s : Substring) res =
    match s with
    | "", _, _ -> res
    | sub, i, j -> (toWikiTitle sub, i, j) :: res

  let rec getInternalLinksAcc (i : int) (res : Substring list) =
    if i >= len then res
    elif i < 0 then failwith "i is negative"
    else
      let i' = i + 1
      let i'' = i + 2
      let next = get i'
      match get i with
      | '[' -> match next with
               | '[' -> if i'' < len && contents.[i''] = '{' then
                          // [[{{Page Name}} link text]]
                          getInternalLinksAcc i'' res
                        else
                          // [[WikiWord B]]
                          // [[AC:text]]
                          // [[quoteright:123:text]]
                          // [[http://example.com text]]
                          let k = find i'' (fun c -> c = ':' || c = ' ')
                          let l = findc2 i'' ']' ']'
                          if l < k && l <> -1 then
                            (match ss i'' (l - 1) with
                             | "", _, _ -> res
                             | w, _, _ when Char.IsLower(w.[0]) -> res
                             | word -> add word res)
                            |> getInternalLinksAcc (l + 2) 
                          else
                            let k = min k l
                            if k = -1 then getInternalLinksAcc i' res
                            else
                              match contents.[k] with
                              | ':' -> let partial, s, e = ss i'' (k - 1)
                                       // filter out links
                                       if partial.StartsWith("http") || partial.StartsWith("mailto") then
                                         match find k ((=) ' ') with
                                         | -1 -> getInternalLinksAcc (k + 1) res
                                         | ns -> getInternalLinksAcc ns res
                                       else
                                         getInternalLinksAcc (k + 1) res
                              | _ -> getInternalLinksAcc (l + 2) <| add (ss i'' (k - 1)) res
               | '=' -> match findc2 i' '=' ']' with
                        | -1 -> getInternalLinksAcc i'' res
                        | k -> getInternalLinksAcc (k + 2) res
               | _ -> getInternalLinksAcc i' res
      | '{' -> match next with
               | '{' -> match findc2 i' '}' '}' with
                        | -1 -> getInternalLinksAcc i' res
                        | k -> getInternalLinksAcc (k + 2) <| add (ss i'' (k - 1)) res
               | _ -> getInternalLinksAcc i' res 
      | '@' when (get i' = '/' || get i' = '.') && get i'' = '{' && get (i + 3) = '{' ->
        // found troper handle
        match findc2 (i + 4) '}' '}' with
        | -1 -> getInternalLinksAcc (i + 4) res
        | k -> getInternalLinksAcc (k + 3) <| add (ss i (k + 1)) res
      | c when Char.IsUpper(c) && not <| Char.IsLetterOrDigit(get (i - 1)) ->
        // found wikiword
        let prev = get (i - 1)
        let found, k = findWikiWord i
        let c1 = get k
        if (c1 = '/' || c1 = '.') then
          if get (k + 1) = '{' && get (k + 2) = '{' then
            // Namespace.{{Page}} Namespace/Page
            let l = findc2 i' '}' '}'
            if l = -1 then getInternalLinksAcc (k + 2) res
            else getInternalLinksAcc (l + 2) <| add (ss i (l + 1)) res
          else
            let found2, m = findWikiWord (k + 1)
            if found2 then
              getInternalLinksAcc m <| add (ss i (m - 1)) res
            else
              getInternalLinksAcc m res 
        elif found then getInternalLinksAcc k <| add (ss i (k - 1)) res
        else getInternalLinksAcc k res
      | _ -> getInternalLinksAcc i' res

  getInternalLinksAcc 0 []
  |> List.filter (fun (s, _, _) -> s.Length > 0)
  |> List.rev

