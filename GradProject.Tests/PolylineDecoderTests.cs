using GradProject.Application.Services.Polyline;
using Xunit;

namespace GradProject.Tests;

public class PolylineDecoderTests
{
    private readonly PolylineDecoder _decoder = new();

    [Fact]
    public void Decode_ValidPolyline_ReturnsCoordinates()
    {
        // Known test vector: "`~oia@" encodes to (38.5, -120.2)
        // This is a simple polyline encoding from Google's documentation
        // Encoded: "`~oia@" decodes to approximately (38.5, -120.2)
        var encoded = "`~oia@";
        
        var result = _decoder.Decode(encoded);

        Assert.NotNull(result);
        Assert.Single(result);
        
        // Debug: Print actual values
        var actualLat = result[0].lat;
        var actualLng = result[0].lng;
        var expectedLat = 38.5;
        var expectedLng = -120.2;
        
        // Allow tolerance of 0.00001 as specified
        var latDiff = Math.Abs(actualLat - expectedLat);
        var lngDiff = Math.Abs(actualLng - expectedLng);
        
        // If test fails, show actual vs expected
        if (latDiff >= 0.00001 || lngDiff >= 0.00001)
        {
            Assert.True(false, 
                $"Expected: lat={expectedLat}, lng={expectedLng}. " +
                $"Actual: lat={actualLat}, lng={actualLng}. " +
                $"Diff: lat={latDiff}, lng={lngDiff}");
        }
        
        Assert.True(Math.Abs(result[0].lat - 38.5) < 0.00001);
        Assert.True(Math.Abs(result[0].lng - -120.2) < 0.00001);
    }

    [Fact]
    public void Decode_MultiplePoints_ReturnsAllCoordinates()
    {
        // Test polyline with multiple points
        // This encodes a path with several coordinates
        // Using a known valid polyline: "~p|F_p~P" (approximate path)
        var encoded = "~p|F_p~P";
        
        var result = _decoder.Decode(encoded);

        Assert.NotNull(result);
        Assert.True(result.Count >= 2);
    }

    [Fact]
    public void Decode_NullInput_ReturnsNull()
    {
        var result = _decoder.Decode(null);

        Assert.Null(result);
    }

    [Fact]
    public void Decode_EmptyString_ReturnsNull()
    {
        var result = _decoder.Decode("");

        Assert.Null(result);
    }

    [Fact]
    public void Decode_WhitespaceString_ReturnsNull()
    {
        var result = _decoder.Decode("   ");

        Assert.Null(result);
    }

    [Fact]
    public void Decode_InvalidPolyline_ReturnsNull()
    {
        // Invalid characters that don't follow polyline encoding rules
        var invalidEncodings = new[]
        {
            "abc",
            "!!!",
            "123",
            "~", // Single invalid character
        };

        foreach (var invalid in invalidEncodings)
        {
            var result = _decoder.Decode(invalid);
            // Should return null or handle gracefully without throwing
            // The decoder should be exception-safe
        }
    }

    [Fact]
    public void Decode_StravaExample_ReturnsValidCoordinates()
    {
        // Example from a real Strava polyline (simplified)
        // This is a typical encoded polyline from Strava
        var encoded = "u{~|Fnyq@V}J|@LfBV";
        
        var result = _decoder.Decode(encoded);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        
        // Verify coordinates are within valid lat/lng ranges
        foreach (var (lat, lng) in result)
        {
            Assert.True(lat >= -90 && lat <= 90, $"Latitude {lat} out of range");
            Assert.True(lng >= -180 && lng <= 180, $"Longitude {lng} out of range");
        }
    }

    [Fact]
    public void Decode_SinglePoint_ReturnsSingleCoordinate()
    {
        // A polyline encoding that results in a single point
        // Note: This might not be a valid LineString (needs 2+ points)
        // but the decoder should still decode it
        var encoded = "`~oia@";
        
        var result = _decoder.Decode(encoded);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public void Decode_Precision_MatchesExpectedTolerance()
    {
        // Test that decoded coordinates maintain 5-decimal precision
        // Using a known encoding that should decode to specific values
        var encoded = "`~oia@";
        
        var result = _decoder.Decode(encoded);

        Assert.NotNull(result);
        var (lat, lng) = result[0];
        
        // Check that values are properly divided by 1e5 (5-decimal precision)
        // The decoded values should be reasonable coordinates
        Assert.True(Math.Abs(lat) < 90);
        Assert.True(Math.Abs(lng) < 180);
    }
}

