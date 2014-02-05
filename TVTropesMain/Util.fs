module TVTropes.Util

/// Breaks a sequence into chunks of at most k elements
let rec chunk k (s : seq<'a>) =
  let s = Seq.cache s
  [| // FIXME bug is here: skips first and lasts
    use ie = s.GetEnumerator()
    while ie.MoveNext() do
      yield [|
          let i = ref 0
          while !i < k && ie.MoveNext() do
            i := !i + 1
            yield ie.Current
        |]
  |]

/// Creates a map using a by grouping keys using the projection
/// then select an item from each group with the selector
let makeMap projection selector =
  Seq.groupBy projection
  >> Seq.map (fun (key, group) -> key, selector key group)
  >> Map.ofSeq

/// Flips a map's key and value, assuming the values are unique.
let flipMap (m : Map<'a, 'b>) = m |> makeMap (fun kv -> kv.Value) (fun i g -> (Seq.head g).Key)
