using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Core;



public class AiAnalyzer(HttpClient httpClient) : IAiAnalyzer
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public Task<List<AiQna>> AnalyzeServiceAsync(DetailedWindowsServiceInfo serviceInfo, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(serviceInfo);

        var prompt = $"Please analyze the following Windows Service. Answer the following 4 questions:\n" +
                     $"1. What does this service do?\n" +
                     $"2. Is it critical for the system?\n" +
                     $"3. Is it safe to disable?\n" +
                     $"4. Is it known to use too much resource?\n\n" +
                     $"Return the response STRICTLY as a JSON array of objects, where each object has a 'Question' and 'Answer' property. " +
                     $"Do NOT use any markdown formatting (no bold, no asterisks, no code blocks). Keep the answers short and concise.\n\n" +
                     $"Service Name: {serviceInfo.ServiceName}\n" +
                     $"Executable Path: {serviceInfo.ExecutablePath}\n" +
                     $"Executable Name: {serviceInfo.ExecutableName}\n" +
                     $"Publisher: {serviceInfo.Publisher}\n" +
                     $"Is Signed: {serviceInfo.IsSigned}\n" +
                     $"Service Account: {serviceInfo.ServiceAccount}\n" +
                     $"Dependencies: {(serviceInfo.Dependencies.Length > 0 ? string.Join(", ", serviceInfo.Dependencies) : "None")}\n";

        return AskGeminiAsync(prompt, settings);
    }

    public Task<List<AiQna>> AnalyzeTaskAsync(WindowsTaskInfo task, DetailedWindowsTaskInfo detailedInfo, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(detailedInfo);

        var prompt = $"Please analyze the following Windows Scheduled Task. Answer the following 3 questions:\n" +
                     $"1. What does this scheduled task do?\n" +
                     $"2. Is it critical for system stability?\n" +
                     $"3. Is it safe to disable?\n\n" +
                     $"Return the response STRICTLY as a JSON array of objects, where each object has a 'Question' and 'Answer' property. " +
                     $"Do NOT use any markdown formatting (no bold, no asterisks, no code blocks). Keep the answers short and concise.\n\n" +
                     $"Task Name: {task.TaskName}\n" +
                     $"Task Path: {task.TaskPath}\n" +
                     $"Description: {task.Description}\n" +
                     $"Author: {task.Author}\n" +
                     $"Actions: {(detailedInfo.Actions.Length > 0 ? string.Join(", ", detailedInfo.Actions) : "None")}\n" +
                     $"Triggers: {(detailedInfo.Triggers.Length > 0 ? string.Join(", ", detailedInfo.Triggers) : "None")}\n";

        return AskGeminiAsync(prompt, settings);
    }

    private async Task<List<AiQna>> AskGeminiAsync(string prompt, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        
        if (string.IsNullOrWhiteSpace(settings.GeminiApiKey))
        {
            throw new InvalidOperationException("API Key is missing. Please configure your Gemini API Key in the settings.");
        }

        var requestBody = new GeminiRequest
        {
            Contents = new List<GeminiContent>
            {
                new GeminiContent
                {
                    Parts = new List<GeminiPart>
                    {
                        new GeminiPart { Text = prompt }
                    }
                }
            }
        };

        var modelName = string.IsNullOrWhiteSpace(settings.GeminiModel) 
            ? "gemini-1.5-flash" 
            : settings.GeminiModel;

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={settings.GeminiApiKey}";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, AiJsonContext.Default.GeminiRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Error from AI service: {response.StatusCode} - {response.ReasonPhrase}\n{error}");
            }
            
            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonResponse);
            
            var text = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("No analysis generated by the AI.");
            }

            text = text.Trim();
            if (text.StartsWith("```json"))
            {
                text = text[7..];
                if (text.EndsWith("```"))
                {
                    text = text[..^3];
                }
            }
            else if (text.StartsWith("```"))
            {
                text = text[3..];
                if (text.EndsWith("```"))
                {
                    text = text[..^3];
                }
            }

            var result = JsonSerializer.Deserialize(text, AiJsonContext.Default.ListAiQna);

            return result ?? throw new InvalidOperationException("Failed to parse AI response.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to analyze: {ex.Message}", ex);
        }
    }
}
