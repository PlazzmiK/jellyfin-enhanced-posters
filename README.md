<div align="center">

<img src="Assets/Logo.png" alt="Enhanced Posters Logo" height="160px" />

# Enhanced Posters

[![Release](https://img.shields.io/github/v/release/PlazzmiK/jellyfin-enhanced-posters?style=flat-square&logo=github&logoColor=F43F5E&label=Release&color=F43F5E)](https://github.com/PlazzmiK/jellyfin-enhanced-posters/releases/latest)
[![Pipeline](https://img.shields.io/github/actions/workflow/status/PlazzmiK/jellyfin-enhanced-posters/CI.yml?branch=master&style=flat-square&logo=githubactions&logoColor=8B5CF6&label=Pipeline&color=8B5CF6)](https://github.com/PlazzmiK/jellyfin-enhanced-posters/actions/workflows/CI.yml)
[![License](https://img.shields.io/github/license/PlazzmiK/jellyfin-enhanced-posters?style=flat-square&label=License&color=14B8A6&logo=opensourceinitiative&logoColor=14B8A6)](./LICENSE.md)

**Enhanced Posters** is a Jellyfin plugin that non-destructively enriches movie and show posters with media info badges (4K, Dolby Vision, HDR, audio codecs), Edition badges (IMAX, Extended), 3D badges, and customizable score-tiered rating pills using high-performance vector and raster compositing powered by **SkiaSharp**.

</div>

**Minimum Jellyfin version:** 10.11.10

---

## Features

- **Non-Destructive Poster Preservation & Auto Refresh:**
  - Untouched original posters are saved alongside your media files as `poster-original.jpg`.
  - **Automatic Artwork Refresh Detection:** When you refresh metadata or pick a new poster via *Edit Images* in Jellyfin, the plugin automatically captures the new base image and reapplies the overlays seamlessly.
- **Unified Dark Translucent Pill Containers:**
  - Badges sharing the same anchor (e.g. 4K + Dolby Vision + 3D + IMAX) are grouped inside a single sleek dark translucent pill container.
  - Configurable background color, opacity slider (0–100%), corner radius, and padding.
- **Edition Badges:**
  - Automatically detected from filename tags like `{edition-Imax}`, `{edition-Extended}`, `{edition-Directors Cut}`, and keywords.
  - Dedicated badges for IMAX, Extended Cut, Director's Cut, Theatrical, Unrated, Special Edition, Remastered, and custom edition tags.
- **3D Version Badge:**
  - Displays a stylish 3D glasses badge for 3D media, detected via `{edition-3D}`, `[3D]`, filename tags (SBS/TAB/MVC), or Jellyfin 3D video stream properties.
- **Selective Media Badges:**
  - **Resolution:** Independently toggle badges for `4K UHD`, `1080p FHD`, `720p HD`, and `SD`.
  - **Video Range / HDR:** Badges for `Dolby Vision (DV)`, `HDR10+`, `HDR10`, `HDR`, and `HLG`.
  - **Audio Codecs:** Optional badges for `Dolby Atmos`, `DTS:X`, `TrueHD`, `DTS-HD MA`, and `FLAC`.
- **Dynamic Rating Pill Badges:**
  - Snug rounded rating pill matching media badge height with anti-aliasing and drop shadows.
  - **Score-based Color Tiers:**
    - `< 5.0`: **Red** (`#E23133`)
    - `5.0 – 5.9`: **Orange** (`#EF7C2A`)
    - `6.0 – 6.9`: **Yellow** (`#F5C518`)
    - `7.0 – 7.9`: **Green** (`#5CB85C`)
    - `≥ 8.0`: **Blue / Diamond** (`#5BC4F0`)
- **Interactive 3x3 Graphical Anchor Pickers:**
  - Visual 9-position button matrix (`TL`, `TC`, `TR`, `CL`, `C`, `CR`, `BL`, `BC`, `BR`) for one-click placement of all badge categories with instant live visual preview.
- **In-Dashboard Custom Badge Asset Manager:**
  - Upload custom PNG, WebP, or SVG badge files directly from your browser to replace standard badge styles (4K, DV, HDR, 3D glasses, IMAX, etc.).
- **Smart Re-Render Optimization:**
  - Automatically skips items if configuration, media info, and source posters have not changed.
- **1-Click Restore Task:**
  - Revert all posters back to pristine backups at any time via the **Restore Original Posters** scheduled task.

---

## Repository URL (for Jellyfin)

To add Enhanced Posters to your Jellyfin server:

1. Open **Dashboard &rarr; Plugins &rarr; Repositories**.
2. Click **Add Repository** (`+`).
3. Enter:
   - **Name:** `Enhanced Posters`
   - **URL:**
     ```text
     https://raw.githubusercontent.com/PlazzmiK/jellyfin-enhanced-posters/master/manifest.json
     ```
4. Open the **Catalog** tab, locate **Enhanced Posters**, and click **Install**.
5. Restart Jellyfin.

---

## Configuration & Setup

1. In Jellyfin, open **Dashboard &rarr; Plugins &rarr; My Plugins &rarr; Enhanced Posters**.
2. Customize badge toggles, anchor placements, translucent pill container styles, and rating tiers. The live interactive preview updates instantly.
3. Click **Save Configuration**.
4. To apply overlays across your library:
   - Go to **Dashboard &rarr; Scheduled Tasks**.
   - Run **Update Enhanced Posters**.

---

## Build from Source

1. Install the .NET 9 SDK.
2. From the repository root, run:

   ```shell
   dotnet publish EnhancedPosters/EnhancedPosters.csproj -c Release
   ```

3. Copy `EnhancedPosters.dll` into your Jellyfin plugin folder and restart Jellyfin.
