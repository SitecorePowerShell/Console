# escape=`

ARG BASE_IMAGE
ARG MANAGEMENT_SERVICES_IMAGE

FROM ${MANAGEMENT_SERVICES_IMAGE} AS management_services
FROM ${BASE_IMAGE}

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# The Sitecore client language packs (Danish, German, Japanese, Chinese) used to be
# installed here from sitecoredev.azureedge.net. That CDN was retired and the media
# URLs now return 404, which fails the build. They were a convenience for exercising
# SPE's own translations (translations/spe-translation-*) in the Sitecore client;
# without them the client is English-only. Re-add a COPY or ADD here if the packs
# are located again.

WORKDIR C:\inetpub\wwwroot

# Copy the Sitecore Management Services Module
COPY --from=management_services C:\module\cm\content .\