FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY CoreBackend/CoreBackend.csproj CoreBackend/

WORKDIR /app/CoreBackend

RUN dotnet restore

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]

