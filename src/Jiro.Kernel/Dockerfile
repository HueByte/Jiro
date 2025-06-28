FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG MAIN_WORK_DIR=/home/app/jiro
WORKDIR ${MAIN_WORK_DIR}

# Update and install required dependencies
RUN apt-get update && apt-get -y upgrade && apt-get install -y \
	build-essential gcc g++ curl tar pkg-config libtool m4 \
	libpng-dev libjpeg-dev libxi6 libgl1-mesa-glx

# Install Node.js for any frontend dependencies
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - && \
	apt-get install -y nodejs

WORKDIR ${MAIN_WORK_DIR}

# Copy solution and project files for dependency caching
COPY Jiro.Kernel.sln ./
COPY Jiro.App/Jiro.App.csproj ./Jiro.App/
COPY Jiro.Core/Jiro.Core.csproj ./Jiro.Core/
COPY Jiro.Infrastructure/Jiro.Infrastructure.csproj ./Jiro.Infrastructure/

# Restore dependencies
RUN dotnet restore Jiro.Kernel.sln

# Copy the rest of the source code and build the application
COPY . ./
RUN dotnet publish Jiro.App/Jiro.App.csproj -c Release -o build \
	--runtime linux-x64 \
	--self-contained true \
	/p:PublishSingleFile=false \
	/p:PublishTrimmed=true \
	/p:UseAppHost=true

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
ARG MAIN_WORK_DIR=/home/app/jiro
WORKDIR ${MAIN_WORK_DIR}

# Create a non-root user
RUN useradd -ms /bin/bash jiro

# For self-contained apps, minimal dependencies are needed
RUN apt-get update && apt-get install -y \
	ca-certificates \
	&& apt-get clean \
	&& rm -rf /var/lib/apt/lists/*

# Copy build output
COPY --from=build ${MAIN_WORK_DIR}/build ${MAIN_WORK_DIR}/build

# Set up directories
ARG LOGS_PATH=${MAIN_WORK_DIR}/Data/Logs
ARG SHARED_PATH=${MAIN_WORK_DIR}/Data/Shared
ENV LOGS_PATH=${LOGS_PATH}
ENV SHARED_PATH=${SHARED_PATH}
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080;https://+:8443

RUN mkdir -p ${LOGS_PATH} ${SHARED_PATH} && \
	chown -R jiro:jiro ${MAIN_WORK_DIR}

WORKDIR ${MAIN_WORK_DIR}/build

# Expose ports
EXPOSE 8080
EXPOSE 8443
EXPOSE 18090

# Update dynamic linker bindings
RUN ldconfig

# Switch to non-root user
USER jiro

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
	CMD pgrep -f "dotnet.*Jiro.App.dll" > /dev/null || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Jiro.App.dll"]
