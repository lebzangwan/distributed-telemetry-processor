FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["DistributedTelemetryProcessor.slnx", "./"]
COPY ["src/Telemetry.Shared/Telemetry.Shared.csproj", "src/Telemetry.Shared/"]
COPY ["src/Telemetry.Publisher/Telemetry.Publisher.csproj", "src/Telemetry.Publisher/"]
COPY ["src/Telemetry.Consumer/Telemetry.Consumer.csproj", "src/Telemetry.Consumer/"]

RUN dotnet restore "src/Telemetry.Publisher/Telemetry.Publisher.csproj"

COPY ["src/Telemetry.Shared/", "src/Telemetry.Shared/"]
COPY ["src/Telemetry.Publisher/", "src/Telemetry.Publisher/"]

WORKDIR "/src/src/Telemetry.Publisher"
RUN dotnet publish "Telemetry.Publisher.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Telemetry.Publisher.dll"]