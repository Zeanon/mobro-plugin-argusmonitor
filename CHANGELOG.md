# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## 0.1.9 - 2025-05-21

### Changed

- Fixed value conversion

## 0.1.8 - 2025-05-20

### Changed

- Further optimized ArgusMonitorLink
- Got rid of basically all intermittend data structures

## 0.1.7 - 2025-05-19

### Changed

- Optimized communication between Plugin and ArgusMonitorLink
- Reduced Ram usage
- Optimized ArgusMonitorLink
- Optimized the compiler
- Adjusted Argus Data API poll rate to be in sync with the plugin to reduce cpu load
- Added poll delay setting

## 0.1.6 - 2025-05-16

### Changed

- Added CPU min and average artificial metrics
- If you disable CPU tracking for the plugin, the artificial CPU sensors are disabled as well now

## 0.1.5 - 2025-05-15

### Changed

- Huge Code Cleanup
- Fixed non-unique sensor IDs
- Added artificial metrics
- Improved error handling

## 0.1.4 - 2025-05-14

### Changed

- Improved data transfer from ArgusMonitorLink to the plugin
- Fixed network transfer rate readout

## 0.1.3 - 2025-05-14

### Changed

- Synthetic Temperatures now have their own hardware type
- Modified Group and Sensor IDs to ensure they are truly unique
- Optimized internal parsing and handling to reduce RAM and CPU usage

## 0.1.2 - 2025-05-14

### Changed

- Updated DisplayName

## 0.1.1 - 2025-05-13

### Changed

- Changed SDK Version from 0.3.0 to 1.0.1

## 0.1.0 - 2025-05-13

### Changed

- Code Cleanup and initial public alpha release

## 0.0.1 - 2025-05-13

### Added

- Initial release
