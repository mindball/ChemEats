# ChemEats — Linux deployment (systemd)

## 1. Publish (on the Windows dev machine, from the repo root)

```powershell
dotnet publish WebApi/WebApi.csproj -c Release -r linux-x64 --self-contained true -o ./publish-linux
```

## 2. Install on the Linux host (first time)

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin chemeats
sudo mkdir -p /opt/chemeats /etc/chemeats /var/log/chemeats
# copy ./publish-linux/* to /opt/chemeats (scp / rsync)
sudo chmod +x /opt/chemeats/WebApi
sudo chown -R chemeats:chemeats /opt/chemeats /var/log/chemeats   # App_Data/company-settings.json is written at runtime

sudo cp chemeats.env.example /etc/chemeats/chemeats.env   # then fill it in
sudo chmod 600 /etc/chemeats/chemeats.env

sudo cp chemeats.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now chemeats
journalctl -u chemeats -f
```

The app listens on `http://0.0.0.0:12345` (Kestrel section in `appsettings.Production.json`); open that port in the firewall (`sudo ufw allow 12345/tcp`).

## 3. Update

```bash
sudo systemctl stop chemeats
# copy new files over /opt/chemeats, keep App_Data/company-settings.json
sudo chmod +x /opt/chemeats/WebApi && sudo chown -R chemeats:chemeats /opt/chemeats
sudo systemctl start chemeats
```

## Notes

- Users are created by an admin (Admin panel → User Management). On an empty DB set `BootstrapAdmin__Abbreviation` once to get the first admin.
- JWT is issued by the app itself (`Jwt__Issuer` / `Jwt__Secret`); the external Employee service is no longer used.
- Firebird: the .NET provider is fully managed, no `fbclient` needed. Database path in the connection string must be the Linux path on the Firebird server.
