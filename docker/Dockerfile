# Build stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Beepul.Afs.FraudDetection.ML.Host.csproj", "./"]
RUN dotnet restore "Beepul.Afs.FraudDetection.ML.Host.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "Beepul.Afs.FraudDetection.ML.Host.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Beepul.Afs.FraudDetection.ML.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install necessary system dependencies for ML.NET
RUN apt-get update && apt-get install -y \
    libgomp1 \
    libomp-dev \
    && rm -rf /var/lib/apt/lists/*

# Create directories for logs and models
RUN mkdir -p /var/log/frauddetection /app/Models

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_ENVIRONMENT=Production

# Run as non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app /var/log/frauddetection
USER appuser

ENTRYPOINT ["dotnet", "Beepul.Afs.FraudDetection.ML.Host.dll"]
