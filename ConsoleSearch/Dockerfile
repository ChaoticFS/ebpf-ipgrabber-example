FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build
WORKDIR /app

COPY . .
RUN dotnet publish /app/ConsoleSearch/ConsoleSearch.csproj -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "/app/ConsoleSearch.dll"]