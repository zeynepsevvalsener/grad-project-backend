using GradProject.Application.DTOs.Geometry;
using GradProject.Application.Interfaces.Geometry;
using NetTopologySuite.Geometries;

namespace GradProject.Application.Services.Geometry
{
    /// <summary>
    /// Service for extracting bounding box (envelope) metadata from LineString geometries.
    /// Uses NetTopologySuite's EnvelopeInternal to calculate axis-aligned minimum bounding rectangle.
    /// </summary>
    public class BoundingBoxService : IBoundingBoxService
    {
        /// <summary>
        /// Extracts the bounding box from a LineString route.
        /// NetTopologySuite uses X = Longitude, Y = Latitude coordinate system.
        /// </summary>
        /// <param name="route">The LineString geometry to extract bounding box from</param>
        /// <returns>BoundingBox with MinLat, MaxLat, MinLng, MaxLng, or null if route is null/empty/invalid</returns>
        public BoundingBox? Extract(LineString? route)
        {
            if (route == null || route.IsEmpty)
                return null;

            try
            {
                var envelope = route.EnvelopeInternal;

                // NetTopologySuite: X = Longitude, Y = Latitude
                // EnvelopeInternal.MinY = MinLat, MaxY = MaxLat
                // EnvelopeInternal.MinX = MinLng, MaxX = MaxLng
                var minLat = envelope.MinY;
                var maxLat = envelope.MaxY;
                var minLng = envelope.MinX;
                var maxLng = envelope.MaxX;

                // Defensive check: validate bounding box values
                // Check for NaN or Infinity values
                if (double.IsNaN(minLat) || double.IsNaN(maxLat) || 
                    double.IsNaN(minLng) || double.IsNaN(maxLng) ||
                    double.IsInfinity(minLat) || double.IsInfinity(maxLat) ||
                    double.IsInfinity(minLng) || double.IsInfinity(maxLng))
                {
                    return null;
                }

                // Validate Min <= Max (should always be true for valid envelope, but defensive)
                if (minLat > maxLat || minLng > maxLng)
                {
                    return null;
                }

                return new BoundingBox
                {
                    MinLat = minLat,
                    MaxLat = maxLat,
                    MinLng = minLng,
                    MaxLng = maxLng
                };
            }
            catch
            {
                // Return null for any envelope calculation errors
                return null;
            }
        }
    }
}

