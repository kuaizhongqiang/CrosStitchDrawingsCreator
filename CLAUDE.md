# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**CrosStitchDrawingsCreator** (十字绣图纸制作工具) — a Windows 10/11 desktop application that converts imported images into cross-stitch patterns. Users import an image, configure parameters (color count, output dimensions), and the app generates a pixelated cross-stitch chart with DMC thread color mapping, preview, and PNG export.

**Core workflow:** `Import Image → Set Parameters → Generate Pattern → Preview → Export PNG`

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Language | C# (.NET 8) |
| UI Framework | WPF with custom styles (modern Fluent Design-inspired) |
| Image Processing | SkiaSharp or direct bitmap manipulation |
| Publishing | `dotnet publish -c Release --self-contained -r win-x64` |
| Distribution | Single folder, copy-to-run (no runtime dependency) |

> **Important distinction:** REQUIREMENTS.md is the authoritative spec (C# .NET 8 + WPF, confirmed decision). The CODEBUDDY.md file contains an outdated Python proposal — do not follow it.

## Project Structure

```
CrosStitchDrawingsCreator/
├── src/                    # Application source code
│   ├── UI/                 # WPF views, styles, view models
│   │   ├── Views/          # XAML windows and user controls
│   │   ├── ViewModels/     # MVVM view models
│   │   └── Styles/         # Global styles, themes, templates
│   ├── Core/               # Business logic (no WPF dependency)
│   │   ├── ImageProcessing/# Image loading, resize, quantization
│   │   ├── Quantization/   # K-Means clustering, Floyd-Steinberg dithering
│   │   ├── DmcMapping/     # DMC thread color matching (450+ colors)
│   │   └── Models/         # Data models (PatternData, Palette, etc.)
│   ├── IO/                 # I/O operations
│   │   ├── Export/         # PNG export with grid, legend, scaling
│   │   └── Config/         # Settings persistence (export path, preferences)
│   └── Resources/          # Embedded resources (DMC color JSON, icons)
├── tests/                  # Unit tests (xUnit)
├── REQUIREMENTS.md         # Full functional and non-functional spec
├── README.md
└── CLAUDE.md
```

## Architecture

### Layered Architecture (MVVM for UI)

**Layer 1 — UI (`src/UI/`)** — WPF views and view models using MVVM pattern (CommunityToolkit.Mvvm). Main window with 3-column layout: left parameter panel, center preview canvas, right color legend. Custom WindowChrome for title bar. All controls use global styles — never default WPF look.

**Layer 2 — Core (`src/Core/`)** — Pure business logic with no WPF dependency, fully unit-testable. The pipeline: import → resize (LANCZOS) → quantize to N colors (K-Means) → map to nearest DMC thread → build pixel grid + palette.

**Layer 3 — I/O (`src/IO/`)** — File loading, PNG export (configurable scale 1-10×, with/without grid, with/without legend), user setting persistence.

### Data Flow
```
Image file → ImageLoader → ImageProcessor.Resize() → ImageProcessor.Quantize()
  → DmcMapper (match palette to DMC colors) → PatternData
  → PreviewCanvas renders grid + ColorLegendPanel renders palette
  → PatternExporter saves PNG
```

### Key Data Model
```
PatternData
├── PixelGrid        (int[,] — color index per stitch)
├── Palette          (index → Color → DMC code + name + stitch count)
├── Dimensions       (width × height in stitches)
└── SourceMetadata   (original filename, dimensions)
```

## Commands

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Run
dotnet run --project src/CrosStitchDrawingsCreator.csproj

# Run tests (all)
dotnet test

# Run a single test class/file
dotnet test --filter FullyQualifiedName~TestClassName

# Publish self-contained for distribution
dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true

# Add a NuGet package
dotnet add src/CrosStitchDrawingsCreator.csproj package PackageName
```

## DMC Color Library

Built-in DMC thread color database (~450+ colors) stored as an embedded JSON resource (`Resources/dmc_colors.json`). Each entry: DMC code, color name, RGB values. Color matching uses Euclidean distance in RGB space.

## Development Guidelines

- **UI must look commercial-grade**: No default WPF styles — override everything via `Style` and `ControlTemplate`. Use 6-8px border radius, subtle shadows, consistent 16-24px spacing, smooth transitions.
- **Background threading**: All image processing runs on background threads — UI never blocks. Use `async/await` or `Task.Run`.
- **Self-contained publish only**: Never rely on system-installed .NET runtime. Everything must compile with `--self-contained`.
- **High DPI**: Test at 125%/150%/175%/200% scaling. Use `Window.DpiChanged` and proper WPF DPI scaling.
- **Target**: Windows 10 (1809+) and Windows 11 only.
- **Deep theme support**: Light and dark themes, default follows system.
- **UI language**: Simplified Chinese.

## Key NuGet Dependencies (expected)

- `CommunityToolkit.Mvvm` — MVVM framework
- `SkiaSharp` — high-quality image processing
- `HandyControl` or `ModernWpf` — accelerated WPF theming (optional)
- `xUnit` — testing

## Phase Plan

| Phase | Focus |
|-------|-------|
| 1 — MVP | Project skeleton + modern UI + image import + parameter panel + basic generation (resize+quantize) + PNG export |
| 2 — Full | DMC mapping + color legend + preview zoom/pan + grid rendering + Floyd-Steinberg dithering |
| 3 — Polish | Dark theme + settings persistence + drag-drop + clipboard import + export options |
| 4 — Advanced | Color highlight interaction + fabric calculator + symbol overlay + auto-update |
