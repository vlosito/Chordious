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
        return RenderPng(svgText, width, height, PreviewBackground.None);
    }

    public static byte[] RenderPng(
        string svgText,
        int width,
        int height,
        PreviewBackground background)
    {
        return RenderImage(svgText, width, height, RasterImageFormat.Png, background);
    }

    public static byte[] RenderImage(
        string svgText,
        int width,
        int height,
        RasterImageFormat format)
    {
        PreviewBackground background = format == RasterImageFormat.Jpg
            ? PreviewBackground.White
            : PreviewBackground.None;
        return RenderImage(svgText, width, height, format, background);
    }

    private static byte[] RenderImage(
        string svgText,
        int width,
        int height,
        RasterImageFormat format,
        PreviewBackground background)
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
        DrawBackground(canvas, width, height, background);

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

    private static void DrawBackground(
        SKCanvas canvas,
        int width,
        int height,
        PreviewBackground background)
    {
        canvas.Clear(background == PreviewBackground.White
            ? SKColors.White
            : SKColors.Transparent);

        if (background != PreviewBackground.Transparent)
        {
            return;
        }

        const int tileSize = 16;
        using SKPaint light = new() { Color = new SKColor(255, 255, 255) };
        using SKPaint dark = new() { Color = new SKColor(224, 224, 224) };

        for (int y = 0; y < height; y += tileSize)
        {
            for (int x = 0; x < width; x += tileSize)
            {
                SKPaint paint = ((x / tileSize) + (y / tileSize)) % 2 == 0
                    ? light
                    : dark;
                canvas.DrawRect(x, y, tileSize, tileSize, paint);
            }
        }
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

internal enum PreviewBackground
{
    None,
    White,
    Transparent
}
