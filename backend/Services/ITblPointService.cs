using backend.Models;

namespace backend.Services;

public interface ITblPointService
{
    Task<IEnumerable<TblPoint>> GetAllAsync(int userId);

    Task<TblPoint?> GetByIdAsync(int id, int userId);

    Task<int?> CreateAsync(string wkt, string name, string color, int userId);

    Task<GeometryUpdateResult> UpdateAsync(int id, string wkt, string name, string color, int userId);

    Task<bool> DeleteAsync(int id, int userId);
}
