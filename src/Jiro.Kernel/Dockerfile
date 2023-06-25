FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 18090
ENV PASSWORD_SEED_PROMPT=false

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Prepare Node
RUN apt-get update
RUN apt-get install -y curl
RUN apt-get install -y libpng-dev libjpeg-dev curl libxi6 build-essential libgl1-mesa-glx
RUN curl -sL https://deb.nodesource.com/setup_lts.x | bash -
RUN apt-get install -y nodejs

COPY ["Jiro.Api/Jiro.Api.csproj", "Jiro.Api/"]
COPY ["Jiro.Core/Jiro.Core.csproj", "Jiro.Core/"]
COPY ["Jiro.Infrastructure/Jiro.Infrastructure.csproj", "Jiro.Infrastructure/"]
RUN dotnet restore "Jiro.Api/Jiro.Api.csproj"
COPY . .
WORKDIR "/Jiro.Api"
RUN dotnet tool restore
RUN dotnet build "Jiro.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Jiro.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Jiro.Api.dll"]