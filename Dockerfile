FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CleanArch.sln .
COPY src/CleanArch.Domain/CleanArch.Domain.csproj src/CleanArch.Domain/
COPY src/CleanArch.Application/CleanArch.Application.csproj src/CleanArch.Application/
COPY src/CleanArch.Infrastructure/CleanArch.Infrastructure.csproj src/CleanArch.Infrastructure/
COPY src/CleanArch.Api/CleanArch.Api.csproj src/CleanArch.Api/

RUN dotnet restore CleanArch.sln

COPY . .
RUN dotnet publish src/CleanArch.Api/CleanArch.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CleanArch.Api.dll"]
