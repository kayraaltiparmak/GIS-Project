using backend.Models;

namespace backend.Services;

public interface ITblPolygonService
{
    Task<IEnumerable<TblPolygon>> GetAllAsync(int userId);

    Task<TblPolygon?> GetByIdAsync(int id, int userId);

    Task<PolygonCreateResult?> CreateAsync(string wkt, string name, string color, int userId);

    Task<GeometryUpdateResult> UpdateAsync(int id, string wkt, string name, string color, int userId);

    Task<bool> DeleteAsync(int id, int userId);
}
