using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Hashing;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helpers;

/// <summary>
/// Helper methods for computing map geometry and generating background images for <see cref="Map"/> instances.
/// </summary>
public static class MapHelper
{
    private const float MinResolution = 0.01f;

    private const float FallbackResolution = 0.05f;
    
    private const int TargetMaxPixels = 2000;

    private const int MinPixelDimension = 100;

    private const float MarginMeters = 2.0f;

    private const float MarginFraction = 0.1f;

    private const int FallbackExtent = 200;

    private static void WritePngChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> buf4 = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(buf4, data.Length);
        stream.Write(buf4);
        stream.Write(type);
        stream.Write(data);

        var crc = new Crc32();
        crc.Append(type);
        crc.Append(data);
        BinaryPrimitives.WriteUInt32BigEndian(buf4, crc.GetCurrentHashAsUInt32());
        stream.Write(buf4);
    }

    /// <summary>
    /// Compute pixel dimensions, resolution, and coordinate offsets from a set of nodes.
    /// </summary>
    internal static (int Width, int Height, float Resolution, float XOffset, float YOffset) ComputeMapGeometry(
        IReadOnlyList<GraphNode> nodes)
    {
        if (nodes.Count == 0)
        {
            return (FallbackExtent, FallbackExtent, FallbackResolution, 0f, 0f);
        }

        float minX = nodes.Min(n => n.X);
        float maxX = nodes.Max(n => n.X);
        float minY = nodes.Min(n => n.Y);
        float maxY = nodes.Max(n => n.Y);

        float rangeX = maxX - minX;
        float rangeY = maxY - minY;

        float marginX = Math.Max(MarginMeters, rangeX * MarginFraction);
        float marginY = Math.Max(MarginMeters, rangeY * MarginFraction);

        float totalX = rangeX + 2 * marginX;
        float totalY = rangeY + 2 * marginY;

        float longestSide = Math.Max(totalX, totalY);
        float resolution = Math.Max(MinResolution, longestSide / TargetMaxPixels);

        int width = Math.Max(MinPixelDimension, (int)Math.Ceiling(totalX / resolution));
        int height = Math.Max(MinPixelDimension, (int)Math.Ceiling(totalY / resolution));

        float xOffset = marginX - minX;
        float yOffset = marginY - minY;

        return (width, height, resolution, xOffset, yOffset);
    }

    /// <summary>
    /// Generate a white PNG of the specified dimensions and return it as a Base64 string.
    /// </summary>
    internal static string GenerateWhitePngBase64(int width, int height)
    {
        using var output = new MemoryStream();

        output.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        byte[] ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = 8;
        ihdr[9] = 2;
        WritePngChunk(output, "IHDR"u8, ihdr);

        byte[] firstRow = new byte[1 + width * 3];
        firstRow[0] = 0;
        Array.Fill(firstRow, (byte)0xFF, 1, width * 3);

        byte[] subsequentRow = new byte[1 + width * 3];
        subsequentRow[0] = 2;

        byte[] compressedData;
        using (var compressedStream = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(firstRow);
                for (int y = 1; y < height; y++)
                {
                    zlib.Write(subsequentRow);
                }
            }

            compressedData = compressedStream.ToArray();
        }

        WritePngChunk(output, "IDAT"u8, compressedData);

        WritePngChunk(output, "IEND"u8, []);

        return Convert.ToBase64String(output.ToArray());
    }
}