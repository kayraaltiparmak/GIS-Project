using System.Globalization;
using System.Net.Http.Json;
using backend.DTOs;
using NetTopologySuite.Geometries;

namespace backend.Services;

public sealed class OsrmService : IOsrmService
{
    private readonly HttpClient _client;
    private readonly ILogger<OsrmService> _logger;

    public OsrmService(HttpClient client, ILogger<OsrmService> logger) =>
        (_client, _logger) = (client, logger);

    public async Task<OsrmRoute> GetRouteAsync(IReadOnlyList<Point> stops, CancellationToken cancellationToken)
    {
        if (stops.Count < 2) throw new InvalidOperationException("Rota hesaplamak icin en az iki aktif durak gereklidir.");
        var coordinates = string.Join(';', stops.Select(point =>
            string.Create(CultureInfo.InvariantCulture, $"{point.X},{point.Y}")));
        var path = $"route/v1/driving/{coordinates}?overview=full&geometries=geojson&steps=false";
        HttpResponseMessage response;
        try
        {
            response = await _client.GetAsync(path, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("OSRM servisi zaman asimina ugradi. Servisin calistigini kontrol edin.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OSRM service at {BaseAddress} is unreachable.", _client.BaseAddress);
            throw new InvalidOperationException("OSRM servisine ulasilamiyor. localhost:5000 adresindeki servisi kontrol edin.");
        }

        using (response)
        {
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OSRM returned HTTP {StatusCode}.", (int)response.StatusCode);
            throw new InvalidOperationException("Rota servisi istegi tamamlayamadi.");
        }
        var payload = await response.Content.ReadFromJsonAsync<OsrmRouteResponse>(cancellationToken: cancellationToken);
        if (payload == null || !string.Equals(payload.Code, "Ok", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("OSRM returned code {Code}: {Message}", payload?.Code, payload?.Message);
            throw new InvalidOperationException("Rota servisi gecerli bir sonuc dondurmedi.");
        }
        var route = payload.Routes.FirstOrDefault();
        if (route == null || !route.Geometry.Type.Equals("LineString", StringComparison.OrdinalIgnoreCase) || route.Geometry.Coordinates.Count < 2)
            throw new InvalidOperationException("Rota servisi bos veya gecersiz geometri dondurdu.");
        return route;
        }
    }
}
