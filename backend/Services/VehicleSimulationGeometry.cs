using NetTopologySuite.Geometries;

namespace backend.Services;

public static class VehicleSimulationGeometry
{
    private const double EarthRadiusMeters = 6_371_000;

    public static double DistanceMeters(Coordinate first, Coordinate second)
    {
        Validate(first); Validate(second);
        var lat1 = DegreesToRadians(first.Y);
        var lat2 = DegreesToRadians(second.Y);
        var latitudeDelta = DegreesToRadians(second.Y - first.Y);
        var longitudeDelta = DegreesToRadians(second.X - first.X);
        var a = Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2);
        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public static (Coordinate Coordinate, int SegmentIndex) Interpolate(
        IReadOnlyList<Coordinate> coordinates, IReadOnlyList<double> segmentLengths, double distanceMeters)
    {
        if (coordinates.Count < 2 || segmentLengths.Count != coordinates.Count - 1)
            throw new ArgumentException("Gecerli bir LineString koordinat dizisi zorunludur.");
        var remaining = Math.Max(0, distanceMeters);
        for (var index = 0; index < segmentLengths.Count; index++)
        {
            var length = segmentLengths[index];
            if (remaining <= length || index == segmentLengths.Count - 1)
            {
                var ratio = length <= 0 ? 0 : Math.Clamp(remaining / length, 0, 1);
                var start = coordinates[index]; var end = coordinates[index + 1];
                var result = new Coordinate(start.X + ((end.X - start.X) * ratio), start.Y + ((end.Y - start.Y) * ratio));
                Validate(result);
                return (result, index);
            }
            remaining -= length;
        }
        return (coordinates[^1].Copy(), coordinates.Count - 2);
    }

    public static void Validate(Coordinate coordinate)
    {
        if (!double.IsFinite(coordinate.X) || !double.IsFinite(coordinate.Y)
            || coordinate.X is < -180 or > 180 || coordinate.Y is < -90 or > 90)
            throw new InvalidOperationException("Gecersiz rota koordinati.");
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
