namespace GradProject.Application.DTOs.Running
{
    /// <summary>
    /// Bounding box metadata response for a running activity.
    /// </summary>
    public class BoundingBoxMetadataDto
    {
        public int RunId { get; set; }
        public string RunName { get; set; } = null!;
        public bool HasRoute { get; set; }
        public BoundingBoxInfoDto? BoundingBox { get; set; }
        public BoundingBoxValidationDto? Validation { get; set; }
    }

    /// <summary>
    /// Bounding box information with calculated center and dimensions.
    /// </summary>
    public class BoundingBoxInfoDto
    {
        public double MinLat { get; set; }
        public double MaxLat { get; set; }
        public double MinLng { get; set; }
        public double MaxLng { get; set; }
        public double CenterLat { get; set; }
        public double CenterLng { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    /// <summary>
    /// Validation information for bounding box.
    /// </summary>
    public class BoundingBoxValidationDto
    {
        public bool IsValid { get; set; }
        public bool MinLatLessThanMaxLat { get; set; }
        public bool MinLngLessThanMaxLng { get; set; }
    }

    /// <summary>
    /// Backfill result for bounding box and convex hull.
    /// </summary>
    public class BackfillBoundingBoxResultDto
    {
        public int RunId { get; set; }
        public string RunName { get; set; } = null!;
        public string Message { get; set; } = null!;
        public BoundingBoxInfoDto? BoundingBox { get; set; }
        public ConvexHullInfoDto? ConvexHull { get; set; }
    }

    /// <summary>
    /// Convex hull information.
    /// </summary>
    public class ConvexHullInfoDto
    {
        public bool Available { get; set; }
        public int NumPoints { get; set; }
        public bool IsValid { get; set; }
        public bool IsEmpty { get; set; }
    }
}

