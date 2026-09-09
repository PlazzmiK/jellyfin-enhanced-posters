# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.4.1] - 2026-09-09

### Added
- Native support for combined resolution and HDR badge assets: automatically unifies resolution and dynamic range into authentic pre-combined badges (e.g. 4K DV, 4K HDR, 4K HDR10+, 4K DV HDR, 1080p DV, 1080p HDR, etc.).
- Audio combo badge support: automatically pairs Dolby Atmos and Dolby TrueHD into unified `truehd_atmos` badges.
- Intelligent HDR fallback detection: detects Dolby Vision media with HDR10 or HDR10+ fallback layers (`DOVIWithHDR10`, `DOVIWithHDR10Plus`, tags) and composites exact multi-format badges (`4kdvhdr`, `4kdvhdrplus`).
- Dashboard toggles for combined badges: easily enable or disable resolution/HDR combining and audio combining under Poster Settings, complete with real-time live preview updates.

## [1.4.0] - 2026-09-09

### Added
- Built-in Kometa badge assets embedded natively for resolution (4K, 1080p, 720p, 480p, 576p), video (Dolby Vision, HDR, HDR10+, HLG), audio (Dolby Atmos, DTS:X, TrueHD, DTS-HD MA, FLAC, Opus), edition (IMAX, Extended, Director's Cut, Theatrical, Unrated, Criterion), and combo badges.
- Glassmorphic translucent pill container with proportional badge scaling, generous top/bottom margins, and configurable tag spacing.
- Unified styling across all badge anchors: edition badges (e.g. IMAX) now render in the matching sleek translucent pill container.

### Changed
- Transitioned project branding, assembly names, solution, and namespaces to "Enhanced Posters" (`EnhancedPosters`) with full backward-compatibility migration for existing disk data and scheduled tasks.
- Updated project logo and catalog assets to the new Enhanced Posters cosmic poster badge design.
- Adjusted default pill container styling for enhanced glassmorphism transparency (65% opacity, soft shadow, subtle white border outline).

## [1.3.0] - 2026-09-09

### Added
- Redesigned plugin configuration UI featuring a modern sidebar layout with 5 categorized tabs and registered in Jellyfin's primary navigation drawer under Plug-ins.
- Direct Task Runner in dashboard: trigger and monitor "Process Posters Now" and "Restore Original Posters" with real-time status and live progress tracking.
- Fully dynamic Score Tiers manager: configure arbitrary score ranges, colors, and tier names with one-click presets (5-tier, 10-tier).
- Rating source preferences: select between Community Rating (IMDb/TMDb), Critic Rating (Rotten Tomatoes/Metacritic), or Combined Average with 10-point normalization.
- Score badge corner radius inheritance: choose between using the default badge corner radius from Global Settings or setting a custom corner radius.
- Automatic low-resolution poster upscaling: upscales small posters to standard 1000×1500 resolution using high-quality filtering so badges never appear oversized.

## [1.2.0] - 2026-09-08

### Added
- Auto-crop to standard 2:3 portrait aspect ratio: automatically center-crops base posters to Jellyfin's official 2:3 (1:1.5) card ratio before applying overlays, permanently eliminating the side margin clipping caused by Jellyfin card containers on wider source posters.
- 2:3 Portrait Frame Anchor Widget: redesigned the position anchor picker into an interactive 2:3 portrait poster frame with 9 directional pin buttons, active glow highlights, and synchronized select dropdowns in the dashboard.

## [1.1.3] - 2026-09-08

### Fixed
- Automatic cache invalidation on plugin update: included assembly version in render stamps so upgrading the plugin automatically forces scheduled tasks to re-render posters with latest engine fixes.

## [1.1.2] - 2026-09-08

### Fixed
- Badges sitting flush against side borders: fixed settings bindings for badge horizontal offsets in Jellyfin dashboard, added automatic fallback to vertical offset when side offset is unspecified/zero, and added proportional resolution scaling.
- Combined pill background: removed duplicate dark solid background box behind HDR10, Dolby Vision, and text badges so badges render cleanly and directly inside the unified translucent pill.

## [1.1.1] - 2026-09-08

### Added
- Automatic detection of refreshed/updated artwork: when posters are refreshed via metadata scans or manually updated in Jellyfin, the pristine backup is automatically updated with the new artwork before re-applying overlays.

## [1.1.0] - 2026-09-08

### Added
- Edition badge detection from filename `{edition-...}` tags (IMAX, Extended, Director's Cut, Theatrical, Unrated, Special Edition, Remastered).
- 3D version badge with 3D glasses icon detected from `{edition-3D}`, `[3D]`, and video stream properties.
- Unified translucent pill container to group co-located badges seamlessly with configurable opacity, corner radius, and padding.
- Interactive 3x3 graphical position anchor pickers for all badge types.
- In-dashboard custom badge asset manager to upload custom PNG/WebP/SVG badge files directly.
- Refined score badge background colors and snug fitting to match media badge dimensions.

## [1.0.0.1] - 2026-09-08

### Fixed
- Fixed `System.InvalidOperationException: An invalid request URI was provided` during poster overlay generation by passing composited image stream with MIME type directly to `ProviderManager.SaveImage`.
- Updated release pipeline to support 4-part version tags.

## [1.0.0-RC1] - 2026-09-08

### Added
- Initial release candidate for Posters Enhanced.
- Non-destructive poster overlays preserving untouched original posters beside media files.
- High-performance SkiaSharp graphics compositing engine.
- Media badges for resolution (4K, 1080p, 720p, SD), video range (Dolby Vision, HDR10+, HDR10, HDR, HLG), and audio codecs (Dolby Atmos, DTS:X, TrueHD, DTS-HD MA, FLAC).
- Dynamic rating pill badge with customizable score-based color tiers (<5 Red, 5-6 Orange, 6-7 Yellow, 7-8 Green, 8+ Blue/Diamond).
- 9-point grid anchor positioning with fine-tuning offsets, scaling, and spacing.
- Smart re-render optimization with SHA-256 state tracking.
- Theme pack support for custom PNG/SVG badge assets.
- Interactive configuration dashboard with real-time live preview.
- One-click restore scheduled task to revert all posters to originals.

[Unreleased]: https://github.com/PlazzmiK/jellyfin-enhanced-posters/compare/1.3.0...HEAD
[1.3.0]: https://github.com/PlazzmiK/jellyfin-enhanced-posters/compare/1.2.0...1.3.0
[1.2.0]: https://github.com/PlazzmiK/jellyfin-enhanced-posters/compare/1.1.3...1.2.0
[1.1.3]: https://github.com/PlazzmiK/jellyfin-enhanced-posters/compare/1.1.2...1.1.3
[1.1.2]: https://github.com/PlazzmiK/jellyfin-enhanced-posters/compare/1.1.1...1.1.2
[1.1.1]: https://github.com/PlazzmiK/jellyfin-enhanced-posters/compare/1.1.0...1.1.1
[1.1.0]: https://github.com/PlazzmiK/jellyfin-enhanced-posters/compare/1.0.0.1...1.1.0
[1.0.0.1]: https://github.com/PlazzmiK/jellyfin-enhanced-posters/compare/1.0.0-RC1...1.0.0.1
[1.0.0-RC1]: https://github.com/PlazzmiK/jellyfin-enhanced-posters/tree/1.0.0-RC1
