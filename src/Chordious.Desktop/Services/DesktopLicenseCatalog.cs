// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.IO;

using Chordious.Core;
using Chordious.Core.ViewModel;

namespace Chordious.Desktop.Services;

internal static class DesktopLicenseCatalog
{
    private const string ImageSharpLicenseResource =
        "Chordious.Desktop.Resources.ImageSharp.LICENSE.txt";

    public static void AddDesktopDependencies(LicensesViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        viewModel.Licenses.Add(new ObservableLicense(
            "Avalonia",
            "Copyright 2013-2026 © The AvaloniaUI Project",
            AppInfo.MitLicenseName,
            AppInfo.MitLicenseBody));
        viewModel.Licenses.Add(new ObservableLicense(
            "Svg.Skia",
            "Copyright © Wiesław Šoltés 2026",
            AppInfo.MitLicenseName,
            AppInfo.MitLicenseBody));
        viewModel.Licenses.Add(new ObservableLicense(
            "SixLabors.ImageSharp",
            "Copyright © Six Labors",
            "Six Labors Split License",
            ReadEmbeddedText(ImageSharpLicenseResource)));
    }

    private static string ReadEmbeddedText(string resourceName)
    {
        using Stream stream = typeof(DesktopLicenseCatalog).Assembly
            .GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded license {resourceName} was not found.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
