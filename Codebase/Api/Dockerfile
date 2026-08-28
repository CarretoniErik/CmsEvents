FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Codebase/Api/CmsEvents.Api.csproj", "Codebase/Api/"]
COPY ["Codebase/Application/CmsEvents.Application.csproj", "Codebase/Application/"]
COPY ["Codebase/Domain/CmsEvents.Domain.csproj", "Codebase/Domain/"]
COPY ["Codebase/Infrastructure/CmsEvents.Infrastructure.csproj", "Codebase/Infrastructure/"]

RUN dotnet restore "Codebase/Api/CmsEvents.Api.csproj"

COPY . .

WORKDIR /src/Codebase/Api
RUN dotnet build "CmsEvents.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CmsEvents.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CmsEvents.Api.dll"]