FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore clients/DottIn.Admin/DottIn.Admin.csproj
RUN dotnet publish clients/DottIn.Admin/DottIn.Admin.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM nginx:1.27-alpine AS final
COPY clients/DottIn.Admin/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 8080
