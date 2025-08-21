FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore product-service.csproj
RUN dotnet publish product-service.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5279
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 5279
ENTRYPOINT ["dotnet", "product-service.dll"]