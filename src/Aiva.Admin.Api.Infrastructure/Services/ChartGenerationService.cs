using System.Text.Json;
using System.Text.Json.Serialization;
using Aiva.Admin.Api.Core.ConversationAggregate.Charts;
using Aiva.Admin.Api.Core.ConversationAggregate.Constants;
using Aiva.Admin.Api.Core.Interfaces;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class ChartGenerationService : IChartGenerationService
{
    public string GenerateChartConfig(List<Dictionary<string, object>> data, string xAxisColumn, string yAxisColumn, string chartType)
    {
        if (data == null || data.Count == 0) return "{}";

        var chart = new HighchartConfigDto
        {
            Chart = new HighchartChartDto { Type = chartType, BackgroundColor = null },
            Title = new HighchartTitleDto { Text = "" },
            XAxis = new HighchartXAxisDto { Categories = new List<string>() },
            YAxis = new HighchartYAxisDto
            {
                Title = new HighchartYAxisTitleDto { Text = "" }
            },
            PlotOptions = new HighchartPlotOptionsDto 
            { 
                Column = new HighchartPlotOptionsColumnDto { BorderWidth = 0, PointPadding = 0.2 } 
            },
            Legend = new HighchartLegendDto { Enabled = false },
            Series = new List<HighchartSeriesDto>()
        };

        var chartSeries = new HighchartSeriesDto
        {
            Name = "",
            DataLabels = new HighchartSeriesDataLabelsDto { Enabled = true },
            Data = new List<object>()
        };

        foreach (var row in data)
        {
            if (!row.ContainsKey(xAxisColumn) || !row.ContainsKey(yAxisColumn)) continue;

            var label = row[xAxisColumn]?.ToString() ?? "";
            var value = row[yAxisColumn]?.ToString() ?? "0";
            decimal number = GetNumberFromString(value);

            if (chartType == ChartTypes.Pie)
            {
                chartSeries.Data.Add(new
                {
                    y = number,
                    name = label
                });
            }
            else
            {
                chartSeries.Data.Add(number);
                chart.XAxis.Categories.Add(label);
            }
        }

        chart.Series.Add(chartSeries);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var configChart = JsonSerializer.Serialize(chart, options);
        configChart = configChart.Replace("\"function()", "function()", StringComparison.Ordinal);
        configChart = configChart.Replace("}\"", "}", StringComparison.Ordinal);
        return configChart;
    }

    public decimal GetNumberFromString(string value)
    {
        try
        {
            var cleanValue = value.Replace(",", "").Replace("%", "");
            decimal number = decimal.Parse(cleanValue.Trim());
            return Math.Round(number, 2);
        }
        catch
        {
            return 0;
        }
    }
}
