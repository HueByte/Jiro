# Multi-stage build for DocFX documentation
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install DocFX
RUN dotnet tool install -g docfx --version 2.77.0
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy project files needed for API documentation generation
WORKDIR /src
COPY ../src/Jiro.Kernel ./Jiro.Kernel

# Copy DocFX configuration and documentation source
WORKDIR /docs
COPY . .

# Build the documentation
RUN docfx docfx.json

# Final stage - serve with nginx
FROM nginx:alpine

# Copy generated documentation from build stage
COPY --from=build /docs/_site /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

# Use default nginx command
CMD ["nginx", "-g", "daemon off;"]