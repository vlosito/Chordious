// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.IO;

using SkiaSharp;
using Svg.Skia;

using SixLabors.ImageSharp.Formats.Gif;

namespace Chordious.Desktop.Services;

internal static class SvgRasterizer
{
    public static byte[] RenderPng(string svgText, int width, int height)
    {
        return RenderImage(svgText, width, height, RasterImageFormat.Png);
    }

    public static byte[] RenderImage(
        string svgText,
        int width,
        int height,
        RasterImageFormat format)
    {
        if (string.IsNullOrWhiteSpace(svgText))
        {
            throw new ArgumentException("SVG content is required.", nameof(svgText));
        }

        width = Math.Max(1, width);
        height = Math.Max(1, height);

        using SKSvg svg = new();
        SKPicture picture = svg.FromSvg(svgText)
            ?? throw new InvalidOperationException("The SVG content could not be rendered.");

        SKRect source = picture.CullRect;
        if (source.Width <= 0 || source.Height <= 0)
        {
            throw new InvalidOperationException("The SVG content has invalid dimensions.");
        }

        using SKSurface surface = SKSurface.Create(new SKImageInfo(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul));

        SKCanvas canvas = surface.Canvas;
        canvas.Clear(format == RasterImageFormat.Jpg ? SKColors.White : SKColors.Transparent);

        float scale = Math.Min(width / source.Width, height / source.Height);
        float offsetX = (width - (source.Width * scale)) / 2f;
        float offsetY = (height - (source.Height * scale)) / 2f;

        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale);
        canvas.Translate(-source.Left, -source.Top);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using SKImage image = surface.Snapshot();
        if (format == RasterImageFormat.Gif)
        {
            return EncodeGif(image);
        }

        SKEncodedImageFormat encodedFormat = format switch
        {
            RasterImageFormat.Png => SKEncodedImageFormat.Png,
            RasterImageFormat.Jpg => SKEncodedImageFormat.Jpeg,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        using SKData data = image.Encode(encodedFormat, 100)
            ?? throw new InvalidOperationException(
                $"The rendered SVG could not be encoded as {format.ToString().ToUpperInvariant()}.");

        return data.ToArray();
    }

    private static byte[] EncodeGif(SKImage image)
    {
        using SKData pngData = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("The rendered SVG could not be prepared for GIF encoding.");
        using SixLabors.ImageSharp.Image gifImage =
            SixLabors.ImageSharp.Image.Load(pngData.ToArray());
        using MemoryStream output = new();
        gifImage.Save(output, new GifEncoder());
        return output.ToArray();
    }
}

internal enum RasterImageFormat
{
    Png,
    Gif,
    Jpg
}
