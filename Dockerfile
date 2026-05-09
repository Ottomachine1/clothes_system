FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "./src/ClothesSystem.Web/ClothesSystem.Web.csproj"
RUN dotnet publish "./src/ClothesSystem.Web/ClothesSystem.Web.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    OpenBrowserOnStart=false \
    DataRoot=/app/data \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/clothes-system.db" \
    SeedDefaultAdmin=true \
    ResetDefaultAdminPassword=false \
    SeedDemoClothing=true \
    SeedDemoUsers=false \
    ResetDemoPasswords=false

RUN mkdir -p /app/data
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ClothesSystem.Web.dll"]
