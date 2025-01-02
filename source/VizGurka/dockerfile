FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the main project file and the referenced project files
COPY ["VizGurka/VizGurka.csproj", "VizGurka/"]
COPY ["GurkaSpec/GurkaSpec.csproj", "GurkaSpec/"]

# Restore dependencies
RUN dotnet restore "VizGurka/VizGurka.csproj"

# Copy the rest of the application files
COPY . .

# Build the application
WORKDIR "/src/VizGurka"
RUN dotnet build "./VizGurka.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./VizGurka.csproj" -c Release -o /app/publish

# Use the runtime image to run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configure the app to listen on port 8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VizGurka.dll"]