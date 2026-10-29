FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Shell/HelixERP.Shell.Api/HelixERP.Shell.Api.csproj"
RUN dotnet publish "src/Shell/HelixERP.Shell.Api/HelixERP.Shell.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    FileStorage__RootPath=/var/helix/files
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "HelixERP.Shell.Api.dll"]