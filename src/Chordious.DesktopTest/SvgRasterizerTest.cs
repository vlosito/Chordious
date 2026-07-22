// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;

using Chordious.Core;
using Chordious.Desktop.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

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
}
