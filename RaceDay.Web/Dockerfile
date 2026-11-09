# ---- build stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# restore first, so Docker can cache the packages until the .csproj changes
COPY RaceDay.Web.csproj ./
RUN dotnet restore

# then copy the rest and publish
COPY . ./
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# run as the non-root user that ships with the .NET 8 images
USER $APP_UID

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

# the address of the Part 2 API is passed in at run time (see the README)
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
