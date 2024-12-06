FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

COPY . .

RUN dotnet clean
RUN dotnet publish /app/WebSearch/WebSearch.csproj -c Release -o /app/publish

FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html
EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

COPY WebSearch/nginx.conf /etc/nginx/nginx.conf

RUN rm -rf /etc/nginx/conf.d/*

ENTRYPOINT ["nginx", "-g", "daemon off;"]