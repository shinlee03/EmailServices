ARG DOTNET_RUNTIME=mcr.microsoft.com/dotnet/aspnet:8.0
ARG DOTNET_SDK=mcr.microsoft.com/dotnet/sdk:8.0

FROM ${DOTNET_SDK} AS build
WORKDIR /source

COPY *.sln .
COPY EmailService/*.csproj ./EmailService/
COPY EmailService.Tests/*.csproj ./EmailService.Tests/
RUN dotnet restore

COPY EmailService/. ./EmailService/
WORKDIR /source/EmailService
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT dotnet-ef database update