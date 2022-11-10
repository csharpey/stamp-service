FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /app

COPY . .
RUN dotnet tool restore
RUN dotnet restore

WORKDIR /app/src/Rst.Pdf.Stamp.Web

RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN apt update && apt install $(cat packages.list) -y
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "./Rst.Pdf.Stamp.Web.dll"]
