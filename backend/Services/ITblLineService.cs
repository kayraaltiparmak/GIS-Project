using backend.Models;

namespace backend.Services;

public interface ITblLineService
{
    Task<IEnumerable<TblLine>> GetAllAsync(int userId);

    Task<TblLine?> GetByIdAsync(int id, int userId);

    Task<int?> CreateAsync(string wkt, string name, string color, int userId);

    Task<GeometryUpdateResult> UpdateAsync(int id, string wkt, string name, string color, int userId);

    Task<bool> DeleteAsync(int id, int userId);
}
