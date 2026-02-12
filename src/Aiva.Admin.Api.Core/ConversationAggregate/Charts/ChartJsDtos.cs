using System.Text.Json.Serialization;

namespace Aiva.Admin.Api.Core.ConversationAggregate.Charts;

public class ChartJsConfigDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("data")]
    public ChartJsDataDto? Data { get; set; }
    
    [JsonPropertyName("options")]
    public ChartJsOptionsDto? Options { get; set; }
}

public class ChartJsDataDto
{
    [JsonPropertyName("labels")]
    public List<string>? Labels { get; set; }
    
    [JsonPropertyName("datasets")]
    public List<ChartJsDatasetDto>? Datasets { get; set; }
}

public class ChartJsDatasetDto
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }
    
    [JsonPropertyName("data")]
    public List<object>? Data { get; set; }
    
    [JsonPropertyName("backgroundColor")]
    public object? BackgroundColor { get; set; }
    
    [JsonPropertyName("borderColor")]
    public object? BorderColor { get; set; }
    
    [JsonPropertyName("borderWidth")]
    public double BorderWidth { get; set; } = 1;
    
    [JsonPropertyName("tension")]
    public double Tension { get; set; } = 0.4;
    
    [JsonPropertyName("fill")]
    public bool Fill { get; set; } = false;
}

public class ChartJsOptionsDto
{
    [JsonPropertyName("responsive")]
    public bool Responsive { get; set; } = true;
    
    [JsonPropertyName("maintainAspectRatio")]
    public bool MaintainAspectRatio { get; set; } = false;
    
    [JsonPropertyName("plugins")]
    public ChartJsPluginsDto? Plugins { get; set; }
    
    [JsonPropertyName("scales")]
    public ChartJsScalesDto? Scales { get; set; }
    
    [JsonPropertyName("elements")]
    public ChartJsElementsDto? Elements { get; set; }
}

public class ChartJsPluginsDto
{
    [JsonPropertyName("title")]
    public ChartJsTitleDto? Title { get; set; }
    
    [JsonPropertyName("legend")]
    public ChartJsLegendDto? Legend { get; set; }
    
    [JsonPropertyName("datalabels")]
    public ChartJsDataLabelsDto? DataLabels { get; set; }
}

public class ChartJsTitleDto
{
    [JsonPropertyName("display")]
    public bool Display { get; set; } = true;
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class ChartJsLegendDto
{
    [JsonPropertyName("display")]
    public bool Display { get; set; } = true;
    
    [JsonPropertyName("position")]
    public string Position { get; set; } = "top";
}

public class ChartJsDataLabelsDto
{
    [JsonPropertyName("display")]
    public bool Display { get; set; } = true;
    
    [JsonPropertyName("align")]
    public string Align { get; set; } = "end";
    
    [JsonPropertyName("anchor")]
    public string Anchor { get; set; } = "end";
}

public class ChartJsScalesDto
{
    [JsonPropertyName("x")]
    public ChartJsScaleDto? X { get; set; }
    
    [JsonPropertyName("y")]
    public ChartJsScaleDto? Y { get; set; }
}

public class ChartJsScaleDto
{
    [JsonPropertyName("display")]
    public bool Display { get; set; } = true;
    
    [JsonPropertyName("title")]
    public ChartJsScaleTitleDto? Title { get; set; }
    
    [JsonPropertyName("ticks")]
    public ChartJsTicksDto? Ticks { get; set; }
}

public class ChartJsScaleTitleDto
{
    [JsonPropertyName("display")]
    public bool Display { get; set; } = true;
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class ChartJsTicksDto
{
    [JsonPropertyName("beginAtZero")]
    public bool BeginAtZero { get; set; } = true;
    
    [JsonPropertyName("callback")]
    public string? Callback { get; set; }
}

public class ChartJsElementsDto
{
    [JsonPropertyName("bar")]
    public ChartJsBarElementDto? Bar { get; set; }
    
    [JsonPropertyName("point")]
    public ChartJsPointElementDto? Point { get; set; }
}

public class ChartJsBarElementDto
{
    [JsonPropertyName("borderWidth")]
    public double BorderWidth { get; set; } = 0;
    
    [JsonPropertyName("borderRadius")]
    public double BorderRadius { get; set; } = 0;
}

public class ChartJsPointElementDto
{
    [JsonPropertyName("radius")]
    public double Radius { get; set; } = 3;
    
    [JsonPropertyName("hoverRadius")]
    public double HoverRadius { get; set; } = 5;
}
