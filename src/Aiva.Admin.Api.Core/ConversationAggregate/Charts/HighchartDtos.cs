using System.Text.Json.Serialization;

namespace Aiva.Admin.Api.Core.ConversationAggregate.Charts;

public class HighchartConfigDto
{
    [JsonPropertyName("chart")]
    public HighchartChartDto? Chart { get; set; }
    
    [JsonPropertyName("title")]
    public HighchartTitleDto? Title { get; set; }
    
    [JsonPropertyName("xAxis")]
    public HighchartXAxisDto? XAxis { get; set; }
    
    [JsonPropertyName("yAxis")]
    public HighchartYAxisDto? YAxis { get; set; }
    
    [JsonPropertyName("plotOptions")]
    public HighchartPlotOptionsDto? PlotOptions { get; set; }
    
    [JsonPropertyName("legend")]
    public HighchartLegendDto? Legend { get; set; }
    
    [JsonPropertyName("series")]
    public List<HighchartSeriesDto>? Series { get; set; }
}

public class HighchartChartDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; }
}

public class HighchartTitleDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class HighchartXAxisDto
{
    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }
}

public class HighchartYAxisDto
{
    [JsonPropertyName("title")]
    public HighchartYAxisTitleDto? Title { get; set; }
    
    [JsonPropertyName("labels")]
    public HighchartYAxisLabelsDto? Labels { get; set; }
}

public class HighchartYAxisTitleDto
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class HighchartYAxisLabelsDto
{
    [JsonPropertyName("formatter")]
    public string? Formatter { get; set; }
}

public class HighchartPlotOptionsDto
{
    [JsonPropertyName("column")]
    public HighchartPlotOptionsColumnDto? Column { get; set; }
}

public class HighchartPlotOptionsColumnDto
{
    [JsonPropertyName("pointPadding")]
    public double PointPadding { get; set; }
    
    [JsonPropertyName("borderWidth")]
    public double BorderWidth { get; set; }
}

public class HighchartLegendDto
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

public class HighchartSeriesDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("dataLabels")]
    public HighchartSeriesDataLabelsDto? DataLabels { get; set; }
    
    [JsonPropertyName("data")]
    public List<object>? Data { get; set; }
}

public class HighchartSeriesDataLabelsDto
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}
