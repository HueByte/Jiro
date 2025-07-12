# 🔧 API Documentation

This directory contains the automatically generated API documentation for the Jiro.Kernel project, created from XML documentation comments in the source code.

## 📚 What's Included

The API documentation covers all public classes, interfaces, methods, and properties from:

- **Jiro.Core** - Core business logic, services, and abstractions
- **Jiro.App** - Application entry point and configuration
- **Jiro.Infrastructure** - Data access layer and infrastructure services

## 🏗️ Generation Process

The API documentation is automatically generated using DocFX from XML documentation comments in the C# source code. This ensures the documentation is always up-to-date with the latest code changes.

### Building Locally

To regenerate the API documentation locally:

```bash
# From the src directory
dotnet restore Main.sln
docfx docfx.json --serve
```

This will build the documentation and serve it locally at `http://localhost:8080`.

## 📖 Navigation

- Browse by **Namespace** to see the logical organization of the codebase
- View **Class** details to understand individual components
- Explore **Method** documentation for usage examples and parameter details
- Check **Interface** definitions to understand contracts and abstractions

## 🔗 Related Documentation

- [Main Documentation](index.md) - Project overview and guides
- [User Guide](user-guide.md) - End-user documentation
- [Workflow Pipelines](workflow-pipelines.md) - CI/CD documentation

---

> **Note:** This documentation is automatically generated and deployed with every push to the main branch. Manual edits to these files will be overwritten.
