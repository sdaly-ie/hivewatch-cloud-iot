# HiveWatch Dashboard container image
# Build context expected: repository root
# Build command:
# docker build -f dashboard/HiveWatch.Dashboard/Dockerfile -t hivewatch-dashboard:local .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY dashboard/HiveWatch.Dashboard/HiveWatch.Dashboard.csproj dashboard/HiveWatch.Dashboard/
RUN dotnet restore dashboard/HiveWatch.Dashboard/HiveWatch.Dashboard.csproj

COPY dashboard/HiveWatch.Dashboard/ dashboard/HiveWatch.Dashboard/

WORKDIR /src/dashboard/HiveWatch.Dashboard
RUN dotnet publish HiveWatch.Dashboard.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HiveWatch.Dashboard.dll"]
