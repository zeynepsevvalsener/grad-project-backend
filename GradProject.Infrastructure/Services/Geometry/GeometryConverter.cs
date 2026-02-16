using NetTopologySuite.Geometries;

namespace GradProject.Infrastructure.Services.Geometry
{
    /// <summary>
    /// Converts decoded polyline coordinates to NetTopologySuite LineString geometry.
    /// Handles coordinate order conversion (lat, lng) -> (lng, lat) for GeoJSON standard.
    /// </summary>
    public class GeometryConverter
    {
        private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

        /// <summary>
        /// Converts a list of (latitude, longitude) coordinates to a NetTopologySuite LineString.
        /// Coordinate order is converted to [longitude, latitude] for GeoJSON compatibility.
        /// </summary>
        /// <param name="coordinates">List of (lat, lng) tuples from polyline decoder</param>
        /// <returns>LineString geometry with SRID 4326, or null if input is invalid</returns>
        public LineString? ToLineString(List<(double lat, double lng)>? coordinates)
        {
            if (coordinates == null || coordinates.Count == 0)
                return null;

            // Single point creates an invalid LineString, return null
            if (coordinates.Count < 2)
                return null;

            // Convert (lat, lng) to [lng, lat] for GeoJSON standard
            var points = new Coordinate[coordinates.Count];
            for (int i = 0; i < coordinates.Count; i++)
            {
                var (lat, lng) = coordinates[i];
                points[i] = new Coordinate(lng, lat);
            }

            var lineString = GeometryFactory.CreateLineString(points);
            return lineString;
        }
    }
}

