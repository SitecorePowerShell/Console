# escape=`

ARG BASE_IMAGE
ARG MANAGEMENT_SERVICES_IMAGE

FROM ${MANAGEMENT_SERVICES_IMAGE} AS management_services
FROM ${BASE_IMAGE}

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

WORKDIR /translations
RUN New-Item -Path packages -ItemType Directory; New-Item -Path extract -ItemType Directory;

# Danish
ADD https://sitecoredev.azureedge.net/~/media/CE62A8BDCB464E039D4EC36970AD5DE3.ashx?date=20221124T073715 '.\packages\'
# German
ADD https://sitecoredev.azureedge.net/~/media/561F69767C104E62AF6BA47728833652.ashx?date=20221124T073716 '.\packages\'
# Japanese
ADD https://sitecoredev.azureedge.net/~/media/20A814F6985E4E61AFBE241E4628EB56.ashx?date=20221124T073716 '.\packages\'
# Chinese
ADD https://sitecoredev.azureedge.net/~/media/5E6BEFCF4BE8434DBAABEFF8CB7E9341.ashx?date=20221124T073716 '.\packages\'
RUN Get-ChildItem -Path '.\packages\*.zip' | ForEach-Object { $_ | Expand-Archive -DestinationPath '.\extract' }; Copy-Item -Path '.\extract\*' -Destination 'C:\inetpub\wwwroot\App_Data' -Recurse -Force; Get-Item -Path ".\packages" | Remove-Item -Recurse; Get-Item -Path ".\extract" | Remove-Item -Recurse;

WORKDIR C:\inetpub\wwwroot

# Copy the Sitecore Management Services Module
COPY --from=management_services C:\module\cm\content .\