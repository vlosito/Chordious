// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;

using Chordious.Core;
using Chordious.Desktop.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

namespace Chordious.DesktopTest;

[TestClass]
public class SvgRasterizerTest
{
    [TestMethod]
    public void RenderPng_RendersRealChordiousDiagram()
    {
        AppInfo.Assembly = typeof(SvgRasterizerTest).Assembly;

        Diagram diagram = new(ConfigFile.DefaultConfig.DiagramStyle, 6, 5)
        {
            Title = "C"
        };
        diagram.NewMark(new MarkPosition(2, 1));
        diagram.NewMark(new MarkPosition(4, 2));
        diagram.NewMark(new MarkPosition(5, 3));

        string svg = diagram.ToImageMarkup(ImageMarkupType.SVG);
        byte[] png = SvgRasterizer.RenderPng(
            svg,
            (int)Math.Ceiling(diagram.TotalWidth()),
            (int)Math.Ceiling(diagram.TotalHeight()));

        Assert.IsTrue(png.Length > 8);
        CollectionAssert.AreEqual(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            png[..8]);
    }

    [TestMethod]
    public void RenderPng_RejectsEmptySvg()
    {
        Assert.ThrowsException<ArgumentException>(() => SvgRasterizer.RenderPng("", 100, 100));
    }

    [DataTestMethod]
    [DataRow((int)RasterImageFormat.Png, new byte[] { 0x89, 0x50, 0x4E, 0x47 })]
    [DataRow((int)RasterImageFormat.Gif, new byte[] { 0x47, 0x49, 0x46, 0x38 })]
    [DataRow((int)RasterImageFormat.Jpg, new byte[] { 0xFF, 0xD8, 0xFF })]
    public void RenderImage_EncodesSupportedRasterFormats(
        int formatValue,
        byte[] expectedSignature)
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" width="40" height="20">
              <rect width="40" height="20" fill="#336699"/>
            </svg>
            """;

        byte[] image = SvgRasterizer.RenderImage(
            svg,
            80,
            40,
            (RasterImageFormat)formatValue);

        Assert.IsTrue(image.Length > expectedSignature.Length);
        CollectionAssert.AreEqual(expectedSignature, image[..expectedSignature.Length]);
    }

    [TestMethod]
    public void RenderImage_UsesTransparentPngAndWhiteJpgBackgrounds()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" width="40" height="20">
              <circle cx="20" cy="10" r="5" fill="#336699"/>
            </svg>
            """;

        using SKBitmap png = SKBitmap.Decode(
            SvgRasterizer.RenderImage(svg, 40, 20, RasterImageFormat.Png));
        using SKBitmap jpg = SKBitmap.Decode(
            SvgRasterizer.RenderImage(svg, 40, 20, RasterImageFormat.Jpg));

        Assert.AreEqual(0, png.GetPixel(0, 0).Alpha);
        SKColor jpgCorner = jpg.GetPixel(0, 0);
        Assert.IsTrue(jpgCorner.Red >= 250);
        Assert.IsTrue(jpgCorner.Green >= 250);
        Assert.IsTrue(jpgCorner.Blue >= 250);
    }

    [TestMethod]
    public void RenderPng_AppliesConfiguredPreviewBackground()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" width="40" height="20">
              <circle cx="20" cy="10" r="5" fill="#336699"/>
            </svg>
            """;

        using SKBitmap none = SKBitmap.Decode(
            SvgRasterizer.RenderPng(svg, 40, 20, PreviewBackground.None));
        using SKBitmap white = SKBitmap.Decode(
            SvgRasterizer.RenderPng(svg, 40, 20, PreviewBackground.White));
        using SKBitmap transparency = SKBitmap.Decode(
            SvgRasterizer.RenderPng(svg, 40, 20, PreviewBackground.Transparent));

        Assert.AreEqual(0, none.GetPixel(0, 0).Alpha);
        Assert.AreEqual(SKColors.White, white.GetPixel(0, 0));
        Assert.AreEqual(new SKColor(255, 255, 255), transparency.GetPixel(0, 0));
        Assert.AreEqual(new SKColor(224, 224, 224), transparency.GetPixel(16, 0));
    }
}
