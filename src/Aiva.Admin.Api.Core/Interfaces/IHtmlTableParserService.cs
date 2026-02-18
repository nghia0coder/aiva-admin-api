using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

namespace Aiva.Admin.Api.Core.Interfaces;

public interface IHtmlTableParserService
{
  /// <summary>
  /// Parses HTML table from additionalUserData and extracts product selection data
  /// </summary>
  /// <param name="htmlTable">HTML table string from frontend</param>
  /// <returns>Structured product selection data</returns>
  ProductSelectionData ParseProductTable(string htmlTable);
}
