FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

COPY src/CSharpApp.sln src/
COPY src/CSharpApp.Core/CSharpApp.Core.csproj src/CSharpApp.Core/
COPY src/CSharpApp.Application/CSharpApp.Application.csproj src/CSharpApp.Application/
COPY src/CSharpApp.Infrastructure/CSharpApp.Infrastructure.csproj src/CSharpApp.Infrastructure/
COPY src/CSharpApp.Api/CSharpApp.Api.csproj src/CSharpApp.Api/
COPY tests/CSharpApp.Application.Tests/CSharpApp.Application.Tests.csproj tests/CSharpApp.Application.Tests/
COPY tests/CSharpApp.Api.Tests/CSharpApp.Api.Tests.csproj tests/CSharpApp.Api.Tests/

RUN dotnet restore src/CSharpApp.sln

COPY src/ src/
COPY tests/ tests/

FROM build AS test
RUN dotnet test src/CSharpApp.sln \
    --configuration Release \
    --no-restore

FROM test AS publish
RUN dotnet publish src/CSharpApp.Api/CSharpApp.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "CSharpApp.Api.dll"]
