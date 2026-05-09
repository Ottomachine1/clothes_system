# Docker Deployment

This project can be deployed as the ASP.NET Core Web app in Docker.

## Build and run

```powershell
docker compose up -d --build
```

Open:

```text
http://SERVER_IP:8080
```

Default admin account for a new database:

```text
admin@clothes.local
Admin123!
```

The first container start applies EF Core migrations and seeds two demo clothing items:

- `DEMO-2026-001` - Demo Commuter Light Jacket
- `DEMO-2026-002` - Demo Printed Summer Dress

## Persistent data

`docker-compose.yml` mounts local `./docker-data` to `/app/data` in the container.
SQLite database, logs, exports, and uploaded image storage live under that folder.

Back up this directory before moving servers:

```powershell
Compress-Archive -Path .\docker-data -DestinationPath .\docker-data-backup.zip
```

## Useful commands

```powershell
docker compose logs -f
docker compose restart
docker compose down
```

To run on another host port, change the left side of the port mapping:

```yaml
ports:
  - "80:8080"
```

## Production notes

- Put Nginx, Caddy, or another reverse proxy in front if you need HTTPS.
- Change the default admin password after first login.
- Keep `ResetDefaultAdminPassword=false` after initial deployment so container restarts do not overwrite your password.
