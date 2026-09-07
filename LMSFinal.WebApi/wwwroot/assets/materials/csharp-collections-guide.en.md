# Choosing a collection — C# Fundamentals

Pick a collection by what you do with it most often, not by habit.

## Quick reference

| Task | Type | Lookup | Why |
|---|---|---|---|
| A plain ordered list | `List<T>` | O(n) | Indexing, sorting, minimal overhead |
| Checking "does it contain X" | `HashSet<T>` | O(1) | Hashing instead of scanning |
| Looking a value up by key | `Dictionary<K,V>` | O(1) | Same, but with a payload |
| Handing data out read-only | `IReadOnlyList<T>` | — | The type says: don't modify |
| A queue of work | `Queue<T>` | — | FIFO without manual indexing |

## The mistake that costs the most

```csharp
// O(n) inside a loop over n items = O(n²).
// With 10,000 records that is millions of comparisons.
foreach (var id in ids)
{
    if (allLessons.Any(l => l.Id == id)) { /* ... */ }
}

// One pass to build the set, then O(1) per check.
var lessonIds = allLessons.Select(l => l.Id).ToHashSet();

foreach (var id in ids)
{
    if (lessonIds.Contains(id)) { /* ... */ }
}
```

## Modifying a collection while iterating it

`foreach` throws `InvalidOperationException` if you add or remove inside the
loop. Collect the changes first:

```csharp
var toRemove = items.Where(i => i.IsExpired).ToList();
foreach (var item in toRemove)
{
    items.Remove(item);
}
```

## LINQ is lazy

`Where` and `Select` compute nothing until the result is enumerated. Two
`foreach` loops over the same query mean two passes over the source. If you
need the result more than once, materialise it: `.ToList()`.
