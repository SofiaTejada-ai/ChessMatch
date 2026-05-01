FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY WebApplication/WebApplication.csproj WebApplication/
RUN dotnet restore WebApplication/WebApplication.csproj

# Copy everything and build
COPY . .
WORKDIR /src/WebApplication
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway sets PORT env var at runtime
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Use a shell entrypoint so Railway's PORT env var is expanded at runtime
CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet WebApplication.dll
