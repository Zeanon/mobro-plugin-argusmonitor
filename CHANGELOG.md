# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 0.1.5 - Unreleased

### Added

- Optional configurable delay before initializing the sensors

### Fixed

- Fixed order of item registration

## 0.1.4 - 2024-01-03

### Changed

- Updated to LibreHardwareMonitor 0.9.3

## 0.1.3 - 2023-07-27

### Changed

- Updated SDK

## 0.1.2 - 2023-07-03

### Added

- Added Program.cs to run and test the plugin locally
- Expose update frequency as settings

### Changed

- Updated SDK
- Do not publish .dll of SDK

## 0.1.1 - 2023-04-03

### Changed

- Updated to LibreHardwareMonitor 0.9.2

### Fixed

- Sensors of sub hardware were not correctly registered as metrics and groups

## 0.1.0 - 2023-01-23

### Added

- Support for metrics in battery category
- Support for electric current based metrics
- Support for duration based metric values

### Changed

- Parse metrics of no specific type as numeric instead of text where applicable
- Sensor as struct to reduce memory allocations
- Updated to new SDK
- Updated to .NET 7

## 0.0.2 - 2022-11-06

### Fixed

- Throughput being returned in bytes instead of bits

## 0.0.1 - 2022-10-17

### Added

- Initial release
