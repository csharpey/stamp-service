## Development Build

```shell
ln -s /lib/x86_64-linux-gnu/libdl.so.2 /lib/x86_64-linux-gnu/libdl.so
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
