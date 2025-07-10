# Changelog

All notable changes to the Jiro project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial changelog documentation
- 🎨 **Jiro Banner** - Added the official Jiro banner to the documentation homepage for better branding

### Changed

- 📁 **Project Structure Reorganization** - Moved all code-related files to `src/` directory:
  - Moved `api/` and `docs/` folders to `src/`
  - Moved configuration files (`.env`, `.env.example`, `filterConfig.yml`) to `src/`
  - Moved `docker-compose.yml`, `.markdownlint.json`, `.dockerignore`, and `index.md` to `src/`
  - Updated all file references and build configurations
  - Updated VS Code tasks and GitHub workflows
  - Updated `.gitignore` file to reflect new paths and structure
  - Consolidated DocFX configuration to work from `src/` directory

### Deprecated

### Removed

### Fixed

### Security

## [1.0.0] - 2025-07-10

### Added

- 🎯 Command System - Extensible command handling framework
- 💬 Conversation Management - Advanced chat session and message handling
- 🌤️ Weather Integration - Built-in weather services and data
- 👤 User Management - Complete authentication and authorization
- 🗄️ Database Integration - Entity Framework Core with repository pattern
- 🔌 Extensible Architecture - Plugin-based system for easy extension
- Core business logic and domain models (Jiro.Core)
- Data access and external services (Jiro.Infrastructure)
- Application configuration and startup (Jiro.App)
- Comprehensive API documentation
- User guide and documentation
- Database schema and ERD documentation
- Workflow and pipeline documentation

### Changed

### Fixed

---

## How to Use This Changelog

- **Added** for new features
- **Changed** for changes in existing functionality
- **Deprecated** for soon-to-be removed features
- **Removed** for now removed features
- **Fixed** for any bug fixes
- **Security** in case of vulnerabilities

## Version Format

This project uses [Semantic Versioning](https://semver.org/):

- **MAJOR** version when you make incompatible API changes
- **MINOR** version when you add functionality in a backwards compatible manner
- **PATCH** version when you make backwards compatible bug fixes
