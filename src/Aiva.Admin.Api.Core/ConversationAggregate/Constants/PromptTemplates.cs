namespace Aiva.Admin.Api.Core.ConversationAggregate.Constants;

public static class PromptTemplates
{
    public const string MessageTemplateForGenerateQuestion = @"Based on the <new_question> and the <chat_history>, your task is to generate a standalone question. You ALWAYS follow the guidelines below to do your tasks:
      <guidelines_for_standalone>
      - If the <new_question> is not related to the <chat_history>, return the <new_question> as the standalone question. Just generate the standalone question without commentary.
      - Analyze to see if the <new_question> is related to the <chat_history>, if so create a standalone question that fully covers the content and context of both the <chat_history> and the <new_question>. If the <new_question> is not related to the <chat_history>, simply use the content of the <new_question> to create the standalone question without relying on the <chat_history>.
      - Do not translate the English words and terms mentioned in the <new_question>; If you are not sure, choose Vietnamese as default language.
      - If the <chat_history> is empty, it means that the <new_question> is the first question in the conversation so you must return exactly the <new_question> as your answer.
      - Do not answer the <new_question> in the standalone question.
      - When the <new_question> refers to something mentioned ""above"", ""this"", or ""that"", it means they are referring to information previously discussed in the <chat_history>; so you need to combine the <new_question> and the <chat_history>.
      </guidelines_for_standalone>

      <chat_history>
      @{chat_history}
      </chat_history>

      <new_question>
      @{chat_input}
      </new_question>";

    public const string SystemTemplateCognitiveOutput = @"You are a data analysis assistant that helps users understand their data through both textual summaries and appropriate visualizations.

          Given the question, SQL query and dataset, provide a JSON response with:

          - ""answer"": A clear answer in the question's language (max 100 words)
          - ""chart_type"": Visualization recommendation based on these rules:
            * Return null ONLY if: dataset is empty OR has only 1 row with 1 value
            * For 2+ numeric columns: suggest ""table"", ""bar"", ""column"", ""line"", or ""pie""
            * For time-based data: prefer ""line""
            * For categories with values: prefer ""bar"" or ""column""
            * For proportions/percentages: prefer ""pie""
          - ""chart_x_axis"": Column name best suited for x-axis (usually categorical or time)
          - ""chart_y_axis"": Column name best suited for y-axis (usually numeric values)
          - ""chart_series"": Additional grouping column if applicable, null otherwise

          IMPORTANT: Even for summary data (totals, averages), if the underlying dataset has multiple rows/categories, suggest appropriate visualization.

          Example: Revenue summary with multiple time periods or categories should suggest charts to show trends/breakdowns.

          Return only valid JSON format.";

    public const string MessageTemplateCognitiveOutput = @"Question: @{standalone_question}
                                        SQL Query: @{sql_query}
                                        Dataset Info:
                                        - Row count: @{row_count}
                                        - Columns: @{column_names}
                                        - Data preview: @{result_data}
                                        Current datetime: @{system_time}

                                        Analyze if this data would benefit from visualization beyond the text summary.";
}
