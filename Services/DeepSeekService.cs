using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Yorvis.Services
{
    public class DeepSeekService
    {
        private readonly HttpClient _http;
        private static readonly string ApiBase = "https://api.deepseek.com";

        public DeepSeekService()
        {
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> Chat(string apiKey, string model, string systemPrompt, string userMessage)
        {
            var requestBody = new
            {
                model = model ?? "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.7,
                max_tokens = 4096
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = content;

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var choice = doc.RootElement.GetProperty("choices")[0];
            var messageContent = choice.GetProperty("message").GetProperty("content").GetString();

            return messageContent ?? "No response from AI.";
        }

        public static string BuildSystemPrompt(Models.Project[] projects, string processName, string windowTitle, string category)
        {
            var sb = new StringBuilder();
            sb.Append("You are Yorvis AI, a productivity assistant embedded in the user's PC activity tracker. ");
            sb.Append("Respond in the same language the user writes to you. Be concise, practical, and actionable. ");
            sb.Append("Never use emojis. Never use em dashes (—); use a regular hyphen (-) instead.\n\n");

            sb.Append("## Current Context\n");
            sb.Append($"- Active Process: {processName ?? "Unknown"}\n");
            sb.Append($"- Active Window: {windowTitle ?? "None"}\n");
            sb.Append($"- Category: {category ?? "Uncategorized"}\n");
            sb.Append($"- Time: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n");

            if (projects != null && projects.Length > 0)
            {
                sb.Append("## User's Projects\n");
                foreach (var p in projects)
                {
                    sb.Append($"- {p.Name}");
                    if (!string.IsNullOrWhiteSpace(p.Url)) sb.Append($" ({p.Url})");
                    if (!string.IsNullOrWhiteSpace(p.Keywords)) sb.Append($" | Keywords: {p.Keywords}");
                    if (!string.IsNullOrWhiteSpace(p.Notes)) sb.Append($" | Notes: {p.Notes}");
                    sb.Append('\n');
                }
            }

            sb.Append("\nWhen the user asks you to write an article, blog post, or content, ");
            sb.Append("use proper heading hierarchy: a single H1 (#) for the main title, ");
            sb.Append("then at least 2-3 H2 headings (##) for main sections, ");
            sb.Append("and only use H3 (###) within those sections if needed. ");
            sb.Append("Never use only one H2. Include at least one bullet or numbered list and paragraphs. ");
            sb.Append("The content must be ready to paste into a content editor after Markdown-to-HTML conversion.\n\n");
            sb.Append("When the user asks about SEO, products, or general questions, provide structured markdown as needed.\n\n");
            sb.Append("If the user reports completing a task, acknowledge it and suggest the next logical step.\n\n");
            sb.Append("If you suggest specific actionable tasks the user can do right now (e.g. \"Write a page\", \"Generate keywords\", \"Check rankings\"), ");
            sb.Append("include them at the very end of your response in this exact JSON format on its own line:\n");
            sb.Append("🔹ACTIONS: [{\"label\":\"Write Page\",\"prompt\":\"Write content about X\"},{\"label\":\"SEO Desc\",\"prompt\":\"Write meta description for X\"}]\n");
            sb.Append("Keep labels short (2-3 words max). The prompt should be a brief instruction. Include 1-3 actions maximum.");

            return sb.ToString();
        }
    }
}
