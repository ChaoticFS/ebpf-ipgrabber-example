FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

COPY . .

RUN dotnet clean
RUN dotnet publish /app/SearchAPI/SearchAPI.csproj -o /app/publish -a $TARGETARCH

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
EXPOSE 5262

ENV ASPNETCORE_URLS=http://+:5262

WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "/app/SearchAPI.dll"]