using GradProject.Application.DTOs.Running;
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

        public RouteService(AppDbContext db, ILogger<RouteService> logger)
        {
            _db = db;
            _logger = logger;
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
                    Distance = activity.DistanceMeters
                }
            };
        }
    }
}

