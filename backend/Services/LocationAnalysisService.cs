using System.Text.Json;
using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace backend.Services;

public sealed class LocationAnalysisService : ILocationAnalysisService
{
    private readonly AppDbContext _db;
    private readonly GeometryFactory _factory = new(new PrecisionModel(), 4326);
    public LocationAnalysisService(AppDbContext db) => _db = db;

    public async Task<LocationAnalysisServiceResult> AnalyzeAsync(LocationAnalysisRequest request, int userId)
    {
        var validation = Validate(request);
        if (validation != null) return new(null, validation);
        Geometry target;
        try { target = ReadGeometry(request.TargetGeometry); }
        catch (Exception) { return new(null, "Hedef geometri geçerli bir Polygon veya MultiPolygon olmalıdır."); }
        if (!target.IsValid || target.IsEmpty) return new(null, "Hedef geometri geçersiz veya boştur.");
        if (target.NumPoints > 10000) return new(null, "Hedef geometri en fazla 10000 köşe içerebilir.");

        var categories = await _db.TblPoiCategories.Where(x => x.IsActive && !x.IsDeleted)
            .Select(x => new { x.Id, x.Name, x.ParentId }).ToListAsync();
        var requested = request.Criteria.Select(x => x.CategoryId).ToHashSet();
        if (requested.Any(id => categories.All(x => x.Id != id))) return new(null, "Geçersiz veya pasif CategoryId gönderildi.");

        var criterionCategoryIds = new Dictionary<int, HashSet<int>>();
        foreach (var criterion in request.Criteria)
        {
            var ids = new HashSet<int> { criterion.CategoryId };
            var frontier = new Queue<int>(); frontier.Enqueue(criterion.CategoryId);
            while (frontier.Count > 0)
            {
                var parent = frontier.Dequeue();
                foreach (var child in categories.Where(x => x.ParentId == parent && ids.Add(x.Id))) frontier.Enqueue(child.Id);
            }
            criterionCategoryIds[criterion.CategoryId] = ids;
        }
        var allIds = criterionCategoryIds.Values.SelectMany(x => x).Distinct().ToList();
        var pois = await _db.TblPois.AsNoTracking().Include(x => x.Category)
            .Where(x => x.IsActive && !x.IsDeleted && allIds.Contains(x.CategoryId) && x.Geometry.Intersects(target))
            .ToListAsync();

        await _db.LocationAnalysisResults.Where(x => x.UserId == userId && x.IsActive)
            .ExecuteUpdateAsync(x => x.SetProperty(r => r.IsActive, false));
        var analysisId = Guid.NewGuid();
        var results = new List<LocationAnalysisResult>();
        foreach (var poi in pois)
        {
            var criterion = request.Criteria.First(x => criterionCategoryIds[x.CategoryId].Contains(poi.CategoryId));
            results.Add(new LocationAnalysisResult { AnalysisId = analysisId, UserId = userId, PoiId = poi.Id,
                CategoryId = poi.CategoryId, CriterionWeight = criterion.Weight, HeatWeight = criterion.Weight / 100d,
                Geometry = poi.Geometry.Copy() as Point ?? poi.Geometry });
        }
        _db.LocationAnalysisResults.AddRange(results);
        await _db.SaveChangesAsync();
        var summaries = request.Criteria.Select(c => new LocationAnalysisCriterionSummary {
            CategoryId = c.CategoryId, CategoryName = categories.First(x => x.Id == c.CategoryId).Name, Weight = c.Weight,
            MatchedPoiCount = pois.Count(p => criterionCategoryIds[c.CategoryId].Contains(p.CategoryId)) }).ToList();
        return new(new LocationAnalysisResponse { AnalysisId = analysisId, MatchedPoiCount = results.Count,
            CriteriaSummary = summaries,
            Points = results.Select(x => new LocationAnalysisPointResponse { PoiId = x.PoiId, Longitude = x.Geometry.X, Latitude = x.Geometry.Y, HeatWeight = x.HeatWeight }).ToList(),
            Message = results.Count == 0 ? "Seçilen alanda uygun POI bulunamadı." : "Analiz başarıyla tamamlandı" }, null);
    }

    public async Task<bool> DeactivateAsync(Guid analysisId, int userId, bool isAdmin)
    {
        var query = _db.LocationAnalysisResults.Where(x => x.AnalysisId == analysisId && x.IsActive);
        if (!isAdmin) query = query.Where(x => x.UserId == userId);
        return await query.ExecuteUpdateAsync(x => x.SetProperty(r => r.IsActive, false)) > 0;
    }

    private static string? Validate(LocationAnalysisRequest request)
    {
        if (request.Criteria.Count is < 2 or > 5) return "En az 2, en fazla 5 kriter seçilmelidir.";
        if (request.Criteria.Any(x => x.CategoryId <= 0 || x.Weight is < 1 or > 100)) return "Her kriterde geçerli kategori ve 1-100 arası tam sayı ağırlık zorunludur.";
        if (request.Criteria.Select(x => x.CategoryId).Distinct().Count() != request.Criteria.Count) return "Aynı kategori birden fazla seçilemez.";
        if (request.Criteria.Sum(x => x.Weight) != 100) return "Kriter toplamı 100 olmalıdır.";
        if (request.TargetGeometry.ValueKind != JsonValueKind.Object) return "Hedef bölge seçilmedi.";
        return null;
    }

    private Geometry ReadGeometry(JsonElement json)
    {
        var type = json.GetProperty("type").GetString();
        var coords = json.GetProperty("coordinates");
        if (type == "Polygon") return ReadPolygon(coords);
        if (type == "MultiPolygon") return _factory.CreateMultiPolygon(coords.EnumerateArray().Select(ReadPolygon).ToArray());
        throw new ArgumentException();
    }
    private Polygon ReadPolygon(JsonElement coords)
    {
        var rings = coords.EnumerateArray().Select(r => _factory.CreateLinearRing(r.EnumerateArray()
            .Select(p => new Coordinate(p[0].GetDouble(), p[1].GetDouble())).ToArray())).ToArray();
        return _factory.CreatePolygon(rings[0], rings.Skip(1).ToArray());
    }
}
