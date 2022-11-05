## Host config

`/etc/docker/daemon.json`

```json
{
    "hosts": [
        "tcp://0.0.0.0:2375",
        "unix:///var/run/docker.sock"
    ],
    "insecure-registries": [
    ],
    "bip": "172.100.0.1/16"
}
```
