using System.Text;
using System.Text.Json;

namespace backend.Services;

public sealed class OverpassBarbershopCatalog : IExternalBarbershopCatalog
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OverpassBarbershopCatalog> _logger;
    private readonly string _interpreterPath;
    private readonly int _timeoutSeconds;

    public OverpassBarbershopCatalog(HttpClient httpClient, IConfiguration configuration,
        ILogger<OverpassBarbershopCatalog> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _interpreterPath = configuration["Overpass:InterpreterPath"] ?? "/api/interpreter";
        _timeoutSeconds = int.TryParse(configuration["Overpass:TimeoutSeconds"], out var value) ? value : 25;
    }

    public async Task<IReadOnlyList<ExternalPlaceCandidate>> SearchPlacesInCityAsync(string cityDisplayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityDisplayName))
            return Array.Empty<ExternalPlaceCandidate>();

        var query = BuildOverpassQuery(cityDisplayName, _timeoutSeconds);
        using var request = new HttpRequestMessage(HttpMethod.Post, _interpreterPath)
        {
            Content = new StringContent(query, Encoding.UTF8, "text/plain"),
        };

        request.Headers.UserAgent.ParseAdd("BarbershopBookingSystem/1.0 (Overpass import)");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Overpass request failed for {City}. Status: {Status}. Body: {Body}",
                cityDisplayName, response.StatusCode, body);
            return Array.Empty<ExternalPlaceCandidate>();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var results = new List<ExternalPlaceCandidate>();
        var dedup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!document.RootElement.TryGetProperty("elements", out var elements) ||
            elements.ValueKind != JsonValueKind.Array)
            return results;

        foreach (var element in elements.EnumerateArray())
        {
            var type = element.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
            var id = element.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
            if (string.IsNullOrWhiteSpace(type) || id <= 0)
                continue;

            var externalId = $"osm:{type}:{id}";
            if (!dedup.Add(externalId))
                continue;

            var tags = element.TryGetProperty("tags", out var tagsProp) ? tagsProp : default;
            var name = TryGetTag(tags, "name");
            if (string.IsNullOrWhiteSpace(name))
                name = "Barbershop (OSM)";

            var address = BuildAddress(tags);
            var (lat, lon) = ResolveCoordinates(element, type);

            results.Add(new ExternalPlaceCandidate(
                externalId,
                name,
                address,
                lat,
                lon));
        }

        return results
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildOverpassQuery(string cityDisplayName, int timeoutSeconds)
    {
        var cityEscaped = cityDisplayName.Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"""
                [out:json][timeout:{timeoutSeconds}];
                area["name"="{cityEscaped}"]["boundary"="administrative"]->.searchArea;
                (
                  node["shop"="barber"](area.searchArea);
                  way["shop"="barber"](area.searchArea);
                  relation["shop"="barber"](area.searchArea);
                );
                out center tags;
                """;
    }

    private static (double? Latitude, double? Longitude) ResolveCoordinates(JsonElement element, string type)
    {
        if (type == "node")
        {
            var lat = element.TryGetProperty("lat", out var latProp) ? latProp.GetDouble() : (double?)null;
            var lon = element.TryGetProperty("lon", out var lonProp) ? lonProp.GetDouble() : (double?)null;
            return (lat, lon);
        }

        if (element.TryGetProperty("center", out var center))
        {
            var lat = center.TryGetProperty("lat", out var latProp) ? latProp.GetDouble() : (double?)null;
            var lon = center.TryGetProperty("lon", out var lonProp) ? lonProp.GetDouble() : (double?)null;
            return (lat, lon);
        }

        return (null, null);
    }

    private static string? TryGetTag(JsonElement tags, string key)
    {
        if (tags.ValueKind != JsonValueKind.Object)
            return null;
        return tags.TryGetProperty(key, out var value) ? value.GetString() : null;
    }

    private static string? BuildAddress(JsonElement tags)
    {
        var street = TryGetTag(tags, "addr:street");
        var houseNumber = TryGetTag(tags, "addr:housenumber");
        var city = TryGetTag(tags, "addr:city");

        var line1 = string.Join(" ", new[] { street, houseNumber }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        var line2 = city?.Trim();
        var combined = string.Join(", ", new[] { line1, line2 }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }
}
