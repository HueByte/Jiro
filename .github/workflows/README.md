# Jiro.Kernel CI/CD Workflows

This directory contains GitHub Actions workflows specifically designed for the Jiro.Kernel project. These workflows provide comprehensive testing, building, security scanning, and deployment automation.

## 🚀 Workflows Overview

### 1. `jiro-kernel-ci.yml` - Main CI/CD Pipeline

**Triggers:** Push/PR to main branches with changes in `src/Jiro.Kernel/`

**What it does:**

- ✅ **Build & Test**: Compiles the solution and runs all tests
- 📝 **Format Check**: Ensures code follows formatting standards
- � **Markdown Lint**: Validates markdown file formatting and style
- �🔒 **Security Scan**: Checks for vulnerabilities using multiple tools
- 🐳 **Docker Build**: Builds and tests Docker container
- 📊 **Quality Gate**: Provides overall pipeline status

**Jobs:**

1. **build-and-test**: Builds solution, runs tests, uploads coverage
2. **markdown-lint**: Validates markdown file formatting and style
3. **security-scan**: .NET audit, Snyk scan, CodeQL analysis
4. **docker-build**: Docker build, vulnerability scan with Trivy
5. **quality-gate**: Summarizes all results

### 2. `jiro-kernel-security.yml` - Weekly Security Audit

**Triggers:** Every Monday at 2 AM UTC + manual dispatch

**What it does:**

- 🔍 Comprehensive security scanning
- 📋 Dependency vulnerability review
- 🚨 Creates GitHub issues for found vulnerabilities
- 📈 Generates security reports

### 3. `jiro-kernel-performance.yml` - Performance Testing

**Triggers:** Every Sunday at 3 AM UTC + manual dispatch

**What it does:**

- ⚡ Runs performance tests
- 📊 Executes benchmarks (if BenchmarkDotNet is configured)
- 📁 Uploads performance artifacts

## 🔧 Setup Requirements

### Required Secrets

Add these to your repository secrets (`Settings > Secrets and variables > Actions`):

```
SNYK_TOKEN (optional) - For Snyk security scanning
```

### Required Repository Settings

1. Enable **Actions** in repository settings
2. Allow **GitHub Actions** to create and approve pull requests (for Dependabot)
3. Enable **CodeQL** security analysis
4. Configure **branch protection rules** for main branch

## 📦 Dependabot Configuration

The `dependabot.yml` file automatically:

- Updates NuGet packages weekly
- Updates Docker base images weekly
- Updates GitHub Actions weekly
- Groups related packages together
- Auto-assigns reviewers

## 🐳 Docker Improvements

The updated `Dockerfile`:

- Uses .NET 9 runtime and SDK
- Implements security best practices:
  - Non-root user execution
  - Minimal attack surface
  - Updated base images
  - Health checks
- Optimized build process with multi-stage builds
- Includes `.dockerignore` for faster builds

## 🔒 Security Features

### Automated Scans

- **CodeQL**: Static analysis for code vulnerabilities
- **Dependabot**: Dependency vulnerability scanning
- **Trivy**: Container image vulnerability scanning
- **Snyk**: Third-party security scanning (optional)
- **.NET Security Audit**: Built-in .NET vulnerability checking

### Security Reporting

- SARIF reports uploaded to GitHub Security tab
- Automated issue creation for vulnerabilities
- Regular audit summaries

## 📊 Code Coverage

- Coverage reports uploaded to Codecov
- Results available in PR comments
- Historical tracking of coverage trends

## 🚦 Quality Gates

The pipeline will fail if:

- ❌ Build fails
- ❌ Tests fail
- ❌ Code formatting is incorrect
- ❌ Docker build fails

The pipeline will warn (but not fail) if:

- ⚠️ Security vulnerabilities found (creates issue)
- ⚠️ Performance degradation detected

## 🎯 Usage Tips

### Running Workflows Manually

1. Go to `Actions` tab in GitHub
2. Select the workflow you want to run
3. Click `Run workflow`
4. Provide any required inputs

### Monitoring Performance

- Check the Performance workflow results weekly
- Review benchmark trends over time
- Add `[Category("Performance")]` to your test methods

### Security Best Practices

1. Review security issues created by workflows
2. Keep dependencies updated via Dependabot PRs
3. Monitor the Security tab for vulnerability alerts
4. Configure Snyk token for enhanced scanning

### Customization

- Modify trigger conditions in workflow files
- Add environment-specific configurations
- Extend quality gates as needed
- Add custom steps for your specific requirements

## 🔄 Maintenance

### Weekly Tasks

- [ ] Review Dependabot PRs
- [ ] Check security scan results
- [ ] Monitor performance trends

### Monthly Tasks

- [ ] Update workflow actions to latest versions
- [ ] Review and update security policies
- [ ] Optimize Docker images and build times

### Quarterly Tasks

- [ ] Review and update quality gates
- [ ] Assess new security tools and integrations
- [ ] Performance baseline review

## � Markdown Linting Configuration

The repository includes automated markdown linting to ensure consistent documentation quality:

### Configuration File

The `.markdownlint.json` file contains the linting rules:

- **MD013** (line length): Disabled for flexibility
- **MD033** (inline HTML): Allowed for enhanced formatting
- **MD041** (first line heading): Not enforced
- **MD024** (duplicate headings): Allowed for structured documents
- **MD029** (ordered list prefixes): Flexible numbering allowed

### Excluded Directories

Markdown linting automatically excludes:

- `node_modules/`
- `TestResults/`
- `src/docs/build/`

### Customization

To modify markdown linting rules, edit `.markdownlint.json` in the repository root. The workflow will automatically create default configuration if the file doesn't exist.

## �📞 Support

For issues with workflows:

1. Check the Actions tab for detailed logs
2. Review this documentation
3. Check GitHub's Actions documentation
4. Create an issue in the repository

---

**Last Updated:** $(date)
**Workflow Version:** v1.0
