// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Chordious.Desktop.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

namespace Chordious.DesktopTest;

[TestClass]
public class DiagramFileExporterTest
{
    private const string TestSvg = """
        <svg xmlns="http://www.w3.org/2000/svg" width="40" height="20">
          <rect width="40" height="20" fill="#336699"/>
        </svg>
        """;

    [TestMethod]
    public async Task ExportAsync_WritesEverySupportedFormatWithExpectedDimensions()
    {
        string testDirectory = CreateTestDirectory();
        try
        {
            Dictionary<DiagramExportFormat, byte[]> signatures = new()
            {
                [DiagramExportFormat.SVG] = Encoding.UTF8.GetBytes("<svg"),
                [DiagramExportFormat.PNG] = [0x89, 0x50, 0x4E, 0x47],
                [DiagramExportFormat.GIF] = [0x47, 0x49, 0x46, 0x38],
                [DiagramExportFormat.JPG] = [0xFF, 0xD8, 0xFF]
            };

            foreach ((DiagramExportFormat format, byte[] signature) in signatures)
            {
                string path = Path.Combine(
                    testDirectory,
                    $"diagram.{format.ToString().ToLowerInvariant()}");
                float scale = format == DiagramExportFormat.SVG ? 1.0f : 2.0f;

                await DiagramFileExporter.ExportAsync(
                    TestSvg,
                    40,
                    20,
                    format,
                    scale,
                    path);

                byte[] content = await File.ReadAllBytesAsync(path);
                Assert.IsTrue(content.Length > signature.Length);
                CollectionAssert.AreEqual(signature, content[..signature.Length]);

                if (format != DiagramExportFormat.SVG)
                {
                    using SKBitmap bitmap = SKBitmap.Decode(content);
                    Assert.IsNotNull(bitmap);
                    Assert.AreEqual(80, bitmap.Width);
                    Assert.AreEqual(40, bitmap.Height);
                }
            }
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void GetMaxScaleFactor_RejectsInvalidDimensions()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => DiagramFileExporter.GetMaxScaleFactor(0, 20));
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => DiagramFileExporter.GetMaxScaleFactor(40, 0));
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"Chordious-export-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
