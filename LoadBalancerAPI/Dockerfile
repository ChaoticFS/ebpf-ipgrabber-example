﻿FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

COPY . .

RUN dotnet clean
RUN dotnet publish /app/LoadBalancerAPI/LoadBalancerAPI.csproj -o /app/publish

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "/app/LoadBalancerAPI.dll"]