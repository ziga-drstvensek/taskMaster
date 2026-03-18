# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["BacklogApi.csproj", "./"]
RUN dotnet restore "BacklogApi.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "BacklogApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install mariadb-client for mysqldump (used by backup service)
RUN apt-get update && apt-get install -y mariadb-client && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BacklogApi.dll"]
