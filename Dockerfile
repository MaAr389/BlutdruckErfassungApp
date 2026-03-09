FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ./BlutdruckErfassungApp.csproj
RUN dotnet publish ./BlutdruckErfassungApp.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
VOLUME ["/app/data", "/app/keyring"]

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "BlutdruckErfassungApp.dll"]
