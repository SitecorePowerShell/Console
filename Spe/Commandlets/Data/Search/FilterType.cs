namespace Spe.Commandlets.Data.Search
{
    public enum FilterType
    {
        None,
        Equals,
        StartsWith,
        Contains,
        ContainsAll,
        ContainsAny,
        EndsWith,
        DescendantOf,
        Fuzzy,
        InclusiveRange,
        ExclusiveRange,
        MatchesRegex,
        MatchesWildcard
    }
}