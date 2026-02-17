using GradProject.Application.Interfaces.Geometry;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;

namespace GradProject.Application.Services.Geometry
{
    /// <summary>
    /// Service for extracting convex hull polygon from LineString geometries.
    /// Uses NetTopologySuite's ConvexHull algorithm to create a minimum convex polygon
    /// that encompasses all route points, providing a tighter fit than axis-aligned bounding box.
    /// </summary>
    public class ConvexHullService : IConvexHullService
    {
        private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

        /// <summary>
        /// Extracts the convex hull polygon from a LineString route.
        /// </summary>
        /// <param name="route">The LineString geometry to extract convex hull from</param>
        /// <returns>Polygon representing the convex hull, or null if route is null/empty/invalid</returns>
        public Polygon? Extract(LineString? route)
        {
            if (route == null || route.IsEmpty)
                return null;

            try
            {
                var numPoints = route.NumPoints;
                
                // Need at least 2 points for any geometry
                if (numPoints < 2)
                {
                    return null;
                }

                // NetTopologySuite ConvexHull algorithm
                var convexHull = new ConvexHull(route);
                var hullGeometry = convexHull.GetConvexHull();

                // ConvexHull can return different geometry types:
                // - Point: if route has only 1 point (shouldn't happen due to check above)
                // - LineString: if route has 2 points or all points are collinear
                // - Polygon: if route has 3+ non-collinear points (expected case)
                
                if (hullGeometry is Polygon polygon)
                {
                    // Validate polygon
                    if (!polygon.IsValid || polygon.IsEmpty)
                    {
                        return null;
                    }

                    return polygon;
                }

                // For LineString case (2 points or collinear), create a buffer polygon
                if (hullGeometry is LineString lineString)
                {
                    // Create a small buffer around the line to form a polygon
                    // Buffer distance: ~50 meters in degrees (approximate)
                    // Use a larger buffer to ensure polygon creation
                    const double bufferDistanceDegrees = 0.0005; // ~55 meters at equator
                    
                    try
                    {
                        var bufferOp = new BufferOp(lineString, new BufferParameters
                        {
                            EndCapStyle = EndCapStyle.Round,
                            JoinStyle = JoinStyle.Round,
                            MitreLimit = 5.0
                        });
                        var buffered = bufferOp.GetResultGeometry(bufferDistanceDegrees);
                        
                        if (buffered is Polygon bufferedPolygon && bufferedPolygon.IsValid && !bufferedPolygon.IsEmpty)
                        {
                            return bufferedPolygon;
                        }
                    }
                    catch
                    {
                        // Failed to create buffer polygon, try creating from envelope
                        try
                        {
                            var envelope = lineString.EnvelopeInternal;
                            if (!envelope.IsNull)
                            {
                                // Create a small rectangle polygon from envelope
                                var minX = envelope.MinX;
                                var maxX = envelope.MaxX;
                                var minY = envelope.MinY;
                                var maxY = envelope.MaxY;
                                
                                // Add small buffer
                                const double buffer = 0.0001; // ~11 meters
                                var coords = new[]
                                {
                                    new Coordinate(minX - buffer, minY - buffer),
                                    new Coordinate(maxX + buffer, minY - buffer),
                                    new Coordinate(maxX + buffer, maxY + buffer),
                                    new Coordinate(minX - buffer, maxY + buffer),
                                    new Coordinate(minX - buffer, minY - buffer) // Close ring
                                };
                                
                                var linearRing = GeometryFactory.CreateLinearRing(coords);
                                var envelopePolygon = new Polygon(linearRing);
                                
                                if (envelopePolygon.IsValid && !envelopePolygon.IsEmpty)
                                {
                                    return envelopePolygon;
                                }
                            }
                        }
                        catch
                        {
                            // Both methods failed
                        }
                    }
                }

                // For Point case, return null (can't create meaningful region)
                return null;
            }
            catch
            {
                // Return null for any convex hull calculation errors
                return null;
            }
        }
    }
}

