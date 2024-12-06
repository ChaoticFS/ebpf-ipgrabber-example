FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /app

COPY . .

RUN dotnet clean
RUN dotnet publish /app/indexer/indexer.csproj -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "/app/indexer.dll"]