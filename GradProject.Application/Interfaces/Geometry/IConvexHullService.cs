using NetTopologySuite.Geometries;

namespace GradProject.Application.Interfaces.Geometry
{
    /// <summary>
    /// Service for extracting convex hull polygon from LineString geometries.
    /// Convex hull creates a minimum convex polygon that encompasses all route points,
    /// providing a tighter fit than axis-aligned bounding box.
    /// </summary>
    public interface IConvexHullService
    {
        /// <summary>
        /// Extracts the convex hull polygon from a LineString route.
        /// Returns a Polygon that encompasses all points in the route.
        /// </summary>
        /// <param name="route">The LineString geometry to extract convex hull from</param>
        /// <returns>Polygon representing the convex hull, or null if route is null/empty/invalid</returns>
        Polygon? Extract(LineString? route);
    }
}

