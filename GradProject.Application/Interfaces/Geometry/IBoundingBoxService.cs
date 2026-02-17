using GradProject.Application.DTOs.Geometry;
using NetTopologySuite.Geometries;

namespace GradProject.Application.Interfaces.Geometry
{
    /// <summary>
    /// Service for extracting bounding box (envelope) metadata from LineString geometries.
    /// Used for spatial pre-filtering and territory metadata extraction.
    /// </summary>
    public interface IBoundingBoxService
    {
        /// <summary>
        /// Extracts the bounding box (axis-aligned minimum bounding rectangle) from a LineString route.
        /// </summary>
        /// <param name="route">The LineString geometry to extract bounding box from</param>
        /// <returns>BoundingBox with MinLat, MaxLat, MinLng, MaxLng, or null if route is null/empty/invalid</returns>
        BoundingBox? Extract(LineString? route);
    }
}

