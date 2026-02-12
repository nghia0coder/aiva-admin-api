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
        var formattedHeaders = headers.Select(FormatColumnName).ToList();

        sb.Append("| ").Append(string.Join(" | ", formattedHeaders)).Append(" |\n");
        sb.Append("| ").Append(string.Join(" | ", formattedHeaders.Select(_ => "---"))).Append(" |\n");

        foreach (var row in data.Take(20))
        {
            sb.Append("| ").Append(string.Join(" | ", headers.Select(h => row[h]?.ToString() ?? ""))).Append(" |\n");
        }

        if (data.Count > 20) 
            sb.Append($"\n*(Hiển thị 20/{data.Count} dòng dữ liệu)*");

        return sb.ToString();
    }

    /// <summary>
    /// Format column names by adding spaces between words
    /// Example: TenSanPham -> Tên Sản Phẩm, ProductName -> Product Name
    /// </summary>
    private static string FormatColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return columnName;

        // Add space before uppercase letters (for camelCase/PascalCase)
        // TenSanPham -> Ten San Pham
        // ProductName -> Product Name
        var formatted = Regex.Replace(columnName, "([a-z])([A-Z])", "$1 $2");
        
        // Handle consecutive uppercase letters (e.g., URLPath -> URL Path)
        formatted = Regex.Replace(formatted, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        
        // Replace underscores and hyphens with spaces
        formatted = formatted.Replace("_", " ").Replace("-", " ");
        
        // Remove multiple consecutive spaces
        formatted = Regex.Replace(formatted, @"\s+", " ");
        
        return formatted.Trim();
    }

    public string? ExtractSql(string text)
    {
        var pattern = @"```sql\s+(.*?)\s+```";
        var match = Regex.Match(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
