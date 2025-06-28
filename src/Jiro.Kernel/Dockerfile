# Use .NET 9 runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443
EXPOSE 18090

# Security: Create non-root user
RUN addgroup --system --gid 1001 jiro \
	&& adduser --system --uid 1001 --ingroup jiro jiro

# Security: Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PASSWORD_SEED_PROMPT=false
ENV ASPNETCORE_URLS=http://+:8080;https://+:8443

# Use .NET 9 SDK for build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Security: Update packages and clean up
RUN apt-get update && apt-get upgrade -y \
	&& apt-get install -y --no-install-recommends \
	curl \
	libpng-dev \
	libjpeg-dev \
	libxi6 \
	build-essential \
	libgl1-mesa-glx \
	&& curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - \
	&& apt-get install -y nodejs \
	&& apt-get clean \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Copy project files and restore dependencies
COPY ["Jiro.App/Jiro.App.csproj", "Jiro.App/"]
COPY ["Jiro.Core/Jiro.Core.csproj", "Jiro.Core/"]
COPY ["Jiro.Infrastructure/Jiro.Infrastructure.csproj", "Jiro.Infrastructure/"]
RUN dotnet restore "Jiro.App/Jiro.App.csproj"
COPY . .
WORKDIR "/src/Jiro.App"

# Build the application
RUN dotnet build "Jiro.App.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "Jiro.App.csproj" -c Release -o /app/publish \
	--no-restore \
	/p:UseAppHost=false \
	/p:PublishTrimmed=true \
	/p:PublishSingleFile=false

FROM base AS final
WORKDIR /app

# Security: Copy files and set proper ownership
COPY --from=publish --chown=jiro:jiro /app/publish .

# Security: Switch to non-root user
USER jiro

# Health check for console application
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
	CMD pgrep -f "dotnet.*Jiro.App.dll" > /dev/null || exit 1

ENTRYPOINT ["dotnet", "Jiro.App.dll"]
