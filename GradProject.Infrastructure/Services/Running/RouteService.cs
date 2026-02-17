using GradProject.Application.DTOs.Running;
using GradProject.Application.Interfaces.Geometry;
using GradProject.Application.Interfaces.Running;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace GradProject.Infrastructure.Services.Running
{
    /// <summary>
    /// Service implementation for retrieving running activity routes in GeoJSON format.
    /// </summary>
    public class RouteService : IRouteService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<RouteService> _logger;
        private readonly IBoundingBoxService _boundingBoxService;
        private readonly IConvexHullService _convexHullService;

        public RouteService(
            AppDbContext db,
            ILogger<RouteService> logger,
            IBoundingBoxService boundingBoxService,
            IConvexHullService convexHullService)
        {
            _db = db;
            _logger = logger;
            _boundingBoxService = boundingBoxService;
            _convexHullService = convexHullService;
        }

        public async Task<RouteGeoJsonResponseDto?> GetRouteGeoJsonAsync(int runId, int userId, CancellationToken ct = default)
        {
            try
            {
                var activity = await _db.RunningActivities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == runId && r.UserId == userId, ct);

                if (activity == null)
                {
                    _logger.LogWarning("Running activity {RunId} not found or doesn't belong to user {UserId}", runId, userId);
                    return null;
                }

                if (activity.Route == null)
                {
                    _logger.LogInformation("Running activity {RunId} has no route data", runId);
                    return null;
                }

                return ConvertToGeoJson(activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving route for run {RunId} and user {UserId}", runId, userId);
                return null;
            }
        }

        public async Task<bool> ActivityExistsAsync(int runId, int userId, CancellationToken ct = default)
        {
            try
            {
                var exists = await _db.RunningActivities
                    .AsNoTracking()
                    .AnyAsync(r => r.Id == runId && r.UserId == userId, ct);
                
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if activity {RunId} exists for user {UserId}", runId, userId);
                return false;
            }
        }

        private static RouteGeoJsonResponseDto ConvertToGeoJson(RunningActivity activity)
        {
            var lineString = activity.Route!;
            var coordinates = new List<double[]>();

            foreach (var coordinate in lineString.Coordinates)
            {
                // GeoJSON uses [longitude, latitude] order
                coordinates.Add(new[] { coordinate.X, coordinate.Y });
            }

            return new RouteGeoJsonResponseDto
            {
                Type = "Feature",
                Geometry = new RouteGeometryDto
                {
                    Type = "LineString",
                    Coordinates = coordinates.ToArray()
                },
                Properties = new RoutePropertiesDto
                {
                    RunId = activity.Id,
                    Distance = activity.DistanceMeters,
                    BoundingBox = activity.MinLat.HasValue && activity.MaxLat.HasValue && 
                                 activity.MinLng.HasValue && activity.MaxLng.HasValue
                        ? new BoundingBoxDto
                        {
                            MinLat = activity.MinLat.Value,
                            MaxLat = activity.MaxLat.Value,
                            MinLng = activity.MinLng.Value,
                            MaxLng = activity.MaxLng.Value
                        }
                        : null,
                    ConvexHull = activity.ConvexHull != null
                        ? ConvertConvexHullToDto(activity.ConvexHull)
                        : null
                }
            };
        }

        private static ConvexHullDto? ConvertConvexHullToDto(NetTopologySuite.Geometries.Polygon convexHull)
        {
            if (convexHull == null || convexHull.IsEmpty)
                return null;

            try
            {
                // GeoJSON Polygon format: [[[lng, lat], [lng, lat], ...]]
                // First ring is exterior ring, subsequent rings are holes
                var coordinates = new List<double[][]>();
                
                // Exterior ring
                var exteriorRing = new List<double[]>();
                foreach (var coordinate in convexHull.ExteriorRing.Coordinates)
                {
                    exteriorRing.Add(new[] { coordinate.X, coordinate.Y }); // [lng, lat]
                }
                coordinates.Add(exteriorRing.ToArray());

                // Interior rings (holes) - if any
                for (int i = 0; i < convexHull.NumInteriorRings; i++)
                {
                    var interiorRing = new List<double[]>();
                    foreach (var coordinate in convexHull.GetInteriorRingN(i).Coordinates)
                    {
                        interiorRing.Add(new[] { coordinate.X, coordinate.Y }); // [lng, lat]
                    }
                    coordinates.Add(interiorRing.ToArray());
                }

                return new ConvexHullDto
                {
                    Type = "Polygon",
                    Coordinates = coordinates.ToArray()
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<BoundingBoxMetadataDto?> GetBoundingBoxMetadataAsync(int runId, int userId, CancellationToken ct = default)
        {
            try
            {
                var activity = await _db.RunningActivities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == runId && r.UserId == userId, ct);

                if (activity == null)
                {
                    _logger.LogWarning("Running activity {RunId} not found or doesn't belong to user {UserId}", runId, userId);
                    return null;
                }

                if (activity.MinLat == null || activity.MaxLat == null ||
                    activity.MinLng == null || activity.MaxLng == null)
                {
                    return new BoundingBoxMetadataDto
                    {
                        RunId = activity.Id,
                        RunName = activity.Name,
                        HasRoute = activity.Route != null,
                        BoundingBox = null,
                        Validation = null
                    };
                }

                return new BoundingBoxMetadataDto
                {
                    RunId = activity.Id,
                    RunName = activity.Name,
                    HasRoute = activity.Route != null,
                    BoundingBox = new BoundingBoxInfoDto
                    {
                        MinLat = activity.MinLat.Value,
                        MaxLat = activity.MaxLat.Value,
                        MinLng = activity.MinLng.Value,
                        MaxLng = activity.MaxLng.Value,
                        CenterLat = (activity.MinLat.Value + activity.MaxLat.Value) / 2.0,
                        CenterLng = (activity.MinLng.Value + activity.MaxLng.Value) / 2.0,
                        Width = activity.MaxLng.Value - activity.MinLng.Value,
                        Height = activity.MaxLat.Value - activity.MinLat.Value
                    },
                    Validation = new BoundingBoxValidationDto
                    {
                        IsValid = activity.MinLat.Value <= activity.MaxLat.Value && activity.MinLng.Value <= activity.MaxLng.Value,
                        MinLatLessThanMaxLat = activity.MinLat.Value <= activity.MaxLat.Value,
                        MinLngLessThanMaxLng = activity.MinLng.Value <= activity.MaxLng.Value
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bounding box metadata for run {RunId} and user {UserId}", runId, userId);
                return null;
            }
        }

        public async Task<BackfillBoundingBoxResultDto?> BackfillBoundingBoxAsync(int runId, int userId, CancellationToken ct = default)
        {
            try
            {
                var activity = await _db.RunningActivities
                    .FirstOrDefaultAsync(r => r.Id == runId && r.UserId == userId, ct);

                if (activity == null)
                {
                    _logger.LogWarning("Running activity {RunId} not found or doesn't belong to user {UserId}", runId, userId);
                    return null;
                }

                if (activity.Route == null)
                {
                    _logger.LogInformation("Running activity {RunId} has no route data for backfill", runId);
                    return null;
                }

                // Extract bounding box and convex hull from existing route
                var bbox = _boundingBoxService.Extract(activity.Route);
                var convexHull = _convexHullService.Extract(activity.Route);

                var hasBbox = bbox != null && bbox.MinLat <= bbox.MaxLat && bbox.MinLng <= bbox.MaxLng;
                var hasConvexHull = convexHull != null && !convexHull.IsEmpty && convexHull.IsValid;

                // At least one must succeed
                if (!hasBbox && !hasConvexHull)
                {
                    _logger.LogWarning("Failed to extract valid bounding box or convex hull from route for run {RunId}", runId);
                    return null;
                }

                var updated = false;

                if (hasBbox && bbox != null)
                {
                    activity.MinLat = bbox.MinLat;
                    activity.MaxLat = bbox.MaxLat;
                    activity.MinLng = bbox.MinLng;
                    activity.MaxLng = bbox.MaxLng;
                    updated = true;
                }

                if (hasConvexHull)
                {
                    activity.ConvexHull = convexHull;
                    updated = true;
                }

                if (updated)
                {
                    activity.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);

                    // Reload from database to ensure ConvexHull is loaded
                    await _db.Entry(activity).ReloadAsync(ct);
                }

                return new BackfillBoundingBoxResultDto
                {
                    RunId = activity.Id,
                    RunName = activity.Name,
                    Message = "Region metadata backfilled successfully",
                    BoundingBox = hasBbox && activity.MinLat.HasValue && activity.MaxLat.HasValue &&
                                 activity.MinLng.HasValue && activity.MaxLng.HasValue
                        ? new BoundingBoxInfoDto
                        {
                            MinLat = activity.MinLat.Value,
                            MaxLat = activity.MaxLat.Value,
                            MinLng = activity.MinLng.Value,
                            MaxLng = activity.MaxLng.Value,
                            CenterLat = (activity.MinLat.Value + activity.MaxLat.Value) / 2.0,
                            CenterLng = (activity.MinLng.Value + activity.MaxLng.Value) / 2.0,
                            Width = activity.MaxLng.Value - activity.MinLng.Value,
                            Height = activity.MaxLat.Value - activity.MinLat.Value
                        }
                        : null,
                    ConvexHull = hasConvexHull && activity.ConvexHull != null
                        ? new ConvexHullInfoDto
                        {
                            Available = true,
                            NumPoints = activity.ConvexHull.ExteriorRing?.NumPoints ?? 0,
                            IsValid = activity.ConvexHull.IsValid,
                            IsEmpty = activity.ConvexHull.IsEmpty
                        }
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling bounding box for run {RunId} and user {UserId}", runId, userId);
                return null;
            }
        }
    }
}

