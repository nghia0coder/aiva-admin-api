using System.Text.Json;
using System.Text.Json.Serialization;
using Aiva.Admin.Api.Core.ConversationAggregate.Charts;
using Aiva.Admin.Api.Core.ConversationAggregate.Constants;
using Aiva.Admin.Api.Core.Interfaces;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class ChartJsGenerationService : IChartGenerationService
{
    private static readonly List<string> DefaultColors = new()
    {
        "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF",
        "#FF9F40", "#FF6384", "#C9CBCF", "#4BC0C0", "#FF6384"
    };

    public string GenerateChartConfig(List<Dictionary<string, object>> data, string xAxisColumn, string yAxisColumn, string chartType)
    {
        if (data == null || data.Count == 0) return "{}";

        // Convert Highcharts type to Chart.js type
        var chartJsType = ChartTypes.ConvertToChartJs(chartType);
        
        var chartConfig = new ChartJsConfigDto
        {
            Type = chartJsType,
            Data = CreateChartData(data, xAxisColumn, yAxisColumn, chartJsType),
            Options = CreateChartOptions(chartJsType)
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var configChart = JsonSerializer.Serialize(chartConfig, options);
        
        // Handle callback functions for Chart.js
        configChart = configChart.Replace("\"function(", "function(", StringComparison.Ordinal);
        configChart = configChart.Replace("}\"", "}", StringComparison.Ordinal);
        
        return configChart;
    }

    private ChartJsDataDto CreateChartData(List<Dictionary<string, object>> data, string xAxisColumn, string yAxisColumn, string chartType)
    {
        var chartData = new ChartJsDataDto
        {
            Labels = new List<string>(),
            Datasets = new List<ChartJsDatasetDto>()
        };

        var dataset = new ChartJsDatasetDto
        {
            Label = yAxisColumn,
            Data = new List<object>(),
            BackgroundColor = GetBackgroundColors(chartType, data.Count),
            BorderColor = GetBorderColors(chartType, data.Count),
            BorderWidth = chartType == "line" ? 2 : 1,
            Tension = GetTension(chartType),
            Fill = GetFill(chartType)
        };

        foreach (var row in data)
        {
            if (!row.ContainsKey(xAxisColumn) || !row.ContainsKey(yAxisColumn)) continue;

            var label = row[xAxisColumn]?.ToString() ?? "";
            var value = row[yAxisColumn]?.ToString() ?? "0";
            decimal number = GetNumberFromString(value);

            if (chartType == "pie" || chartType == "doughnut" || chartType == "polarArea")
            {
                // For pie-like charts, labels are included in data
                chartData.Labels.Add(label);
                dataset.Data.Add(number);
            }
            else
            {
                // For other charts
                chartData.Labels.Add(label);
                dataset.Data.Add(number);
            }
        }

        chartData.Datasets.Add(dataset);
        return chartData;
    }

    private ChartJsOptionsDto CreateChartOptions(string chartType)
    {
        var options = new ChartJsOptionsDto
        {
            Responsive = true,
            MaintainAspectRatio = false,
            Plugins = new ChartJsPluginsDto
            {
                Title = new ChartJsTitleDto
                {
                    Display = false,
                    Text = ""
                },
                Legend = new ChartJsLegendDto
                {
                    Display = chartType == "pie" || chartType == "doughnut" || chartType == "polarArea",
                    Position = "top"
                },
                DataLabels = new ChartJsDataLabelsDto
                {
                    Display = true,
                    Align = "end",
                    Anchor = "end"
                }
            }
        };

        // Add scales for non-pie charts
        if (chartType != "pie" && chartType != "doughnut" && chartType != "polarArea" && chartType != "radar")
        {
            options.Scales = new ChartJsScalesDto
            {
                X = new ChartJsScaleDto
                {
                    Display = true,
                    Title = new ChartJsScaleTitleDto
                    {
                        Display = false,
                        Text = ""
                    }
                },
                Y = new ChartJsScaleDto
                {
                    Display = true,
                    Title = new ChartJsScaleTitleDto
                    {
                        Display = false,
                        Text = ""
                    },
                    Ticks = new ChartJsTicksDto
                    {
                        BeginAtZero = true
                    }
                }
            };
        }

        // Add elements configuration for bar charts
        if (chartType == "bar")
        {
            options.Elements = new ChartJsElementsDto
            {
                Bar = new ChartJsBarElementDto
                {
                    BorderWidth = 0,
                    BorderRadius = 0
                }
            };
        }

        return options;
    }

    private object GetBackgroundColors(string chartType, int dataCount)
    {
        if (chartType == "pie" || chartType == "doughnut" || chartType == "polarArea")
        {
            // Return array of colors for pie charts
            return DefaultColors.Take(Math.Min(dataCount, DefaultColors.Count)).ToList();
        }
        
        // Return single color for other chart types
        return DefaultColors[0];
    }

    private object GetBorderColors(string chartType, int dataCount)
    {
        if (chartType == "pie" || chartType == "doughnut" || chartType == "polarArea")
        {
            // Return array of colors for pie charts
            return DefaultColors.Take(Math.Min(dataCount, DefaultColors.Count)).ToList();
        }
        
        // Return single color for other chart types
        return DefaultColors[0];
    }

    private double GetTension(string chartType)
    {
        // Add tension for smooth lines (equivalent to Highcharts spline)
        return chartType == "line" ? 0.4 : 0;
    }

    private bool GetFill(string chartType)
    {
        // Fill area for area charts
        return false; // You can modify this based on your needs
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
