// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

namespace Microsoft.FSharp.Collections

open System
open Microsoft.FSharp.Core
open Microsoft.FSharp.Core.Operators
open Microsoft.FSharp.Core.LanguagePrimitives
open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators
open Microsoft.FSharp.Collections
open Microsoft.FSharp.Core.CompilerServices
open System.Collections.Generic

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module List =

    let inline checkNonNull argName arg =
        if isNull arg then
            nullArg argName

    let inline indexNotFound () =
        raise (KeyNotFoundException(SR.GetString(SR.keyNotFoundAlt)))

    [<CompiledName("Length")>]
    let length (list: 'T list) =
        list.Length

    [<CompiledName("Last")>]
    let last (list: 'T list) =
        match Microsoft.FSharp.Primitives.Basics.List.tryLastV list with
        | ValueSome x -> x
        | ValueNone -> invalidArg "list" (SR.GetString(SR.inputListWasEmpty))

    [<CompiledName("TryLast")>]
    let rec tryLast (list: 'T list) =
        match Microsoft.FSharp.Primitives.Basics.List.tryLastV list with
        | ValueSome x -> Some x
        | ValueNone -> None

    [<CompiledName("Reverse")>]
    let rev list =
        Microsoft.FSharp.Primitives.Basics.List.rev list

    [<CompiledName("Concat")>]
    let concat lists =
        Microsoft.FSharp.Primitives.Basics.List.concat lists

    let inline countByImpl
        (comparer: IEqualityComparer<'SafeKey>)
        ([<InlineIfLambda>] projection: 'T -> 'SafeKey)
        ([<InlineIfLambda>] getKey: 'SafeKey -> 'Key)
        (list: 'T list)
        =
        let dict = Dictionary comparer

        let rec loop srcList =
            match srcList with
            | [] -> ()
            | h :: t ->
                let safeKey = projection h
                let mutable prev = 0

                if dict.TryGetValue(safeKey, &prev) then
                    dict.[safeKey] <- prev + 1
                else
                    dict.[safeKey] <- 1

                loop t

        loop list
        Microsoft.FSharp.Primitives.Basics.List.countBy dict getKey

    // We avoid wrapping a StructBox, because under 64 JIT we get some "hard" tailcalls which affect performance
    let countByValueType (projection: 'T -> 'Key) (list: 'T list) =
        countByImpl HashIdentity.Structural<'Key> projection id list

    // Wrap a StructBox around all keys in case the key type is itself a type using null as a representation
    let countByRefType (projection: 'T -> 'Key) (list: 'T list) =
        countByImpl
            RuntimeHelpers.StructBox<'Key>.Comparer
            (projection >> RuntimeHelpers.StructBox)
            (fun sb -> sb.Value)
            list

    [<CompiledName("CountBy")>]
    let countBy (projection: 'T -> 'Key) (list: 'T list) =
        match list with
        | [] -> []
        | _ ->
            if typeof<'Key>.IsValueType then
                countByValueType projection list
            else
                countByRefType projection list

    [<CompiledName("Map")>]
    let map mapping list =
        Microsoft.FSharp.Primitives.Basics.List.map mapping list

    [<CompiledName("MapIndexed")>]
    let mapi mapping list =
        Microsoft.FSharp.Primitives.Basics.List.mapi mapping list

    [<CompiledName("Indexed")>]
    let indexed list =
        Microsoft.FSharp.Primitives.Basics.List.indexed list

    [<CompiledName("MapFold")>]
    let mapFold<'T, 'State, 'Result> (mapping: 'State -> 'T -> 'Result * 'State) state list =
        Microsoft.FSharp.Primitives.Basics.List.mapFold mapping state list

    [<CompiledName("MapFoldBack")>]
    let mapFoldBack<'T, 'State, 'Result> (mapping: 'T -> 'State -> 'Result * 'State) list state =
        match list with
        | [] -> [], state
        | [ h ] -> let h', s' = mapping h state in [ h' ], s'
        | _ ->
            let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(mapping)

            let rec loop res list =
                match list, res with
                | [], _ -> res
                | h :: t, (list', acc') ->
                    let h', s' = f.Invoke(h, acc')
                    loop (h' :: list', s') t

            loop ([], state) (rev list)

    [<CompiledName("Iterate")>]
    let inline iter ([<InlineIfLambda>] action) (list: 'T list) =
        for x in list do
            action x

    [<CompiledName("Distinct")>]
    let distinct (list: 'T list) =
        Microsoft.FSharp.Primitives.Basics.List.distinctWithComparer HashIdentity.Structural<'T> list

    [<CompiledName("DistinctBy")>]
    let distinctBy projection (list: 'T list) =
        Microsoft.FSharp.Primitives.Basics.List.distinctByWithComparer HashIdentity.Structural<_> projection list

    [<CompiledName("OfArray")>]
    let ofArray (array: 'T array) =
        Microsoft.FSharp.Primitives.Basics.List.ofArray array

    [<CompiledName("ToArray")>]
    let toArray (list: 'T list) =
        Microsoft.FSharp.Primitives.Basics.List.toArray list

    [<CompiledName("Empty")>]
    let empty<'T> = ([]: 'T list)

    [<CompiledName("Head")>]
    let head list =
        match list with
        | x :: _ -> x
        | [] -> invalidArg "list" (SR.GetString(SR.inputListWasEmpty))

    [<CompiledName("TryHead")>]
    let tryHead list =
        match list with
        | x :: _ -> Some x
        | [] -> None

    [<CompiledName("Tail")>]
    let tail list =
        match list with
        | _ :: t -> t
        | [] -> invalidArg "list" (SR.GetString(SR.inputListWasEmpty))

    [<CompiledName("IsEmpty")>]
    let isEmpty list =
        match list with
        | [] -> true
        | _ -> false

    [<CompiledName("Append")>]
    let append list1 list2 =
        list1 @ list2

    [<CompiledName("Item")>]
    let rec item index list =
        match list with
        | h :: t when index >= 0 ->
            if index = 0 then
                h
            else
                item (index - 1) t
        | _ -> invalidArg "index" (SR.GetString(SR.indexOutOfBounds))

    [<CompiledName("TryItem")>]
    let rec tryItem index list =
        match list with
        | h :: t when index >= 0 ->
            if index = 0 then
                Some h
            else
                tryItem (index - 1) t
        | _ -> None

    [<CompiledName("Get")>]
    let nth list index =
        item index list

    [<CompiledName("Choose")>]
    let choose chooser list =
        Microsoft.FSharp.Primitives.Basics.List.choose chooser list

    [<CompiledName("SplitAt")>]
    let splitAt index (list: 'T list) =
        Microsoft.FSharp.Primitives.Basics.List.splitAt index list

    [<CompiledName("Take")>]
    let take count (list: 'T list) =
        Microsoft.FSharp.Primitives.Basics.List.take count list

    [<CompiledName("TakeWhile")>]
    let takeWhile predicate (list: 'T list) =
        Microsoft.FSharp.Primitives.Basics.List.takeWhile predicate list

    [<CompiledName("IterateIndexed")>]
    let inline iteri ([<InlineIfLambda>] action) (list: 'T list) =
        let mutable n = 0

        for x in list do
            action n x
            n <- n + 1

    [<CompiledName("Initialize")>]
    let init length initializer =
        Microsoft.FSharp.Primitives.Basics.List.init length initializer

    [<CompiledName("Replicate")>]
    let replicate count initial =
        if count < 0 then
            invalidArg "count" (SR.GetString(SR.inputMustBeNonNegative))

        let mutable result = []

        for i in 0 .. count - 1 do
            result <- initial :: result

        result

    [<CompiledName("Iterate2")>]
    let iter2 action list1 list2 =
        let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(action)

        let rec loop list1 list2 =
            match list1, list2 with
            | [], [] -> ()
            | h1 :: t1, h2 :: t2 ->
                f.Invoke(h1, h2)
                loop t1 t2
            | [], xs2 -> invalidArgDifferentListLength "list1" "list2" xs2.Length
            | xs1, [] -> invalidArgDifferentListLength "list2" "list1" xs1.Length

        loop list1 list2

    [<CompiledName("IterateIndexed2")>]
    let iteri2 action list1 list2 =
        let f = OptimizedClosures.FSharpFunc<_, _, _, _>.Adapt(action)

        let rec loop n list1 list2 =
            match list1, list2 with
            | [], [] -> ()
            | h1 :: t1, h2 :: t2 ->
                f.Invoke(n, h1, h2)
                loop (n + 1) t1 t2
            | [], xs2 -> invalidArgDifferentListLength "list1" "list2" xs2.Length
            | xs1, [] -> invalidArgDifferentListLength "list2" "list1" xs1.Length

        loop 0 list1 list2

    [<CompiledName("Map3")>]
    let map3 mapping list1 list2 list3 =
        Microsoft.FSharp.Primitives.Basics.List.map3 mapping list1 list2 list3

    [<CompiledName("MapIndexed2")>]
    let mapi2 mapping list1 list2 =
        Microsoft.FSharp.Primitives.Basics.List.mapi2 mapping list1 list2

    [<CompiledName("Map2")>]
    let map2 mapping list1 list2 =
        Microsoft.FSharp.Primitives.Basics.List.map2 mapping list1 list2

    [<CompiledName("Fold")>]
    let fold<'T, 'State> folder (state: 'State) (list: 'T list) =
        match list with
        | [] -> state
        | _ ->
            let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(folder)
            let mutable acc = state

            for x in list do
                acc <- f.Invoke(acc, x)

            acc

    [<CompiledName("Pairwise")>]
    let pairwise (list: 'T list) =
        Microsoft.FSharp.Primitives.Basics.List.pairwise list

    [<CompiledName("Reduce")>]
    let reduce reduction list =
        match list with
        | [] -> invalidArg "list" (SR.GetString(SR.inputListWasEmpty))
        | h :: t -> fold reduction h t

    [<CompiledName("Scan")>]
    let scan<'T, 'State> folder (state: 'State) (list: 'T list) =
        Microsoft.FSharp.Primitives.Basics.List.scan folder state list

    [<CompiledName("Singleton")>]
    let inline singleton value =
        [ value ]

    [<CompiledName("Fold2")>]
    let fold2<'T1, 'T2, 'State> folder (state: 'State) (list1: 'T1 list) (list2: 'T2 list) =
        let f = OptimizedClosures.FSharpFunc<_, _, _, _>.Adapt(folder)

        let rec loop acc list1 list2 =
            match list1, list2 with
            | [], [] -> acc
            | h1 :: t1, h2 :: t2 -> loop (f.Invoke(acc, h1, h2)) t1 t2
            | [], xs2 -> invalidArgDifferentListLength "list1" "list2" xs2.Length
            | xs1, [] -> invalidArgDifferentListLength "list2" "list1" xs1.Length

        loop state list1 list2

    let foldArraySubRight (f: OptimizedClosures.FSharpFunc<'T, _, _>) (arr: 'T array) start fin acc =
        let mutable state = acc

        for i = fin downto start do
            state <- f.Invoke(arr.[i], state)

        state

    // this version doesn't causes stack overflow - it uses a private stack
    [<CompiledName("FoldBack")>]
    let foldBack<'T, 'State> folder (list: 'T list) (state: 'State) =
        let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(folder)

        match list with
        | [] -> state
        | [ h ] -> f.Invoke(h, state)
        | [ h1; h2 ] -> f.Invoke(h1, f.Invoke(h2, state))
        | [ h1; h2; h3 ] -> f.Invoke(h1, f.Invoke(h2, f.Invoke(h3, state)))
        | [ h1; h2; h3; h4 ] -> f.Invoke(h1, f.Invoke(h2, f.Invoke(h3, f.Invoke(h4, state))))
        | _ ->
            // It is faster to allocate and iterate an array than to create all those
            // highly nested stacks.  It also means we won't get stack overflows here.
            let arr = toArray list
            let arrn = arr.Length
            foldArraySubRight f arr 0 (arrn - 1) state

    [<CompiledName("ReduceBack")>]
    let reduceBack reduction list =
        match list with
        | [] -> invalidArg "list" (SR.GetString(SR.inputListWasEmpty))
        | _ ->
            let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(reduction)
            let arr = toArray list
            let arrn = arr.Length
            foldArraySubRight f arr 0 (arrn - 2) arr.[arrn - 1]

    let scanArraySubRight<'T, 'State>
        (f: OptimizedClosures.FSharpFunc<'T, 'State, 'State>)
        (arr: _ array)
        start
        fin
        initState
        =
        let mutable state = initState
        let mutable res = [ state ]

        for i = fin downto start do
            state <- f.Invoke(arr.[i], state)
            res <- state :: res

        res

    [<CompiledName("ScanBack")>]
    let scanBack<'T, 'State> folder (list: 'T list) (state: 'State) =
        match list with
        | [] -> [ state ]
        | [ h ] -> [ folder h state; state ]
        | _ ->
            let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(folder)
            // It is faster to allocate and iterate an array than to create all those
            // highly nested stacks.  It also means we won't get stack overflows here.
            let arr = toArray list
            let arrn = arr.Length
            scanArraySubRight f arr 0 (arrn - 1) state

    let foldBack2UsingArrays (f: OptimizedClosures.FSharpFunc<_, _, _, _>) list1 list2 acc =
        let arr1 = toArray list1
        let arr2 = toArray list2
        let n1 = arr1.Length
        let n2 = arr2.Length

        if n1 <> n2 then
            invalidArgFmt
                "list1, list2"
                "{0}\nlist1.Length = {1}, list2.Length = {2}"
                [| SR.GetString SR.listsHadDifferentLengths; arr1.Length; arr2.Length |]

        let mutable res = acc

        for i = n1 - 1 downto 0 do
            res <- f.Invoke(arr1.[i], arr2.[i], res)

        res

    [<CompiledName("FoldBack2")>]
    let rec foldBack2<'T1, 'T2, 'State> folder (list1: 'T1 list) (list2: 'T2 list) (state: 'State) =
        match list1, list2 with
        | [], [] -> state
        | h1 :: rest1, k1 :: rest2 ->
            let f = OptimizedClosures.FSharpFunc<_, _, _, _>.Adapt(folder)

            match rest1, rest2 with
            | [], [] -> f.Invoke(h1, k1, state)
            | [ h2 ], [ k2 ] -> f.Invoke(h1, k1, f.Invoke(h2, k2, state))
            | [ h2; h3 ], [ k2; k3 ] -> f.Invoke(h1, k1, f.Invoke(h2, k2, f.Invoke(h3, k3, state)))
            | [ h2; h3; h4 ], [ k2; k3; k4 ] ->
                f.Invoke(h1, k1, f.Invoke(h2, k2, f.Invoke(h3, k3, f.Invoke(h4, k4, state))))
            | _ -> foldBack2UsingArrays f list1 list2 state
        | [], xs2 -> invalidArgDifferentListLength "list1" "list2" xs2.Length
        | xs1, [] -> invalidArgDifferentListLength "list2" "list1" xs1.Length

    let rec forall2aux (f: OptimizedClosures.FSharpFunc<_, _, _>) list1 list2 =
        match list1, list2 with
        | [], [] -> true
        | h1 :: t1, h2 :: t2 -> f.Invoke(h1, h2) && forall2aux f t1 t2
        | [], xs2 -> invalidArgDifferentListLength "list1" "list2" xs2.Length
        | xs1, [] -> invalidArgDifferentListLength "list2" "list1" xs1.Length

    [<CompiledName("ForAll2")>]
    let forall2 predicate list1 list2 =
        match list1, list2 with
        | [], [] -> true
        | _ ->
            let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(predicate)
            forall2aux f list1 list2

    [<CompiledName("ForAll")>]
    let forall predicate list =
        Microsoft.FSharp.Primitives.Basics.List.forall predicate list

    [<CompiledName("Exists")>]
    let exists predicate list =
        Microsoft.FSharp.Primitives.Basics.List.exists predicate list

    [<CompiledName("Contains")>]
    let inline contains value source =
        let equalityCheck listElement =
            value = listElement

        let rec contains e xs1 =
            match xs1 with
            | [] -> false
            | h1 :: t1 -> equalityCheck h1 || contains e t1

        contains value source

    let rec exists2aux (f: OptimizedClosures.FSharpFunc<_, _, _>) list1 list2 =
        match list1, list2 with
        | [], [] -> false
        | h1 :: t1, h2 :: t2 -> f.Invoke(h1, h2) || exists2aux f t1 t2
        | _ -> invalidArg "list2" (SR.GetString(SR.listsHadDifferentLengths))

    [<CompiledName("Exists2")>]
    let rec exists2 predicate list1 list2 =
        match list1, list2 with
        | [], [] -> false
        | _ ->
            let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt(predicate)
            exists2aux f list1 list2

    [<CompiledName("Find")>]
    let rec find predicate list =
        match list with
        | [] -> indexNotFound ()
        | h :: t ->
            if predicate h then
                h
            else
                find predicate t

    [<CompiledName("TryFind")>]
    let rec tryFind predicate list =
        match list with
        | [] -> None
        | h :: t ->
            if predicate h then
                Some h
            else
                tryFind predicate t

    [<CompiledName("FindBack")>]
    let findBack predicate list =
        list |> toArray |> Microsoft.FSharp.Primitives.Basics.Array.findBack predicate

    [<CompiledName("TryFindBack")>]
    let tryFindBack predicate list =
        list
        |> toArray
        |> Microsoft.FSharp.Primitives.Basics.Array.tryFindBack predicate

    [<CompiledName("TryPick")>]
    let rec tryPick chooser list =
        match list with
        | [] -> None
        | h :: t ->
            match chooser h with
            | None -> tryPick chooser t
            | r -> r

    [<CompiledName("Pick")>]
    let rec pick chooser list =
        match list with
        | [] -> indexNotFound ()
        | h :: t ->
            match chooser h with
            | None -> pick chooser t
            | Some r -> r

    [<CompiledName("Filter")>]
    let filter predicate list =
        Microsoft.FSharp.Primitives.Basics.List.filter predicate list

    [<CompiledName("Except")>]
    let except (itemsToExclude: seq<'T>) list =
        checkNonNull "itemsToExclude" itemsToExclude

        match list with
        | [] -> list
        | _ ->
            let cached = HashSet(itemsToExclude, HashIdentity.Structural)
            list |> filter cached.Add

    [<CompiledName("Where")>]
    let where predicate list =
        Microsoft.FSharp.Primitives.Basics.List.filter predicate list

    let inline groupByImpl
        (comparer: IEqualityComparer<'SafeKey>)
        (keyf: 'T -> 'SafeKey)
        (getKey: 'SafeKey -> 'Key)
        (list: 'T list)
        =
        Microsoft.FSharp.Primitives.Basics.List.groupBy comparer keyf getKey list

    // We avoid wrapping a StructBox, because under 64 JIT we get some "hard" tailcalls which affect performance
    let groupByValueType (keyf: 'T -> 'Key) (list: 'T list) =
        groupByImpl HashIdentity.Structural<'Key> keyf id list

    // Wrap a StructBox around all keys in case the key type is itself a type using null as a representation
    let groupByRefType (keyf: 'T -> 'Key) (list: 'T list) =
        groupByImpl RuntimeHelpers.StructBox<'Key>.Comparer (keyf >> RuntimeHelpers.StructBox) (fun sb -> sb.Value) list

    [<CompiledName("GroupBy")>]
    let groupBy (projection: 'T -> 'Key) (list: 'T list) =
        match list with
        | [] -> []
        | _ ->
            if typeof<'Key>.IsValueType then
                groupByValueType projection list
            else
                groupByRefType projection list

    [<CompiledName("Partition")>]
    let partition predicate list =
        Microsoft.FSharp.Primitives.Basics.List.partition predicate list

    [<CompiledName("Unzip")>]
    let unzip list =
        Microsoft.FSharp.Primitives.Basics.List.unzip list

    [<CompiledName("Unzip3")>]
    let unzip3 list =
        Microsoft.FSharp.Primitives.Basics.List.unzip3 list

    [<CompiledName("Windowed")>]
    let windowed windowSize list =
        Microsoft.FSharp.Primitives.Basics.List.windowed windowSize list

    [<CompiledName("ChunkBySize")>]
    let chunkBySize chunkSize list =
        Microsoft.FSharp.Primitives.Basics.List.chunkBySize chunkSize list

    [<CompiledName("SplitInto")>]
    let splitInto count list =
        Microsoft.FSharp.Primitives.Basics.List.splitInto count list

    [<CompiledName("Zip")>]
    let zip list1 list2 =
        Microsoft.FSharp.Primitives.Basics.List.zip list1 list2

    [<CompiledName("Zip3")>]
    let zip3 list1 list2 list3 =
        Microsoft.FSharp.Primitives.Basics.List.zip3 list1 list2 list3

    [<CompiledName("Skip")>]
    let skip count list =
        if count <= 0 then
            list
        else
            let rec loop i lst =
                match lst with
                | _ when i = 0 -> lst
                | _ :: t -> loop (i - 1) t
                | [] -> invalidArgOutOfRange "count" count "distance past the list" i

            loop count list

    [<CompiledName("SkipWhile")>]
    let rec skipWhile predicate list =
        match list with
        | head :: tail when predicate head -> skipWhile predicate tail
        | _ -> list

    [<CompiledName("SortWith")>]
    let sortWith comparer list =
        match list with
        | []
        | [ _ ] -> list
        | _ ->
            let array = Microsoft.FSharp.Primitives.Basics.List.toArray list
            Microsoft.FSharp.Primitives.Basics.Array.stableSortInPlaceWith comparer array
            Microsoft.FSharp.Primitives.Basics.List.ofArray array

    [<CompiledName("SortBy")>]
    let sortBy projection list =
        match list with
        | []
        | [ _ ] -> list
        | _ ->
            let array = Microsoft.FSharp.Primitives.Basics.List.toArray list
            Microsoft.FSharp.Primitives.Basics.Array.stableSortInPlaceBy projection array
            Microsoft.FSharp.Primitives.Basics.List.ofArray array

    [<CompiledName("Sort")>]
    let sort list =
        match list with
        | []
        | [ _ ] -> list
        | _ ->
            let array = Microsoft.FSharp.Primitives.Basics.List.toArray list
            Microsoft.FSharp.Primitives.Basics.Array.stableSortInPlace array
            Microsoft.FSharp.Primitives.Basics.List.ofArray array

    [<CompiledName("SortByDescending")>]
    let inline sortByDescending projection list =
        let inline compareDescending a b =
            compare (projection b) (projection a)

        sortWith compareDescending list

    [<CompiledName("SortDescending")>]
    let inline sortDescending list =
        let inline compareDescending a b =
            compare b a

        sortWith compareDescending list

    [<CompiledName("OfSeq")>]
    let ofSeq source =
        Seq.toList source

    [<CompiledName("ToSeq")>]
    let toSeq list =
        Seq.ofList list

    [<CompiledName("FindIndex")>]
    let findIndex predicate list =
        let rec loop n list =
            match list with
            | [] -> indexNotFound ()
            | h :: t ->
                if predicate h then
                    n
                else
                    loop (n + 1) t

        loop 0 list

    [<CompiledName("TryFindIndex")>]
    let tryFindIndex predicate list =
        let rec loop n list =
            match list with
            | [] -> None
            | h :: t ->
                if predicate h then
                    Some n
                else
                    loop (n + 1) t

        loop 0 list

    [<CompiledName("FindIndexBack")>]
    let findIndexBack predicate list =
        list
        |> toArray
        |> Microsoft.FSharp.Primitives.Basics.Array.findIndexBack predicate

    [<CompiledName("TryFindIndexBack")>]
    let tryFindIndexBack predicate list =
        list
        |> toArray
        |> Microsoft.FSharp.Primitives.Basics.Array.tryFindIndexBack predicate

    [<CompiledName("Sum")>]
    let inline sum (list: 'T list) =
        match list with
        | [] -> LanguagePrimitives.GenericZero<'T>
        | t ->
            let mutable acc = LanguagePrimitives.GenericZero<'T>

            for x in t do
                acc <- Checked.(+) acc x

            acc

    [<CompiledName("SumBy")>]
    let inline sumBy ([<InlineIfLambda>] projection: 'T -> 'U) (list: 'T list) =
        match list with
        | [] -> LanguagePrimitives.GenericZero<'U>
        | t ->
            let mutable acc = LanguagePrimitives.GenericZero<'U>

            for x in t do
                acc <- Checked.(+) acc (projection x)

            acc

    [<CompiledName("Max")>]
    let inline max (list: _ list) =
        match list with
        | [] -> invalidArg "list" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        | h :: t ->
            let mutable acc = h

            for x in t do
                if x > acc then
                    acc <- x

            acc

    [<CompiledName("MaxBy")>]
    let inline maxBy projection (list: _ list) =
        match list with
        | [] -> invalidArg "list" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        | h :: t ->
            let mutable acc = h
            let mutable accv = projection h

            for x in t do
                let currv = projection x

                if currv > accv then
                    acc <- x
                    accv <- currv

            acc

    [<CompiledName("Min")>]
    let inline min (list: _ list) =
        match list with
        | [] -> invalidArg "list" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        | h :: t ->
            let mutable acc = h

            for x in t do
                if x < acc then
                    acc <- x

            acc

    [<CompiledName("MinBy")>]
    let inline minBy projection (list: _ list) =
        match list with
        | [] -> invalidArg "list" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        | h :: t ->
            let mutable acc = h
            let mutable accv = projection h

            for x in t do
                let currv = projection x

                if currv < accv then
                    acc <- x
                    accv <- currv

            acc

    [<CompiledName("Average")>]
    let inline average (list: 'T list) =
        match list with
        | [] -> invalidArg "source" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        | xs ->
            let mutable sum = LanguagePrimitives.GenericZero<'T>
            let mutable count = 0

            for x in xs do
                sum <- Checked.(+) sum x
                count <- count + 1

            LanguagePrimitives.DivideByInt sum count

    [<CompiledName("AverageBy")>]
    let inline averageBy ([<InlineIfLambda>] projection: 'T -> 'U) (list: 'T list) =
        match list with
        | [] -> invalidArg "source" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        | xs ->
            let mutable sum = LanguagePrimitives.GenericZero<'U>
            let mutable count = 0

            for x in xs do
                sum <- Checked.(+) sum (projection x)
                count <- count + 1

            LanguagePrimitives.DivideByInt sum count

    [<CompiledName("Collect")>]
    let collect mapping list =
        Microsoft.FSharp.Primitives.Basics.List.collect mapping list

    [<CompiledName("AllPairs")>]
    let allPairs list1 list2 =
        Microsoft.FSharp.Primitives.Basics.List.allPairs list1 list2

    [<CompiledName("CompareWith")>]
    let inline compareWith ([<InlineIfLambda>] comparer: 'T -> 'T -> int) (list1: 'T list) (list2: 'T list) =
        let rec loop list1 list2 =
            match list1, list2 with
            | head1 :: tail1, head2 :: tail2 ->
                let c = comparer head1 head2
                if c = 0 then loop tail1 tail2 else c
            | [], [] -> 0
            | _, [] -> 1
            | [], _ -> -1

        loop list1 list2

    [<CompiledName("Permute")>]
    let permute indexMap list =
        list
        |> toArray
        |> Microsoft.FSharp.Primitives.Basics.Array.permute indexMap
        |> ofArray

    [<CompiledName("ExactlyOne")>]
    let exactlyOne (list: _ list) =
        match list with
        | [ x ] -> x
        | [] -> invalidArg "source" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        | _ -> invalidArg "source" (SR.GetString(SR.inputSequenceTooLong))

    [<CompiledName("TryExactlyOne")>]
    let tryExactlyOne (list: _ list) =
        match list with
        | [ x ] -> Some x
        | _ -> None

    [<CompiledName("Transpose")>]
    let transpose (lists: seq<'T list>) =
        checkNonNull "lists" lists
        Microsoft.FSharp.Primitives.Basics.List.transpose (ofSeq lists)

    [<CompiledName("Truncate")>]
    let truncate count list =
        Microsoft.FSharp.Primitives.Basics.List.truncate count list

    [<CompiledName("Unfold")>]
    let unfold<'T, 'State> (generator: 'State -> ('T * 'State) option) (state: 'State) =
        Microsoft.FSharp.Primitives.Basics.List.unfold generator state

    [<CompiledName("RemoveAt")>]
    let removeAt (index: int) (source: 'T list) : 'T list =
        if index < 0 then
            invalidArg "index" "index must be within bounds of the list"

        let mutable i = 0
        let mutable coll = ListCollector()
        let mutable curr = source

        while i < index do // traverse and save the linked list until item to be removed
            match curr with
            | [] -> invalidArg "index" "index must be within bounds of the list"
            | h :: t ->
                coll.Add(h)
                curr <- t

            i <- i + 1

        if curr.IsEmpty then
            invalidArg "index" "index must be within bounds of the list"
        else
            coll.AddManyAndClose(curr.Tail) // when i = index, Head is the item which is ignored and Tail is the rest of the list

    [<CompiledName("RemoveManyAt")>]
    let removeManyAt (index: int) (count: int) (source: 'T list) : 'T list =
        if index < 0 then
            invalidArg "index" "index must be within bounds of the list"

        let mutable i = 0
        let mutable coll = ListCollector()
        let mutable curr = source

        while i < index + count do // traverse and save the linked list until the last item to be removed
            match curr with
            | [] -> invalidArg "index" "index must be within bounds of the list"
            | h :: t ->
                if i < index then
                    coll.Add(h) //items before index we keep

                curr <- t

            i <- i + 1

        coll.AddManyAndClose(curr) // when i = index + count, we keep the rest of the list

    [<CompiledName("UpdateAt")>]
    let updateAt (index: int) (value: 'T) (source: 'T list) : 'T list =
        if index < 0 then
            invalidArg "index" "index must be within bounds of the list"

        let mutable i = 0
        let mutable coll = ListCollector()
        let mutable curr = source

        while i < index do // Traverse and save the linked list until index
            match curr with
            | [] -> invalidArg "index" "index must be within bounds of the list"
            | h :: t ->
                coll.Add(h)
                curr <- t

            i <- i + 1

        coll.Add(value) // add value instead of Head

        if curr.IsEmpty then
            invalidArg "index" "index must be within bounds of the list"
        else
            coll.AddManyAndClose(curr.Tail)

    [<CompiledName("InsertAt")>]
    let insertAt (index: int) (value: 'T) (source: 'T list) : 'T list =
        if index < 0 then
            invalidArg "index" "index must be within bounds of the list"

        let mutable i = 0
        let mutable coll = ListCollector()
        let mutable curr = source

        while i < index do // traverse and save the linked list until index
            match curr with
            | [] -> invalidArg "index" "index must be within bounds of the list"
            | h :: t ->
                coll.Add(h)
                curr <- t

            i <- i + 1

        coll.Add(value)
        coll.AddManyAndClose(curr) // insert item BEFORE the item at the index

    [<CompiledName("InsertManyAt")>]
    let insertManyAt (index: int) (values: seq<'T>) (source: 'T list) : 'T list =
        if index < 0 then
            invalidArg "index" "index must be within bounds of the list"

        let mutable i = 0
        let mutable coll = ListCollector()
        let mutable curr = source

        while i < index do // traverse and save the linked list until index
            match curr with
            | [] -> invalidArg "index" "index must be within bounds of the list"
            | h :: t ->
                coll.Add(h)
                curr <- t

            i <- i + 1

        coll.AddMany(values) // insert values BEFORE the item at the index
        coll.AddManyAndClose(curr)

    [<CompiledName("RandomShuffleWith")>]
    let randomShuffleWith (random: Random) (source: 'T list) : 'T list =
        checkNonNull "random" random

        let tempArray = toArray source
        Microsoft.FSharp.Primitives.Basics.Random.shuffleArrayInPlaceWith random tempArray
        ofArray tempArray

    [<CompiledName("RandomShuffleBy")>]
    let randomShuffleBy (randomizer: unit -> float) (source: 'T list) : 'T list =
        let tempArray = toArray source
        Microsoft.FSharp.Primitives.Basics.Random.shuffleArrayInPlaceBy randomizer tempArray
        ofArray tempArray

    [<CompiledName("RandomShuffle")>]
    let randomShuffle (source: 'T list) : 'T list =
        randomShuffleWith ThreadSafeRandom.Shared source

    [<CompiledName("RandomChoiceWith")>]
    let randomChoiceWith (random: Random) (source: 'T list) : 'T =
        checkNonNull "random" random

        let inputLength = source.Length

        if inputLength = 0 then
            invalidArg "source" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString

        let i = random.Next(0, inputLength)
        source[i]

    [<CompiledName("RandomChoiceBy")>]
    let randomChoiceBy (randomizer: unit -> float) (source: 'T list) : 'T =
        let inputLength = source.Length

        if inputLength = 0 then
            invalidArg "source" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString

        let i = Microsoft.FSharp.Primitives.Basics.Random.next randomizer 0 inputLength
        source[i]

    [<CompiledName("RandomChoice")>]
    let randomChoice (source: 'T list) : 'T =
        randomChoiceWith ThreadSafeRandom.Shared source

    [<CompiledName("RandomChoicesWith")>]
    let randomChoicesWith (random: Random) (count: int) (source: 'T list) : 'T list =
        checkNonNull "random" random

        if count < 0 then
            invalidArgInputMustBeNonNegative "count" count

        let inputLength = source.Length

        if inputLength = 0 then
            if count = 0 then
                []
            else
                invalidArg "source" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        else
            [
                for _ = 0 to count - 1 do
                    let j = random.Next(0, inputLength)
                    source[j]
            ]

    [<CompiledName("RandomChoicesBy")>]
    let randomChoicesBy (randomizer: unit -> float) (count: int) (source: 'T list) : 'T list =
        if count < 0 then
            invalidArgInputMustBeNonNegative "count" count

        let inputLength = source.Length

        if inputLength = 0 then
            if count = 0 then
                []
            else
                invalidArg "source" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        else
            [
                for _ = 0 to count - 1 do
                    let j = Microsoft.FSharp.Primitives.Basics.Random.next randomizer 0 inputLength
                    source[j]
            ]

    [<CompiledName("RandomChoices")>]
    let randomChoices (count: int) (source: 'T list) : 'T list =
        randomChoicesWith ThreadSafeRandom.Shared count source

    [<CompiledName("RandomSampleWith")>]
    let randomSampleWith (random: Random) (count: int) (source: 'T list) : 'T list =
        checkNonNull "random" random

        if count < 0 then
            invalidArgInputMustBeNonNegative "count" count

        let inputLength = source.Length

        if inputLength = 0 then
            if count = 0 then
                []
            else
                invalidArg "source" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        else
            if count > inputLength then
                invalidArg "count" (SR.GetString(SR.notEnoughElements))

            // algorithm taken from https://github.com/python/cpython/blob/69b3e8ea569faabccd74036e3d0e5ec7c0c62a20/Lib/random.py#L363-L456
            let setSize =
                Microsoft.FSharp.Primitives.Basics.Random.getMaxSetSizeForSampling count

            if inputLength <= setSize then
                let pool = source |> toArray

                [
                    for i = 0 to count - 1 do
                        let j = random.Next(0, inputLength - i)
                        let item = pool[j]
                        pool[j] <- pool[inputLength - i - 1]
                        item
                ]
            else
                let selected = HashSet()

                [
                    for _ = 0 to count - 1 do
                        let mutable j = random.Next(0, inputLength)

                        while not (selected.Add j) do
                            j <- random.Next(0, inputLength)

                        source[j]
                ]

    [<CompiledName("RandomSampleBy")>]
    let randomSampleBy (randomizer: unit -> float) (count: int) (source: 'T list) : 'T list =
        if count < 0 then
            invalidArgInputMustBeNonNegative "count" count

        let inputLength = source.Length

        if inputLength = 0 then
            if count = 0 then
                []
            else
                invalidArg "source" LanguagePrimitives.ErrorStrings.InputSequenceEmptyString
        else
            if count > inputLength then
                invalidArg "count" (SR.GetString(SR.notEnoughElements))

            let setSize =
                Microsoft.FSharp.Primitives.Basics.Random.getMaxSetSizeForSampling count

            if inputLength <= setSize then
                let pool = source |> toArray

                [
                    for i = 0 to count - 1 do
                        let j =
                            Microsoft.FSharp.Primitives.Basics.Random.next randomizer 0 (inputLength - i)

                        let item = pool[j]
                        pool[j] <- pool[inputLength - i - 1]
                        item
                ]
            else
                let selected = HashSet()

                [
                    for _ = 0 to count - 1 do
                        let mutable j =
                            Microsoft.FSharp.Primitives.Basics.Random.next randomizer 0 inputLength

                        while not (selected.Add j) do
                            j <- Microsoft.FSharp.Primitives.Basics.Random.next randomizer 0 inputLength

                        source[j]
                ]

    [<CompiledName("RandomSample")>]
    let randomSample (count: int) (source: 'T list) : 'T list =
        randomSampleWith ThreadSafeRandom.Shared count source
