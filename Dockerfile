FROM dotnet/sdk:6.0 AS build

WORKDIR /app

COPY . .

RUN dotnet restore

WORKDIR /app/src/WebApplication

RUN dotnet publish -c Release -o /app/publish

FROM dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "./Rst.Pdf.Stamp.dll"]
