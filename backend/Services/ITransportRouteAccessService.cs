namespace backend.Services;

public interface ITransportRouteAccessService
{
    Task<bool> CanViewAsync(int userId, int routeId, CancellationToken cancellationToken);
}
