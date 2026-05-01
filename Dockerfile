FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY WebApplication/WebApplication.csproj WebApplication/
RUN dotnet restore WebApplication/WebApplication.csproj

# Copy everything else and build
COPY WebApplication/ WebApplication/
RUN dotnet publish WebApplication/WebApplication.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway sets PORT env variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApplication.dll"]
