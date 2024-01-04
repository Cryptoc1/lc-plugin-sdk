namespace LethalCompany.Plugin.Sdk.Internal;

internal static class EnumerableExtensions
{
    public static IEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        Func<TOuter, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TOuter?, TInner?, TResult> resultSelector)
    {
        var outerLookup = outer.ToLookup(outerKeySelector);
        var innerLookup = inner.ToLookup(innerKeySelector);

        var keys = new HashSet<TKey>(
            outerLookup.Select(group => group.Key)
                .Concat(
                    innerLookup.Select(group => group.Key)));

        foreach (var key in keys)
        {
            foreach (var outerItem in outerLookup[key].DefaultIfEmpty())
            {
                foreach (var innerItem in innerLookup[key].DefaultIfEmpty())
                {
                    yield return resultSelector(outerItem, innerItem);
                }
            }
        }
    }
}
