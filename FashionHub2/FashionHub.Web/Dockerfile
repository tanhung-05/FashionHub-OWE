# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["FashionHub.Web/FashionHub.Web.csproj", "FashionHub.Web/"]
RUN dotnet restore "FashionHub.Web/FashionHub.Web.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/FashionHub.Web"
RUN dotnet build "FashionHub.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "FashionHub.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FashionHub.Web.dll"]