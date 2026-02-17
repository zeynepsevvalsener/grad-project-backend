namespace GradProject.Application.DTOs.Geometry
{
    /// <summary>
    /// Represents a bounding box (axis-aligned minimum bounding rectangle) for a geographic region.
    /// Used for spatial pre-filtering and territory metadata.
    /// </summary>
    public record BoundingBox
    {
        public double MinLat { get; init; }
        public double MaxLat { get; init; }
        public double MinLng { get; init; }
        public double MaxLng { get; init; }
    }
}

