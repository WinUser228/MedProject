using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MedProject.Services.Service;

/// <summary>
/// PDFsharp 6 (Core) does not resolve system fonts by default. MigraDoc needs Arial and a fixed
/// "error" font (Courier New). Windows: enable built-in GDI resolver. Linux/macOS: map to TTF files.
/// Must run once before any PDF render. See https://docs.pdfsharp.net/link/font-resolving.html
/// </summary>
public static class PdfFontSetup
{
    private static readonly object InitLock = new();
    private static bool _initialized;

    public static void Ensure()
    {
        if (_initialized) return;
        lock (InitLock)
        {
            if (_initialized) return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WSL_DISTRO_NAME")))
            {
                GlobalFontSettings.UseWindowsFontsUnderWsl2 = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                GlobalFontSettings.FontResolver = new UnixSystemFontResolver(isMacOs: true);
            }
            else
            {
                GlobalFontSettings.FontResolver = new UnixSystemFontResolver(isMacOs: false);
            }

            _initialized = true;
        }
    }
}

/// <summary>Resolves standard PDF font families to common open fonts shipped with most Linux distros.</summary>
internal sealed class UnixSystemFontResolver : IFontResolver
{
    private readonly bool _isMacOs;

    public UnixSystemFontResolver(bool isMacOs) => _isMacOs = isMacOs;

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        var candidates = MapFamilyToCandidates(familyName, bold, italic).ToArray();
        var path = FindExistingPath(candidates) ?? FindByFilenameInKnownFontDirs(candidates.Select(Path.GetFileName).Where(n => !string.IsNullOrWhiteSpace(n))!);
        return path == null ? null : new FontResolverInfo(path);
    }

    public byte[] GetFont(string faceName) => File.ReadAllBytes(faceName);

    private IEnumerable<string> MapFamilyToCandidates(string familyName, bool bold, bool italic)
    {
        var name = familyName.Trim();

        if (name.Equals("Courier New", StringComparison.OrdinalIgnoreCase))
            return MonoCandidates(bold, italic);

        if (name.Equals("Arial", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Helvetica", StringComparison.OrdinalIgnoreCase))
            return SansCandidates(bold, italic);

        if (name.Equals("Times New Roman", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Times", StringComparison.OrdinalIgnoreCase))
            return SerifCandidates(bold, italic);

        return SansCandidates(bold, italic);
    }

    private IEnumerable<string> SansCandidates(bool bold, bool italic)
    {
        if (_isMacOs)
        {
            yield return "/Library/Fonts/Arial.ttf";
            yield return "/System/Library/Fonts/Supplemental/Arial.ttf";
            yield return "/System/Library/Fonts/Supplemental/Arial Bold.ttf";
        }

        // Prefer matching style first
        foreach (var s in StyleOrderedNames(
            regular: "LiberationSans-Regular.ttf",
            boldName: "LiberationSans-Bold.ttf",
            italicName: "LiberationSans-Italic.ttf",
            boldItalicName: "LiberationSans-BoldItalic.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/liberation/{s}";
            yield return $"/usr/share/fonts/truetype/liberation2/{s}";
        }

        foreach (var s in StyleOrderedNames(
            regular: "DejaVuSans.ttf",
            boldName: "DejaVuSans-Bold.ttf",
            italicName: "DejaVuSans-Oblique.ttf",
            boldItalicName: "DejaVuSans-BoldOblique.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/dejavu/{s}";
            yield return $"/usr/share/fonts/TTF/{s}";
        }

        // Noto is common on many distros
        foreach (var s in StyleOrderedNames(
            regular: "NotoSans-Regular.ttf",
            boldName: "NotoSans-Bold.ttf",
            italicName: "NotoSans-Italic.ttf",
            boldItalicName: "NotoSans-BoldItalic.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/noto/{s}";
            yield return $"/usr/share/fonts/noto/{s}";
        }

        // FreeFont fallback (sometimes present in minimal installs)
        foreach (var s in StyleOrderedNames(
            regular: "FreeSans.ttf",
            boldName: "FreeSansBold.ttf",
            italicName: "FreeSansOblique.ttf",
            boldItalicName: "FreeSansBoldOblique.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/freefont/{s}";
        }
    }

    private IEnumerable<string> MonoCandidates(bool bold, bool italic)
    {
        if (_isMacOs)
        {
            yield return "/System/Library/Fonts/Supplemental/Courier New.ttf";
            yield return "/Library/Fonts/Courier New.ttf";
        }

        foreach (var s in StyleOrderedNames(
            regular: "LiberationMono-Regular.ttf",
            boldName: "LiberationMono-Bold.ttf",
            italicName: "LiberationMono-Italic.ttf",
            boldItalicName: "LiberationMono-BoldItalic.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/liberation/{s}";
            yield return $"/usr/share/fonts/truetype/liberation2/{s}";
        }

        foreach (var s in StyleOrderedNames(
            regular: "DejaVuSansMono.ttf",
            boldName: "DejaVuSansMono-Bold.ttf",
            italicName: "DejaVuSansMono-Oblique.ttf",
            boldItalicName: "DejaVuSansMono-BoldOblique.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/dejavu/{s}";
            yield return $"/usr/share/fonts/TTF/{s}";
        }

        foreach (var s in StyleOrderedNames(
            regular: "NotoSansMono-Regular.ttf",
            boldName: "NotoSansMono-Bold.ttf",
            italicName: "NotoSansMono-Regular.ttf",
            boldItalicName: "NotoSansMono-Bold.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/noto/{s}";
            yield return $"/usr/share/fonts/noto/{s}";
        }

        foreach (var s in StyleOrderedNames(
            regular: "FreeMono.ttf",
            boldName: "FreeMonoBold.ttf",
            italicName: "FreeMonoOblique.ttf",
            boldItalicName: "FreeMonoBoldOblique.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/freefont/{s}";
        }
    }

    private IEnumerable<string> SerifCandidates(bool bold, bool italic)
    {
        if (_isMacOs)
        {
            yield return "/Library/Fonts/Times New Roman.ttf";
            yield return "/System/Library/Fonts/Supplemental/Times New Roman.ttf";
        }

        foreach (var s in StyleOrderedNames(
            regular: "LiberationSerif-Regular.ttf",
            boldName: "LiberationSerif-Bold.ttf",
            italicName: "LiberationSerif-Italic.ttf",
            boldItalicName: "LiberationSerif-BoldItalic.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/liberation/{s}";
            yield return $"/usr/share/fonts/truetype/liberation2/{s}";
        }

        foreach (var s in StyleOrderedNames(
            regular: "DejaVuSerif.ttf",
            boldName: "DejaVuSerif-Bold.ttf",
            italicName: "DejaVuSerif-Italic.ttf",
            boldItalicName: "DejaVuSerif-BoldItalic.ttf",
            bold, italic))
        {
            yield return $"/usr/share/fonts/truetype/dejavu/{s}";
            yield return $"/usr/share/fonts/TTF/{s}";
        }
    }

    private static string? FindExistingPath(IEnumerable<string> candidates)
    {
        foreach (var p in candidates)
        {
            if (File.Exists(p))
                return p;
        }

        return null;
    }

    private static IEnumerable<string> StyleOrderedNames(string regular, string boldName, string italicName, string boldItalicName, bool bold, bool italic)
    {
        if (bold && italic)
        {
            yield return boldItalicName;
            yield return boldName;
            yield return italicName;
            yield return regular;
            yield break;
        }
        if (bold)
        {
            yield return boldName;
            yield return regular;
            yield return boldItalicName;
            yield return italicName;
            yield break;
        }
        if (italic)
        {
            yield return italicName;
            yield return regular;
            yield return boldItalicName;
            yield return boldName;
            yield break;
        }

        yield return regular;
        yield return boldName;
        yield return italicName;
        yield return boldItalicName;
    }

    private static string? FindByFilenameInKnownFontDirs(IEnumerable<string> fileNames)
    {
        // This is a best-effort fallback for distros where fonts live in different subfolders.
        // We keep it shallow to avoid expensive scans.
        var dirs = new[]
        {
            "/usr/share/fonts",
            "/usr/local/share/fonts",
            "/usr/share/fonts/truetype",
            "/usr/share/fonts/opentype",
            "/usr/share/fonts/TTF",
        };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
                continue;

            foreach (var fileName in fileNames)
            {
                var p = TryFindInDirectory(dir, fileName, maxDepth: 3);
                if (p != null)
                    return p;
            }
        }

        return null;
    }

    private static string? TryFindInDirectory(string root, string fileName, int maxDepth)
    {
        try
        {
            if (maxDepth < 0)
                return null;

            var direct = Path.Combine(root, fileName);
            if (File.Exists(direct))
                return direct;

            foreach (var sub in Directory.EnumerateDirectories(root))
            {
                var found = TryFindInDirectory(sub, fileName, maxDepth - 1);
                if (found != null)
                    return found;
            }
        }
        catch
        {
            // ignore permission errors and keep trying other dirs
        }

        return null;
    }
}
