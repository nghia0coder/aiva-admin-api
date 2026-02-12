using System.Text;
using System.Text.RegularExpressions;
using Aiva.Admin.Api.Core.Interfaces;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class DataFormatterService : IDataFormatterService
{
    public string FormatAsMarkdownTable(List<Dictionary<string, object>> data)
    {
        if (data == null || data.Count == 0) 
            return "*Không có dữ liệu trả về.*";

        var sb = new StringBuilder();
        var headers = data[0].Keys.ToList();

        sb.Append("| ").Append(string.Join(" | ", headers)).Append(" |\n");
        sb.Append("| ").Append(string.Join(" | ", headers.Select(_ => "---"))).Append(" |\n");

        foreach (var row in data.Take(20))
        {
            sb.Append("| ").Append(string.Join(" | ", headers.Select(h => row[h]?.ToString() ?? ""))).Append(" |\n");
        }

        if (data.Count > 20) 
            sb.Append($"\n*(Hiển thị 20/{data.Count} dòng dữ liệu)*");

        return sb.ToString();
    }

    public string? ExtractSql(string text)
    {
        var pattern = @"```sql\s+(.*?)\s+```";
        var match = Regex.Match(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
