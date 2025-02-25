namespace TrafficSimulationTool.Runtime.SimData
{
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class CsvHeaderAttribute : System.Attribute
    {
        public string HeaderName { get; }

        public CsvHeaderAttribute(string headerName)
        {
            HeaderName = headerName;
        }
    }
}