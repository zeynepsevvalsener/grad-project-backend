namespace GradProject.Application.DTOs.Running
{
    /// <summary>
    /// GeoJSON Feature response for a running activity route.
    /// Follows GeoJSON Feature specification.
    /// </summary>
    public class RouteGeoJsonResponseDto
    {
        public string Type { get; set; } = "Feature";

        public RouteGeometryDto Geometry { get; set; } = null!;

        public RoutePropertiesDto Properties { get; set; } = null!;
    }

    /// <summary>
    /// GeoJSON LineString geometry with coordinates in [longitude, latitude] order.
    /// </summary>
    public class RouteGeometryDto
    {
        public string Type { get; set; } = "LineString";

        public double[][] Coordinates { get; set; } = null!;
    }

    /// <summary>
    /// Properties associated with the route feature.
    /// </summary>
    public class RoutePropertiesDto
    {
        public int RunId { get; set; }

        public double Distance { get; set; }
    }
}

