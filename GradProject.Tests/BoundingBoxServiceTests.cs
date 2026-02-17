using GradProject.Application.DTOs.Geometry;
using GradProject.Application.Services.Geometry;
using NetTopologySuite.Geometries;
using Xunit;

namespace GradProject.Tests;

public class BoundingBoxServiceTests
{
    private readonly BoundingBoxService _service = new();
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    [Fact]
    public void Extract_GivenValidLineString_ReturnsCorrectBBox()
    {
        // Arrange: Create a LineString with known coordinates
        // Istanbul coordinates: (41.0082, 28.9784) to (41.0123, 28.9856)
        var coordinates = new[]
        {
            new Coordinate(28.9784, 41.0082), // Longitude, Latitude
            new Coordinate(28.9856, 41.0123)
        };
        var lineString = GeometryFactory.CreateLineString(coordinates);

        // Act
        var result = _service.Extract(lineString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(41.0082, result.MinLat, precision: 4);
        Assert.Equal(41.0123, result.MaxLat, precision: 4);
        Assert.Equal(28.9784, result.MinLng, precision: 4);
        Assert.Equal(28.9856, result.MaxLng, precision: 4);
        Assert.True(result.MinLat <= result.MaxLat);
        Assert.True(result.MinLng <= result.MaxLng);
    }

    [Fact]
    public void Extract_GivenEmptyLineString_ReturnsNull()
    {
        // Arrange: Create an empty LineString
        var emptyLineString = GeometryFactory.CreateLineString(Array.Empty<Coordinate>());

        // Act
        var result = _service.Extract(emptyLineString);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_GivenNullRoute_ReturnsNull()
    {
        // Act
        var result = _service.Extract(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_GivenSinglePoint_ReturnsBBoxWithMinEqualsMax()
    {
        // Arrange: Single point LineString (min = max)
        var coordinates = new[]
        {
            new Coordinate(28.9784, 41.0082),
            new Coordinate(28.9784, 41.0082) // Same point
        };
        var lineString = GeometryFactory.CreateLineString(coordinates);

        // Act
        var result = _service.Extract(lineString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.MinLat, result.MaxLat, precision: 6);
        Assert.Equal(result.MinLng, result.MaxLng, precision: 6);
    }

    [Fact]
    public void Extract_GivenMultiPointLineString_ReturnsCorrectBBox()
    {
        // Arrange: LineString with multiple points forming a path
        var coordinates = new[]
        {
            new Coordinate(28.9784, 41.0082), // Min point
            new Coordinate(28.9856, 41.0123), // Max point
            new Coordinate(28.9800, 41.0100), // Middle point
            new Coordinate(28.9820, 41.0110)  // Another middle point
        };
        var lineString = GeometryFactory.CreateLineString(coordinates);

        // Act
        var result = _service.Extract(lineString);

        // Assert
        Assert.NotNull(result);
        // MinLat should be the minimum Y coordinate
        Assert.Equal(41.0082, result.MinLat, precision: 4);
        // MaxLat should be the maximum Y coordinate
        Assert.Equal(41.0123, result.MaxLat, precision: 4);
        // MinLng should be the minimum X coordinate
        Assert.Equal(28.9784, result.MinLng, precision: 4);
        // MaxLng should be the maximum X coordinate
        Assert.Equal(28.9856, result.MaxLng, precision: 4);
    }

    [Fact]
    public void Extract_GivenLineStringWithNegativeCoordinates_ReturnsCorrectBBox()
    {
        // Arrange: LineString with negative coordinates (e.g., US West Coast)
        var coordinates = new[]
        {
            new Coordinate(-122.4194, 37.7749), // San Francisco
            new Coordinate(-122.4094, 37.7849)  // Slightly northeast
        };
        var lineString = GeometryFactory.CreateLineString(coordinates);

        // Act
        var result = _service.Extract(lineString);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.MinLat < result.MaxLat);
        Assert.True(result.MinLng < result.MaxLng);
        Assert.True(result.MinLng < 0); // Negative longitude
    }
}

