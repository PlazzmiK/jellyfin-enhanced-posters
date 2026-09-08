# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/PlazzmiK/jellyfin-posters-enchanced/compare/1.0.0.1...HEAD
[1.0.0.1]: https://github.com/PlazzmiK/jellyfin-posters-enchanced/compare/1.0.0-RC1...1.0.0.1
[1.0.0-RC1]: https://github.com/PlazzmiK/jellyfin-posters-enchanced/tree/1.0.0-RC1
