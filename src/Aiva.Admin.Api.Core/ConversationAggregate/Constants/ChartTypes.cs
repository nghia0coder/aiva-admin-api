namespace Aiva.Admin.Api.Core.ConversationAggregate.Constants;

public static class ChartTypes
{
    // Chart.js compatible chart types
    public const string Line = "line";
    public const string Bar = "bar"; // Chart.js uses 'bar' for both vertical and horizontal bars
    public const string Pie = "pie";
    public const string Doughnut = "doughnut";
    public const string PolarArea = "polarArea";
    public const string Radar = "radar";
    public const string Bubble = "bubble";
    public const string Scatter = "scatter";
    
    // Mapping from old Highcharts types to Chart.js types
    public const string Column = "bar"; // Highcharts column = Chart.js bar
    public const string Spline = "line"; // Highcharts spline = Chart.js line with tension
    public const string Area = "line"; // Highcharts area = Chart.js line with fill
    public const string SplineArea = "line"; // Highcharts splineArea = Chart.js line with tension and fill

    public static readonly List<string> AllTypes = new()
    {
        Line,
        Bar,
        Pie,
        Doughnut,
        PolarArea,
        Radar,
        Bubble,
        Scatter
    };
    
    // Helper method to convert Highcharts types to Chart.js types
    public static string ConvertToChartJs(string highchartsType)
    {
        return highchartsType switch
        {
            "column" => Bar,
            "spline" => Line,
            "area" => Line,
            "splineArea" => Line,
            _ => highchartsType
        };
    }
}
