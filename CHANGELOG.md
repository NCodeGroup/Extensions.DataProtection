# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- `AssemblyVersion` now moves only with the major version (`1.0.0.0` for every 1.x release), so a minor or patch upgrade is a drop-in replacement; `FileVersion` and `InformationalVersion` keep the exact version. Releases before this one used the full version, such as `1.0.1.0`.
- The public API is unchanged and now tracked in `PublicAPI.Shipped.txt`; no library behavior changed.

## [1.0.1] - 2026-01-22

### Fixed

- XML documentation.

## [1.0.0] - 2026-01-22

### Added

- Initial release.

[Unreleased]: https://github.com/NCodeGroup/Extensions.DataProtection/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/NCodeGroup/Extensions.DataProtection/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/NCodeGroup/Extensions.DataProtection/releases/tag/v1.0.0
