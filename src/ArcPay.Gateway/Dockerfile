FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ArcPay.Shared/ArcPay.Shared.csproj", "src/ArcPay.Shared/"]
COPY ["src/ArcPay.Gateway/ArcPay.Gateway.csproj", "src/ArcPay.Gateway/"]
RUN dotnet restore "src/ArcPay.Gateway/ArcPay.Gateway.csproj"

COPY src/ArcPay.Shared/ src/ArcPay.Shared/
COPY src/ArcPay.Gateway/ src/ArcPay.Gateway/
RUN dotnet publish "src/ArcPay.Gateway/ArcPay.Gateway.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "ArcPay.Gateway.dll"]
