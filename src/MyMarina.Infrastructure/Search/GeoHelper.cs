namespace MyMarina.Infrastructure.Search;

internal static class GeoHelper
{
    private const double EarthRadiusMiles = 3958.8;

    internal static (decimal MinLat, decimal MaxLat, decimal MinLon, decimal MaxLon) BoundingBox(
        decimal centerLat, decimal centerLon, decimal radiusMiles)
    {
        const decimal DegreesPerMile = 1.0m / 69.0m;
        var deltaLat = radiusMiles * DegreesPerMile;
        var cosLat = (decimal)Math.Cos((double)centerLat * Math.PI / 180.0);
        var deltaLon = cosLat == 0 ? deltaLat : radiusMiles * DegreesPerMile / cosLat;
        return (centerLat - deltaLat, centerLat + deltaLat, centerLon - deltaLon, centerLon + deltaLon);
    }

    internal static double HaversineDistanceMiles(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusMiles * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
