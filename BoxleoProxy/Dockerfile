# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["BoxleoProxy.csproj", "./"]
RUN dotnet restore "BoxleoProxy.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "BoxleoProxy.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "BoxleoProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BoxleoProxy.dll"]
