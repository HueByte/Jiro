# Migration Guide: From v0.1.0-alpha

## Overview

This guide helps you migrate from v0.1.0-alpha to newer versions of Jiro AI Assistant.

## Breaking Changes

### Configuration Changes

- Environment configuration has been updated
- Docker configuration structure modified
- Database schema updates may require migration

### File Structure Changes

- Documentation moved to `dev/docs/` directory
- Configuration files reorganized
- Build scripts updated

## Migration Steps

### 1. Backup Your Data

```bash
# Backup your database
# Backup your configuration files
# Backup any custom modifications
```

### 2. Update Configuration

```bash
# Update environment variables
# Review Docker compose configuration
# Update any custom build scripts
```

### 3. Database Migration

```bash
# Run Entity Framework migrations
dotnet ef database update
```

### 4. Verify Installation

```bash
# Test the application
# Verify all features work as expected
# Check logs for any errors
```

## Support

If you encounter issues during migration:

1. Check the [Changelog](index.md) for detailed changes
2. Review the [User Guide](../user-guide.md) for updated procedures
3. Create an issue in the GitHub repository

## Post-Migration

After successful migration:

- Update your documentation
- Test all integrations
- Update any automation scripts
- Train team members on any changes
