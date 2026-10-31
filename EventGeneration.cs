using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Collections;

namespace UnlimitedEventExpansion
{
  public partial class ModEntry
  {

    public static bool switched = false;
    public static bool IsMaxedLimit = false;
    public static int totalFailedCheck = 0;
    public static string EventModel = "";
    public static string EventKey = "";
    public static object EventReasoning = new { effort = "minimal" };




    public static async Task<(int, int)> GetOpenAIUsage(string admin_key)
    {
      List<string> premiumModels = new List<string> { "gpt-5.4", "gpt-5.2", "gpt-5.1", "gpt-5.1-codex", "gpt-5", "gpt-5-codex", "gpt-5-chat-latest", "gpt-4.1", "gpt-4o", "o1", "o3" };
      List<string> regularModels = new List<string> { "gpt-5.4-mini", "gpt-5.4-nano", "gpt-5.1-codex-mini", "gpt-5-mini", "gpt-5-nano", "gpt-4.1-mini", "gpt-4.1-nano", "gpt-4o-mini",
                                                            "o1-mini", "o3-mini", "o4-mini", "codex-mini-latest" };

      string usageUrl = "https://api.openai.com/v1/organization/usage/completions";
      using HttpClient client = new HttpClient();

      client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", admin_key);

      DateTime utcNow = DateTime.UtcNow;
      DateTime utcStartOfToday = utcNow.Date;
      long startTime = new DateTimeOffset(utcStartOfToday).ToUnixTimeSeconds();
      long endTime = new DateTimeOffset(utcNow).ToUnixTimeSeconds();

      try
      {
        var perModelTotals = new Dictionary<string, (long Input, long Output)>();
        string? nextPage = null;
        bool hasMore;

        do
        {
          var query = $"start_time={startTime}&end_time={endTime}&group_by=model";
          if (!string.IsNullOrWhiteSpace(nextPage))
            query += $"&page={Uri.EscapeDataString(nextPage)}";

          string requestUrl = $"{usageUrl}?{query}";
          HttpResponseMessage response = await client.GetAsync(requestUrl);

          if (!response.IsSuccessStatusCode)
          {
            string error = await response.Content.ReadAsStringAsync();
            SMonitor.Log($"Error: {response.StatusCode} - {error}", LogLevel.Error);
            return (-1, -1);
          }

          string jsonResponse = await response.Content.ReadAsStringAsync();
          JObject json = JObject.Parse(jsonResponse);

          var data = json["data"] as JArray;
          if (data != null)
          {
            foreach (var bucket in data)
            {
              var results = bucket?["results"] as JArray;
              if (results == null)
                continue;

              foreach (var result in results)
              {
                string? model = result?["model"]?.ToString();
                if (string.IsNullOrWhiteSpace(model))
                  model = "unknown";

                long inputTokens = result?["input_tokens"]?.Value<long>() ?? 0;
                long outputTokens = result?["output_tokens"]?.Value<long>() ?? 0;

                if (perModelTotals.TryGetValue(model, out var totals))
                {
                  perModelTotals[model] = (totals.Input + inputTokens, totals.Output + outputTokens);
                }
                else
                {
                  perModelTotals[model] = (inputTokens, outputTokens);
                }
              }
            }
          }

          hasMore = json["has_more"]?.Value<bool>() ?? false;
          nextPage = json["next_page"]?.ToString();
        }
        while (hasMore && !string.IsNullOrWhiteSpace(nextPage));

        // Aggregate totals for premium and regular model groups
        long premiumInputTotal = 0, premiumOutputTotal = 0;
        long regularInputTotal = 0, regularOutputTotal = 0;

        foreach (var kv in perModelTotals)
        {
          var modelName = kv.Key ?? string.Empty;
          var input = kv.Value.Input;
          var output = kv.Value.Output;

          // Prefer matching regular (specific) names first, then premium.
          bool isRegular = regularModels.Any(rm =>
              modelName.StartsWith(rm, StringComparison.OrdinalIgnoreCase)
              || string.Equals(rm, modelName, StringComparison.OrdinalIgnoreCase)
          );
          bool isPremium = !isRegular && premiumModels.Any(pm =>
              modelName.StartsWith(pm, StringComparison.OrdinalIgnoreCase)
              || string.Equals(pm, modelName, StringComparison.OrdinalIgnoreCase)
          );

          if (isRegular)
          {
            regularInputTotal += input;
            regularOutputTotal += output;
          }
          else if (isPremium)
          {
            premiumInputTotal += input;
            premiumOutputTotal += output;
          }
        }

        // SMonitor.Log($"Premium models: {premiumInputTotal} input tokens, {premiumOutputTotal} output tokens.\nRegular models: {regularInputTotal} input tokens, {regularOutputTotal} output tokens.", LogLevel.Error);
        return ((int)(premiumInputTotal + premiumOutputTotal), (int)(regularInputTotal + regularOutputTotal));
      }
      catch (Exception ex)
      {
        SMonitor.Log($"Request failed: {ex.Message}", LogLevel.Error);
        return (-1, -1);
      }
    }



    private static async Task<string> RequestAiResponseAsync(string instructionPrompt, string userPrompt)
    {
      string provider = Config?.ApiProvider ?? ModConfig.ProviderAuto;

      if (string.Equals(provider, ModConfig.ProviderCustom, StringComparison.OrdinalIgnoreCase))
        return await RequestCustomChatCompletionsResponseAsync(instructionPrompt, userPrompt);

      if (string.Equals(provider, ModConfig.ProviderGemini, StringComparison.OrdinalIgnoreCase))
        return await RequestGeminiResponseAsync(instructionPrompt, userPrompt);

      if (string.Equals(provider, ModConfig.ProviderOpenAI, StringComparison.OrdinalIgnoreCase))
        return await RequestOpenAiResponseAsync(instructionPrompt, userPrompt);

      // Auto preserves the original model-based routing for existing configs.
      return ModConfig.IsGeminiModel(EventModel)
        ? await RequestGeminiResponseAsync(instructionPrompt, userPrompt)
        : await RequestOpenAiResponseAsync(instructionPrompt, userPrompt);
    }

    private static async Task<string> RequestCustomChatCompletionsResponseAsync(string instructionPrompt, string userPrompt)
    {
      string baseUrl = (Config?.CustomApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
      if (string.IsNullOrWhiteSpace(baseUrl))
      {
        SMonitor.Log("Custom API base URL is empty. Set it in Generic Mod Config Menu.", LogLevel.Error);
        return string.Empty;
      }

      string requestUrl = baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
        ? baseUrl
        : baseUrl + "/chat/completions";

      using var httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromSeconds(120);
      httpClient.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", EventKey);

      var requestBody = new Dictionary<string, object>
      {
        { "model", EventModel },
        { "messages", new object[]
          {
            new { role = "system", content = instructionPrompt },
            new { role = "user", content = userPrompt }
          }
        },
        { "max_tokens", 3000 },
        { "response_format", new { type = "json_object" } }
      };

      string jsonRequest = JsonConvert.SerializeObject(requestBody);
      using var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
      using var httpResponse = await httpClient.PostAsync(requestUrl, httpContent);
      string jsonResponse = await httpResponse.Content.ReadAsStringAsync();

      if (!httpResponse.IsSuccessStatusCode)
      {
        SMonitor.Log($"Custom API error {(int)httpResponse.StatusCode}: {jsonResponse}", LogLevel.Error);
        return string.Empty;
      }

      try
      {
        var responseJson = JObject.Parse(jsonResponse);
        JToken? content = responseJson.SelectToken("$.choices[0].message.content");
        string responseMessage = content?.Type == JTokenType.String
          ? content.Value<string>() ?? string.Empty
          : content is JArray parts
            ? string.Join("\\n", parts.Select(part => part["text"]?.ToString()).Where(text => !string.IsNullOrWhiteSpace(text)))
            : string.Empty;

        if (string.IsNullOrWhiteSpace(responseMessage))
        {
          SMonitor.Log("Custom API response did not contain choices[0].message.content.", LogLevel.Error);
          return string.Empty;
        }

        return NormalizeModelResponseText(responseMessage);
      }
      catch (JsonException ex)
      {
        SMonitor.Log($"Error parsing custom Chat Completions response: {ex.Message}", LogLevel.Error);
        return string.Empty;
      }
    }

    private static async Task<string> RequestOpenAiResponseAsync(string instructionPrompt, string userPrompt)
    {
      // SMonitor.Log("\n========================", LogLevel.Error);
      // SMonitor.Log(instructionPrompt, LogLevel.Error);
      // SMonitor.Log("\n========================", LogLevel.Error);
      // SMonitor.Log(userPrompt, LogLevel.Error);

      using var httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", EventKey);

      var requestBody = new Dictionary<string, object>
            {
                { "model", EventModel },
                { "max_output_tokens",  3000 },
                { "input", new object[]
                    {
                        new
                        {
                            role = "developer",
                            content = new object[]
                            {
                                new { type = "input_text", text = instructionPrompt }
                            }
                        },
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "input_text", text = userPrompt }
                            }
                        }
                    }
                },
                { "text", new { format = new { type = "json_object" }, verbosity = "medium" } },
                { "reasoning", EventReasoning }
            };

      var jsonRequest = JsonConvert.SerializeObject(requestBody);
      var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
      var httpResponse = await httpClient.PostAsync("https://api.openai.com/v1/responses", httpContent);
      var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

      if (httpResponse.IsSuccessStatusCode)
      {
        // SMonitor.Log(jsonResponse, LogLevel.Error);

        if (TryExtractOpenAiResponseMessage(jsonResponse, out var responseMessage))
          return NormalizeModelResponseText(responseMessage);

        return string.Empty;
      }

      LogOpenAiError(httpResponse, jsonResponse);
      return string.Empty;
    }

    private static async Task<string> RequestGeminiResponseAsync(string instructionPrompt, string userPrompt)
    {
      using var httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Add("X-goog-api-key", EventKey);

      var requestBody = new Dictionary<string, object>
            {
                {
                    "systemInstruction",
                    new
                    {
                        parts = new object[]
                        {
                            new { text = instructionPrompt }
                        }
                    }
                },
                {
                    "contents",
                    new object[]
                    {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new { text = userPrompt }
                            }
                        }
                    }
                },
                {
                    "generationConfig",
                    new
                    {
                        thinkingConfig = new { thinkingLevel = "MINIMAL" }
                    }
                }
            };

      var jsonRequest = JsonConvert.SerializeObject(requestBody);
      var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
      string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(EventModel)}:generateContent";
      var httpResponse = await httpClient.PostAsync(requestUrl, httpContent);
      var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

      if (httpResponse.IsSuccessStatusCode)
      {
        if (TryExtractGeminiResponseMessage(jsonResponse, out var responseMessage))
          return NormalizeModelResponseText(responseMessage);

        return string.Empty;
      }

      LogGeminiError(httpResponse, jsonResponse);
      return string.Empty;
    }

    private static bool TryExtractOpenAiResponseMessage(string jsonResponse, out string responseMessage)
    {
      responseMessage = string.Empty;

      try
      {
        var responseJson = JObject.Parse(jsonResponse);
        responseMessage = responseJson.Value<string>("output_text") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(responseMessage))
        {
          responseMessage = string.Join("\n", responseJson
              .SelectTokens("$.output[*].content[*]")
              .OfType<JObject>()
              .Where(content => string.Equals(content.Value<string>("type"), "output_text", StringComparison.OrdinalIgnoreCase))
              .Select(content => content.Value<string>("text"))
              .Where(text => !string.IsNullOrWhiteSpace(text)));
        }

        return !string.IsNullOrWhiteSpace(responseMessage);
      }
      catch (JsonException ex)
      {
        SMonitor.Log($"Error parsing OpenAI response: {ex.Message}", LogLevel.Error);
        return false;
      }
    }

    private static bool TryExtractGeminiResponseMessage(string jsonResponse, out string responseMessage)
    {
      responseMessage = string.Empty;

      try
      {
        var responseJson = JObject.Parse(jsonResponse);
        responseMessage = string.Join("\n", responseJson
            .SelectTokens("$.candidates[*].content.parts[*].text")
            .Select(token => token?.ToString())
            .Where(text => !string.IsNullOrWhiteSpace(text)));

        return !string.IsNullOrWhiteSpace(responseMessage);
      }
      catch (JsonException ex)
      {
        SMonitor.Log($"Error parsing Gemini response: {ex.Message}", LogLevel.Error);
        return false;
      }
    }

    private static string NormalizeModelResponseText(string responseMessage)
    {
      if (string.IsNullOrWhiteSpace(responseMessage))
        return string.Empty;

      string trimmed = responseMessage.Trim();

      if (trimmed.StartsWith("```", StringComparison.Ordinal))
      {
        int firstNewLine = trimmed.IndexOf('\n');
        if (firstNewLine >= 0)
          trimmed = trimmed.Substring(firstNewLine + 1);

        if (trimmed.EndsWith("```", StringComparison.Ordinal))
          trimmed = trimmed.Substring(0, trimmed.Length - 3);

        trimmed = trimmed.Trim();
      }

      int objectStart = trimmed.IndexOf('{');
      int objectEnd = trimmed.LastIndexOf('}');
      if (objectStart >= 0 && objectEnd > objectStart)
        return trimmed.Substring(objectStart, objectEnd - objectStart + 1).Trim();

      return trimmed;
    }

    private static JToken ConvertToJToken(string responseMessage)
    {
      try
      {
        JToken parsed = JToken.Parse(responseMessage);
        return parsed;
      }
      catch (JsonReaderException)
      {
        SMonitor.Log("Error: Assistant returned invalid JSON", LogLevel.Error);
      }
      return null;
    }

    private static void LogOpenAiError(HttpResponseMessage httpResponse, string responseBody)
    {
      var statusCode = (int)httpResponse.StatusCode;
      string errorMessage = "Check for mod update";

      switch (statusCode)
      {
        case 403:
          errorMessage = "Country, region, or territory not supported.";
          break;
        case 429:
          errorMessage = "Please try again in a few minutes. If not work, then total AI usage for all players has passed the limit set by OpenAI. This will be reset the next day in timezone UTC+0";
          break;
        case 500:
          errorMessage = "Server Error: The server had an issue while processing your request. Please try again.";
          break;
        case 503:
          errorMessage = "Server Overload: The server is experiencing high traffic. Please try again later.";
          break;
      }

      SMonitor.Log($"OpenAI error {statusCode}: {responseBody}", LogLevel.Error);
      SMonitor.Log($"Unable to receive AI content. {errorMessage}\n\n", LogLevel.Error);
    }

    private static void LogGeminiError(HttpResponseMessage httpResponse, string responseBody)
    {
      var statusCode = (int)httpResponse.StatusCode;
      string errorMessage = "Check Gemini API key and selected model.";

      switch (statusCode)
      {
        case 400:
          errorMessage = "Bad request. Please check the selected Gemini model and payload format.";
          break;
        case 403:
          errorMessage = "Access denied. Please verify your Gemini API key permissions.";
          break;
        case 429:
          errorMessage = "Rate limit exceeded. Please try again in a few minutes.";
          break;
        case 500:
          errorMessage = "Server error while processing Gemini request. Please try again.";
          break;
        case 503:
          errorMessage = "Gemini service is temporarily unavailable. Please try again later.";
          break;
      }

      SMonitor.Log($"Gemini error {statusCode}: {responseBody}", LogLevel.Error);
      SMonitor.Log($"Unable to receive AI content. {errorMessage}\n\n", LogLevel.Error);
    }


    private static string GetNpcCharacteristicForPrompt(string npcName, bool minimal = false)
    {
      if (string.IsNullOrWhiteSpace(npcName) || Game1.getCharacterFromName(npcName) is not NPC npc || npc is null)
        return string.Empty;

      string npcAge = npc.Age == 0 ? "adult" : npc.Age == 1 ? "teens" : npc.Age == 2 ? "child" : "adult";
      string npcManner = npc.Manners == 0 ? "a typical neutral manner" : npc.Manners == 1 ? "a polite and courteous manner" : npc.Manners == 2 ? "a distant and reserved manner" : "a typical neutral manner";
      string npcSocial = npc.SocialAnxiety == 0 ? "an outgoing person" : npc.SocialAnxiety == 1 ? "a little shy person" : "neither too outgoing nor shy";

      string npcCharacteristic = $" {npc.Name} is {npcAge}, {npcManner}, and is {npcSocial}";

      // CUSTOM CHARACTERISTIC OVERRIDE
      if (!string.IsNullOrWhiteSpace(Config.Key))
      {
        if (Config.CharacteristicMode == ModConfig.CharacteristicModeLong && NpcCharacteristicsLong.TryGetValue(npc.Name, out string? customCharacteristicLong) && !string.IsNullOrWhiteSpace(customCharacteristicLong))
        {
          npcCharacteristic = customCharacteristicLong;
        }
        else if (Config.CharacteristicMode == ModConfig.CharacteristicModeShort && NpcCharacteristicsShort.TryGetValue(npc.Name, out string? customCharacteristic) && !string.IsNullOrWhiteSpace(customCharacteristic))
        {
          npcCharacteristic = customCharacteristic;
        }
        else if (Config.CharacteristicMode == ModConfig.CharacteristicModeMinimal && NpcCharacteristicsMinimal.TryGetValue(npc.Name, out string? customCharacteristicMinimal) && !string.IsNullOrWhiteSpace(customCharacteristicMinimal))
        {
          npcCharacteristic = customCharacteristicMinimal;
        }
        return npcCharacteristic.Trim();
      }
      // DEFAULT CHARACTERISTIC
      else
      {
        if (NpcCharacteristicsShort.TryGetValue(npc.Name, out string? customCharacteristic) && !string.IsNullOrWhiteSpace(customCharacteristic) && !minimal)
        {
          npcCharacteristic = customCharacteristic;
          if (npcCharacteristic.Length > 800)
            npcCharacteristic = npcCharacteristic.Substring(0, 800);
        }
        else if (NpcCharacteristicsMinimal.TryGetValue(npc.Name, out string? customCharacteristicMinimal) && !string.IsNullOrWhiteSpace(customCharacteristicMinimal) && minimal)
        {
          npcCharacteristic = customCharacteristicMinimal;
          if (npcCharacteristic.Length > 400)
            npcCharacteristic = npcCharacteristic.Substring(0, 400);
        }

        return npcCharacteristic.Trim();
      }
    }


    private static string GetPreferedEventLength()
    {
      if (string.IsNullOrWhiteSpace(Config.Key))
        return "Keep the total conversation under 8-10 exchanges, with each dialogue under 30 words.";

      switch (Config.EventLength)
      {
        case ModConfig.EventLengthShort:
          return "Keep the total conversation under 8-10 exchanges, with each dialogue under 30 words.";
        case ModConfig.EventLengthMedium:
          return "Keep the total conversation under 10-12 exchanges, with each dialogue under 35 words.";
        case ModConfig.EventLengthLong:
          return "Keep the total conversation under 12-15 exchanges, with each dialogue under 40 words.";
        case ModConfig.EventLengthExtraLong:
          return "Keep the total conversation under 15-20 exchanges, with each dialogue under 40 words.";
        default:
          return "Keep the total conversation under 8-10 exchanges, with each dialogue under 30 words.";
      }
    }

    private static string AppendLanguageInstruction(string systemInstruction)
    {
      if (string.IsNullOrWhiteSpace(systemInstruction))
        return string.Empty;

      string selectedLanguage = Config?.Language?.Trim() ?? ModConfig.LanguageEnglish;
      if (string.IsNullOrWhiteSpace(selectedLanguage)
          || string.Equals(selectedLanguage, ModConfig.LanguageEnglish, StringComparison.OrdinalIgnoreCase))
      {
        return systemInstruction;
      }

      return $"{systemInstruction}\nUse {selectedLanguage} language and alphabet.";
    }


    // -------------------------------------------------------------------------------------------------
    // END OF EVENT GENERATION
    // -------------------------------------------------------------------------------------------------

  }
}
