using Aiva.Admin.Api.Core.ConversationAggregate.Charts;

namespace Aiva.Admin.Api.Core.Interfaces;

public interface IChartGenerationService
{
    string GenerateChartConfig(List<Dictionary<string, object>> data, string xAxisColumn, string yAxisColumn, string chartType);
    decimal GetNumberFromString(string value);
}
