namespace backend.Services;

public class GeographicPermissionResult
{
    public bool IsAllowed { get; set; }

    public string? ErrorMessage { get; set; }

    public static GeographicPermissionResult Allowed()
    {
        return new GeographicPermissionResult { IsAllowed = true };
    }

    public static GeographicPermissionResult Denied(string message)
    {
        return new GeographicPermissionResult { IsAllowed = false, ErrorMessage = message };
    }
}
