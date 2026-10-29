FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Modules/Sales/HelixERP.Modules.Sales.Api/HelixERP.Modules.Sales.Api.csproj"
RUN dotnet publish "src/Modules/Sales/HelixERP.Modules.Sales.Api/HelixERP.Modules.Sales.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "HelixERP.Modules.Sales.Api.dll"]