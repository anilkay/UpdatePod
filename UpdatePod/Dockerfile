FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["UpdatePod/UpdatePod.csproj", "UpdatePod/"]
COPY ["UpdatePod.Domain/UpdatePod.Domain.csproj", "UpdatePod.Domain/"]
RUN dotnet restore "UpdatePod/UpdatePod.csproj"

# Copy the rest of the source code
COPY . .

WORKDIR "/src/UpdatePod"
RUN dotnet build "UpdatePod.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "UpdatePod.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UpdatePod.dll"]