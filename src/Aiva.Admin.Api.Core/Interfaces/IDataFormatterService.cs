namespace Aiva.Admin.Api.Core.Interfaces;

public interface IDataFormatterService
{
    string FormatAsMarkdownTable(List<Dictionary<string, object>> data);
    string? ExtractSql(string text);
}
