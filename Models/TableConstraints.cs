namespace Schema_Converters.Models
{
    public class TableConstraints
    {
        public string TableName { get; set; }
        public Dictionary<string, string>? Columns { get; set; }
        public Dictionary<string, string>? PrimaryKeys { get; set; }
        public Dictionary<string, string>? ForeignKeys { get; set; }
        public Dictionary<string, string>? Constraints { get; set; }
    }
}
