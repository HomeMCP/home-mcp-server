using System.Text;
using HomeMcp.Domain.Memory;
using HomeMcp.Domain.Plugins;
using HomeMcp.Domain.Sessions;
using HomeMcp.Domain.SharedKernel;

namespace HomeMcp.Application.Orchestration;

public sealed class PromptBuilder
{
    public string Build(
        Session session,
        IReadOnlyList<IPlugin> activePlugins,
        IReadOnlyList<Fact> relevantFacts,
        string locale)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GetBaseSystemPrompt(locale));

        foreach (var plugin in activePlugins)
        {
            var fragment = plugin.GetWorldContextFragment(session.UserId.Value, locale);
            if (!string.IsNullOrWhiteSpace(fragment))
            {
                sb.AppendLine();
                sb.AppendLine(fragment);
            }
        }

        if (relevantFacts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## User facts:");
            foreach (var fact in relevantFacts)
            {
                sb.AppendLine($"- [{fact.Scope}] {fact.Key}: {fact.Value}");
            }
        }

        if (session.Summary.HasValue)
        {
            sb.AppendLine();
            sb.AppendLine("## Earlier in this conversation:");
            sb.AppendLine(session.Summary.Value);
        }

        return sb.ToString().TrimEnd();
    }

    private static readonly IReadOnlyDictionary<string, string> BaseSystemPrompts =
        new Dictionary<string, string>
        {
            ["en"] = "You are a helpful home assistant. Be concise and to the point. Use available tools when needed.",
            ["ru"] = "Ты полезный домашний ассистент. Отвечай кратко и по делу. Используй доступные инструменты когда нужно.",
        };

    private static string GetBaseSystemPrompt(string locale)
    {
        var code = Locale.ToLanguageCode(locale);
        return BaseSystemPrompts.TryGetValue(code, out var prompt) ? prompt : BaseSystemPrompts["en"];
    }
}
