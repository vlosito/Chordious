// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Chordious.Desktop.Services;

internal static class DiagramFileExporter
{
    private const int MaxBitmapDimension = 32768;

    public static float GetMaxScaleFactor(int width, int height)
    {
        ValidateBaseDimensions(width, height);

        float maxByDimension = MaxBitmapDimension / (float)Math.Max(width, height);
        float maxByMemory = (float)Math.Sqrt(
            MaxBitmapDimension * (MaxBitmapDimension / (width * (double)height)));

        return Math.Min(maxByDimension, maxByMemory);
    }

    public static async Task ExportAsync(
        string svgText,
        int width,
        int height,
        DiagramExportFormat format,
        float scaleFactor,
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(svgText))
        {
            throw new ArgumentException("SVG content is required.", nameof(svgText));
        }

        ValidateBaseDimensions(width, height);

        if (scaleFactor <= 0 ||
            (format != DiagramExportFormat.SVG &&
             scaleFactor > GetMaxScaleFactor(width, height)))
        {
            throw new ArgumentOutOfRangeException(nameof(scaleFactor));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("An output file is required.", nameof(filePath));
        }

        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (format == DiagramExportFormat.SVG)
        {
            await File.WriteAllTextAsync(filePath, svgText, new UTF8Encoding(false));
            return;
        }

        int scaledWidth = Math.Max(1, (int)Math.Ceiling(width * scaleFactor));
        int scaledHeight = Math.Max(1, (int)Math.Ceiling(height * scaleFactor));
        RasterImageFormat rasterFormat = format switch
        {
            DiagramExportFormat.PNG => RasterImageFormat.Png,
            DiagramExportFormat.GIF => RasterImageFormat.Gif,
            DiagramExportFormat.JPG => RasterImageFormat.Jpg,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        byte[] image = await Task.Run(
            () => SvgRasterizer.RenderImage(svgText, scaledWidth, scaledHeight, rasterFormat));
        await File.WriteAllBytesAsync(filePath, image);
    }

    private static void ValidateBaseDimensions(int width, int height)
    {
        if (width <= 0 || width > MaxBitmapDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0 || height > MaxBitmapDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
    }
}

internal enum DiagramExportFormat
{
    SVG = 0,
    PNG,
    GIF,
    JPG
}
