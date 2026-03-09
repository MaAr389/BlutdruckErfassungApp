using System.Text.Json;

namespace BlutdruckErfassungApp.Services;

public sealed class AzureDocumentIntelligenceOcrService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IOcrService
{
    public async Task<string> ExtractTextAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["AzureDocumentIntelligence:Endpoint"]?.TrimEnd('/');
        var apiKey = configuration["AzureDocumentIntelligence:ApiKey"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Azure Document Intelligence ist nicht konfiguriert. Bitte AzureDocumentIntelligence:Endpoint und AzureDocumentIntelligence:ApiKey setzen (ideal via Key Vault)."
            );
        }

        var pollIntervalMs = configuration.GetValue("AzureDocumentIntelligence:PollIntervalMs", 1200);
        var maxPollAttempts = configuration.GetValue("AzureDocumentIntelligence:MaxPollAttempts", 20);

        var client = httpClientFactory.CreateClient();

        using var analyzeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{endpoint}/documentintelligence/documentModels/prebuilt-read:analyze?api-version=2024-11-30");

        analyzeRequest.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
        analyzeRequest.Content = new StreamContent(imageStream);
        analyzeRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        using var analyzeResponse = await client.SendAsync(analyzeRequest, cancellationToken);
        analyzeResponse.EnsureSuccessStatusCode();

        if (!analyzeResponse.Headers.TryGetValues("operation-location", out var operationHeaders))
        {
            throw new InvalidOperationException("Document Intelligence hat keine operation-location zurückgegeben.");
        }

        var operationLocation = operationHeaders.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(operationLocation))
        {
            throw new InvalidOperationException("Die operation-location von Document Intelligence ist leer.");
        }

        for (var attempt = 0; attempt < maxPollAttempts; attempt++)
        {
            using var pollRequest = new HttpRequestMessage(HttpMethod.Get, operationLocation);
            pollRequest.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

            using var pollResponse = await client.SendAsync(pollRequest, cancellationToken);
            pollResponse.EnsureSuccessStatusCode();

            await using var responseStream = await pollResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            var status = document.RootElement.GetProperty("status").GetString();

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Document Intelligence konnte den Text nicht extrahieren.");
            }

            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var root = document.RootElement;
                if (!root.TryGetProperty("analyzeResult", out var analyzeResult))
                {
                    return string.Empty;
                }

                if (analyzeResult.TryGetProperty("pages", out var pages))
                {
                    var lines = pages
                        .EnumerateArray()
                        .SelectMany(page => page.GetProperty("lines").EnumerateArray())
                        .Select(line => line.GetProperty("content").GetString())
                        .Where(line => !string.IsNullOrWhiteSpace(line));

                    return string.Join(Environment.NewLine, lines!);
                }

                if (analyzeResult.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? string.Empty;
                }

                return string.Empty;
            }

            await Task.Delay(pollIntervalMs, cancellationToken);
        }

        throw new TimeoutException("Document Intelligence hat zu lange gebraucht. Bitte erneut versuchen.");
    }
}
