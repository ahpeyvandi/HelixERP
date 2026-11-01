FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Ids/HelixERP.IdentityServer.Api/HelixERP.IdentityServer.Api.csproj"
RUN dotnet publish "src/Ids/HelixERP.IdentityServer.Api/HelixERP.IdentityServer.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    IdentityServer__SigningKeyFile=/var/helix/ids/ids-signing-key.rsa
EXPOSE 8080
COPY --from=build /app/publish .
USER 1654:1654
ENTRYPOINT ["dotnet", "HelixERP.IdentityServer.Api.dll"]
