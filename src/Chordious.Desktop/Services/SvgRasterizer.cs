// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;

using SkiaSharp;
using Svg.Skia;

namespace Chordious.Desktop.Services;

internal static class SvgRasterizer
{
    public static byte[] RenderPng(string svgText, int width, int height)
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
        canvas.Clear(SKColors.Transparent);

        float scale = Math.Min(width / source.Width, height / source.Height);
        float offsetX = (width - (source.Width * scale)) / 2f;
        float offsetY = (height - (source.Height * scale)) / 2f;

        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale);
        canvas.Translate(-source.Left, -source.Top);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using SKImage image = surface.Snapshot();
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("The rendered SVG could not be encoded as PNG.");

        return data.ToArray();
    }
}
