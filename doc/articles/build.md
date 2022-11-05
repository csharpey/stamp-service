## Development Build

```shell
dotnet dev-certs https
dotnet tool restore
dotnet restore
dotnet build
```

## Build with Docker

- Build backend image

```shell
docker-compose -f docker-compose.yaml build backend
```
