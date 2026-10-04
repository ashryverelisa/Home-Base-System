FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props HomeBase.slnx ./
COPY HomeBase/HomeBase.csproj HomeBase/
COPY HomeBase.Database/HomeBase.Database.csproj HomeBase.Database/
RUN dotnet restore HomeBase/HomeBase.csproj -a $TARGETARCH

COPY . .
RUN dotnet publish HomeBase/HomeBase.csproj \
    -c Release \
    -a $TARGETARCH \
    --no-restore \
    -o /app/publish \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    Assets__DocumentPath=/data/assets

RUN mkdir -p /data/assets && chown -R $APP_UID:$APP_UID /data
VOLUME /data

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "HomeBase.dll"]
