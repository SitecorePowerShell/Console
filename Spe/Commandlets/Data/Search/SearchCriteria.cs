namespace Spe.Commandlets.Data.Search
{
    public class SearchCriteria
    {
        public FilterType Filter { get; set; }
        public string Field { get; set; }
        public object Value { get; set; }
        public bool? CaseSensitive { get; set; }
        internal string StringValue { get { return Value.ToString(); } }
        public bool Invert { get; set; }
        public float Boost { get; set; } = 1;
    }
}