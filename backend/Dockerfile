FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["backend.csproj", "./"]
RUN dotnet restore "backend.csproj"

# Copy everything explicitly to make sure no files are missed
COPY ["Controllers/", "./Controllers/"]
COPY ["Models/", "./Models/"]
COPY ["Services/", "./Services/"]
COPY ["Data/", "./Data/"]
COPY ["Migrations/", "./Migrations/"]
COPY ["Program.cs", "./"]
COPY ["appsettings.json", "./"]
COPY ["appsettings.Development.json", "./"]
COPY ["data.csv", "./"]

RUN dotnet build "backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["data.csv", "./data.csv"]
ENTRYPOINT ["dotnet", "backend.dll"]
