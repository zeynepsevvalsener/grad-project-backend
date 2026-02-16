namespace GradProject.Application.Services.Polyline
{
    /// <summary>
    /// Decodes Google Polyline Algorithm encoded strings (used by Strava for summary_polyline).
    /// Implements the algorithm with 5-decimal precision (value / 1e5).
    /// </summary>
    public class PolylineDecoder
    {
        private const double Precision = 1e5;

        /// <summary>
        /// Decodes a polyline string into a list of (latitude, longitude) coordinate pairs.
        /// Returns null if the input is invalid or decoding fails.
        /// </summary>
        /// <param name="encoded">The encoded polyline string</param>
        /// <returns>List of (lat, lng) tuples, or null if decoding fails</returns>
        public List<(double lat, double lng)>? Decode(string? encoded)
        {
            if (string.IsNullOrWhiteSpace(encoded))
                return null;

            try
            {
                // Estimate initial capacity: polyline length / 2 (each coordinate pair needs ~2-4 chars)
                // This reduces List resizing for large polylines
                var estimatedCapacity = Math.Max(10, encoded.Length / 2);
                var coordinates = new List<(double lat, double lng)>(estimatedCapacity);
                var index = 0;
                var lat = 0.0;
                var lng = 0.0;

                while (index < encoded.Length)
                {
                    var deltaLat = DecodeValue(encoded, ref index);
                    var deltaLng = DecodeValue(encoded, ref index);

                    if (index < 0)
                        return null; // Invalid encoding

                    lat += deltaLat;
                    lng += deltaLng;

                    coordinates.Add((lat / Precision, lng / Precision));
                }

                return coordinates.Count > 0 ? coordinates : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Decodes a single value from the polyline string using delta and ZigZag decoding.
        /// </summary>
        private static int DecodeValue(string encoded, ref int index)
        {
            var result = 0;
            var shift = 0;
            var byteValue = 0;

            do
            {
                if (index >= encoded.Length)
                    return -1; // Invalid encoding

                byteValue = encoded[index++] - 63;

                if (byteValue < 0 || byteValue > 95)
                    return -1; // Invalid byte value

                result |= (byteValue & 0x1F) << shift;
                shift += 5;
            }
            while (byteValue >= 0x20);

            // ZigZag decode: (result >> 1) ^ -(result & 1)
            var delta = (result >> 1) ^ (-(result & 1));
            return delta;
        }
    }
}

