# 📚 Jiro Documentation

This directory contains the documentation for the Jiro AI Assistant project, built using [DocFX](https://dotnet.github.io/docfx/).

## 🏗️ Structure

```text
/
├── docs/                    # Documentation content
│   ├── api-index.md        # API documentation index
│   ├── project-description.md  # Project overview
│   ├── user-guide.md       # End-user documentation
│   ├── workflow-pipelines.md  # CI/CD workflows
│   └── README.md           # This file
├── generated/              # Generated documentation (after build)
│   ├── docs/               # Documentation output
│   ├── api/                # API reference output
│   └── assets/             # Static assets (images, CSS, etc.)
├── assets/                 # Source assets (logos, images)
├── docfx.json              # DocFX configuration
├── toc.yml                 # Main navigation structure
└── filterConfig.yml        # API documentation filter
```

## 🚀 Building Documentation Locally

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- DocFX tool

### Setup

1. **Install DocFX:**

   ```bash
   dotnet tool install -g docfx
   ```

2. **Navigate to the repository root:**

   ```bash
   cd /path/to/Jiro
   ```

3. **Build the documentation:**

   ```bash
   docfx docfx.json
   ```

4. **Serve locally (optional):**

   ```bash
   docfx docfx.json --serve
   ```

   The documentation will be available at `http://localhost:8080`

## 🌐 Automatic Deployment

The documentation is automatically deployed to GitHub Pages when changes are pushed to the `main` branch. The deployment is handled by the GitHub Actions workflow at `.github/workflows/deploy-docs.yml`.

### Deployment Triggers

The documentation deployment is triggered by:

- **Push to main branch** with changes to:
  - `docs/**` (any documentation files)
  - `docfx.json` (DocFX configuration)
  - `.github/workflows/deploy-docs.yml` (workflow file)
- **Manual workflow dispatch** (can be triggered manually from GitHub Actions)

### Deployment Process

1. **Checkout** - Retrieves the latest code
2. **Setup .NET** - Installs .NET 9.x SDK
3. **Install DocFX** - Installs the latest DocFX tool
4. **Restore Dependencies** - Restores .NET project dependencies for API docs
5. **Build Documentation** - Generates the static site using DocFX
6. **Deploy to GitHub Pages** - Publishes to GitHub Pages

## 📝 Content Guidelines

### Adding New Documentation

1. **Create your markdown file** in the `docs/` directory
2. **Add entry to toc.yml** to include it in the navigation
3. **Use proper markdown formatting** with headings, lists, and links
4. **Include emojis** for visual appeal (following the existing style)
5. **Test locally** before committing

### Markdown Style Guide

- Use descriptive headings with appropriate levels
- Include blank lines around headings and lists
- Use code blocks for commands and configuration
- Include links to related documentation
- Add emojis to section headers for visual organization

### API Documentation

API documentation is automatically generated from XML comments in the .NET code. To improve API docs:

1. **Add XML documentation** to all public classes, methods, and properties
2. **Use `<summary>`, `<param>`, `<returns>`** tags appropriately
3. **Include `<example>` blocks** for complex methods
4. **Document exceptions** with `<exception>` tags

## 🔧 Configuration

### DocFX Configuration (`docfx.json`)

Key configuration options:

- **metadata.src**: Source code paths for API documentation generation
- **build.content**: Documentation content files to include
- **build.resource**: Static resources (images, assets)
- **build.output**: Output directory for generated site
- **globalMetadata**: Site-wide settings and branding

### Customization

To customize the documentation:

1. **Modify `docfx.json`** for build configuration
2. **Update `globalMetadata`** for site information
3. **Add custom templates** in the `template` directory (if needed)
4. **Include custom CSS/JS** in the `resource` files

## 🎯 Best Practices

### Documentation Writing

- **Clear and Concise** - Write for your audience
- **Up-to-date** - Keep documentation current with code changes
- **Well-structured** - Use consistent formatting and organization
- **Searchable** - Include relevant keywords and cross-references

### Maintenance

- **Regular Updates** - Review and update documentation regularly
- **Link Validation** - Ensure all links work correctly
- **Accessibility** - Use proper heading structure and alt text
- **Performance** - Optimize images and keep pages reasonably sized

## 🐛 Troubleshooting

### Common Issues

1. **DocFX build fails**
   - Check that all referenced files exist
   - Validate JSON syntax in `docfx.json`
   - Ensure .NET projects can be restored

2. **Missing API documentation**
   - Verify XML documentation is enabled in project files
   - Check that projects build successfully
   - Confirm correct paths in `docfx.json`

3. **Deployment fails**
   - Check GitHub Actions logs for specific errors
   - Verify repository settings allow GitHub Pages
   - Ensure workflow has necessary permissions

### Getting Help

- Check the [DocFX documentation](https://dotnet.github.io/docfx/)
- Review GitHub Actions workflow logs
- Examine the generated `_site` directory for build issues

## 📞 Support

For questions about the documentation system or contributing to the docs, please refer to the main project documentation or open an issue in the repository.
