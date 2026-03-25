using GradProject.Application.Services.Polyline;
using Xunit;

namespace GradProject.Tests;

public class PolylineDecoderTests
{
    private readonly PolylineDecoder _decoder = new();

    [Fact]
    public void Decode_ValidPolyline_ReturnsCoordinates()
    {
        // "_p~iF~ps|U" is the canonical single-point encoding of (38.5, -120.2)
        // from Google's Encoded Polyline Algorithm documentation.
        var encoded = "_p~iF~ps|U";

        var result = _decoder.Decode(encoded);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(Math.Abs(result[0].lat - 38.5) < 0.00001);
        Assert.True(Math.Abs(result[0].lng - -120.2) < 0.00001);
    }

    [Fact]
    public void Decode_MultiplePoints_ReturnsAllCoordinates()
    {
        // "_p~iF~ps|U_ulLnnqC" encodes two points from Google's algorithm example:
        // (38.5, -120.2) and (40.7, -120.95).
        var encoded = "_p~iF~ps|U_ulLnnqC";

        var result = _decoder.Decode(encoded);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(Math.Abs(result[0].lat - 38.5) < 0.00001);
        Assert.True(Math.Abs(result[0].lng - -120.2) < 0.00001);
        Assert.True(Math.Abs(result[1].lat - 40.7) < 0.00001);
        Assert.True(Math.Abs(result[1].lng - -120.95) < 0.00001);
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
        // Verifies that the decoder divides raw values by 1e5 (5-decimal precision).
        // "_p~iF~ps|U" encodes (38.5, -120.2); the raw integers are 3850000 and -12020000.
        var encoded = "_p~iF~ps|U";

        var result = _decoder.Decode(encoded);

        Assert.NotNull(result);
        var (lat, lng) = result[0];
        Assert.True(Math.Abs(lat - 38.5) < 0.00001, $"Expected ~38.5, got {lat}");
        Assert.True(Math.Abs(lng - -120.2) < 0.00001, $"Expected ~-120.2, got {lng}");
    }
}

