// Copyright (c) Jon Thysell <http://jonthysell.com>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Chordious.Core.ViewModel;

namespace Chordious.Desktop.Services;

internal sealed class DiagramExportPathBuilder
{
    private readonly HashSet<string> _createdFiles = new(StringComparer.OrdinalIgnoreCase);

    public void Reset()
    {
        _createdFiles.Clear();
    }

    public string BuildPath(
        string outputPath,
        string filenameFormat,
        string collectionName,
        IReadOnlyList<ObservableDiagram> diagrams,
        int diagramIndex,
        DiagramExportFormat exportFormat,
        bool overwriteFiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(filenameFormat);
        ArgumentNullException.ThrowIfNull(diagrams);

        if (diagramIndex < 0 || diagramIndex >= diagrams.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(diagramIndex));
        }

        ObservableDiagram diagram = diagrams[diagramIndex];
        string expanded = ExpandFormat(
            filenameFormat,
            CleanSegment(collectionName),
            CleanSegment(diagram.Title),
            diagram.TotalWidth,
            diagram.TotalHeight,
            diagramIndex,
            diagrams.Count,
            exportFormat);

        string relativePath = NormalizeRelativePath(expanded);
        string requestedPath = Path.GetFullPath(Path.Combine(outputPath, relativePath));
        string outputRoot = Path.GetFullPath(outputPath);
        if (!requestedPath.StartsWith(
                outputRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.Ordinal) &&
            !requestedPath.Equals(outputRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The filename format points outside the output folder.");
        }

        string directory = Path.GetDirectoryName(requestedPath) ?? outputRoot;
        string fileName = Path.GetFileNameWithoutExtension(requestedPath);
        string extension = Path.GetExtension(requestedPath);
        string candidate = requestedPath;
        int attempt = 1;

        while (File.Exists(candidate) &&
               (_createdFiles.Contains(candidate) || !overwriteFiles))
        {
            candidate = Path.Combine(directory, $"{fileName} ({attempt}){extension}");
            attempt++;
        }

        while (_createdFiles.Contains(candidate))
        {
            candidate = Path.Combine(directory, $"{fileName} ({attempt}){extension}");
            attempt++;
        }

        return candidate;
    }

    public void MarkCreated(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _createdFiles.Add(Path.GetFullPath(filePath));
    }

    private static string ExpandFormat(
        string filenameFormat,
        string collectionName,
        string title,
        int width,
        int height,
        int diagramIndex,
        int diagramCount,
        DiagramExportFormat exportFormat)
    {
        StringBuilder result = new();
        for (int i = 0; i < filenameFormat.Length; i++)
        {
            char current = filenameFormat[i];
            if (current != '%' || i + 1 >= filenameFormat.Length)
            {
                result.Append(current);
                continue;
            }

            char token = filenameFormat[++i];
            result.Append(token switch
            {
                't' => title,
                'c' => collectionName,
                'h' => height.ToString(),
                'w' => width.ToString(),
                '0' => diagramIndex.ToString(),
                '1' => (diagramIndex + 1).ToString(),
                '#' => diagramCount.ToString(),
                'x' => exportFormat.ToString().ToLowerInvariant(),
                'X' => exportFormat.ToString(),
                '%' => "%",
                _ => string.Empty
            });
        }

        return result.ToString();
    }

    private static string NormalizeRelativePath(string path)
    {
        string normalized = path
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        string[] segments = normalized.Split(
            Path.DirectorySeparatorChar,
            StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            throw new InvalidOperationException("The filename format did not produce a file name.");
        }

        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = CleanSegment(segments[i]);
        }

        return Path.Combine(segments);
    }

    private static string CleanSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        HashSet<char> invalidChars = new(Path.GetInvalidFileNameChars())
        {
            '<', '>', ':', '"', '/', '\\', '|', '?', '*'
        };

        StringBuilder result = new();
        foreach (char character in value.Trim())
        {
            if (!invalidChars.Contains(character))
            {
                result.Append(character);
            }
        }

        return result.ToString();
    }
}
