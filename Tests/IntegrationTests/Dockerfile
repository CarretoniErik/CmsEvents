FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY . .
RUN dotnet restore "Tests/UnitTests/CmsEvents.UnitTests.csproj" \
 && dotnet restore "Tests/IntegrationTests/CmsEvents.IntegrationTests.csproj"

ENTRYPOINT ["/bin/sh", "-c", "dotnet test Tests/UnitTests/CmsEvents.UnitTests.csproj --no-restore && dotnet test Tests/IntegrationTests/CmsEvents.IntegrationTests.csproj --no-restore"]