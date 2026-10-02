# ============================================================
# Stage 1: Build – .NET SDK 10
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG RUNTIME=linux-x64

WORKDIR /src

COPY ["src/TITKUL.PMTTCU.Web/TITKUL.PMTTCU.Web.csproj", "src/TITKUL.PMTTCU.Web/"]
COPY ["nuget.config", "."]
COPY ["global.json", "."]
COPY ["Directory.Build.props", "."]

RUN dotnet restore src/TITKUL.PMTTCU.Web/TITKUL.PMTTCU.Web.csproj \
    -r $RUNTIME \
    /p:PublishReadyToRun=true

COPY ["src/", "src/"]

RUN dotnet publish src/TITKUL.PMTTCU.Web/TITKUL.PMTTCU.Web.csproj \
    -c $BUILD_CONFIGURATION \
    -r $RUNTIME \
    --self-contained false \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishReadyToRun=true \
    --no-restore

# ============================================================
# Stage 2: Runtime
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Npgsql can libgssapi-krb5-2 cho Kerberos/GSSAPI
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV TZ=Asia/Ho_Chi_Minh

WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TITKUL.PMTTCU.Web.dll"]

