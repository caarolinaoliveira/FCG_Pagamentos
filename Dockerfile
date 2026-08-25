FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore FCG.Pagamentos.Worker/FCG.Pagamentos.Worker.csproj
RUN dotnet publish FCG.Pagamentos.Worker/FCG.Pagamentos.Worker.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FCG.Pagamentos.Worker.dll"]
