# syntax=docker/dockerfile:1.4
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG CHUMMER_BUILD_CONCURRENCY=1
WORKDIR /src

COPY --from=play-source . chummer-play/
COPY --from=core-engine-contracts . package-plane/engine-contracts/
COPY --from=campaign-contracts . package-plane/campaign-contracts/
COPY --from=play-contracts . package-plane/play-contracts/
COPY --from=ui-kit-source src/Chummer.Ui.Kit/ package-plane/ui-kit/

ENV CHUMMER_PACKAGE_PLANE_ENGINE_CONTRACTS_PROJECT=/src/package-plane/engine-contracts/Chummer.Contracts.csproj
ENV CHUMMER_PACKAGE_PLANE_CAMPAIGN_CONTRACTS_PROJECT=/src/package-plane/campaign-contracts/Chummer.Campaign.Contracts.csproj
ENV CHUMMER_PACKAGE_PLANE_PLAY_CONTRACTS_PROJECT=/src/package-plane/play-contracts/Chummer.Play.Contracts.csproj
ENV CHUMMER_PACKAGE_PLANE_UI_KIT_PROJECT=/src/package-plane/ui-kit/Chummer.Ui.Kit.csproj

WORKDIR /src/chummer-play
RUN scripts/ai/with-package-plane.sh restore src/Chummer.Play.Web/Chummer.Play.Web.csproj \
    -p:ChummerDesktopRuntimeIdentifiers=linux-x64
RUN scripts/ai/with-package-plane.sh publish src/Chummer.Play.Web/Chummer.Play.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    --nologo \
    -m:${CHUMMER_BUILD_CONCURRENCY} \
    -p:BuildInParallel=false \
    -p:UseSharedCompilation=false \
    -p:ChummerDesktopRuntimeIdentifiers=linux-x64

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Chummer.Play.Web.dll"]
