# ChemEats — Linux deployment (systemd)

## 1. Publish (on the Windows dev machine, from the repo root)

```powershell
dotnet publish WebApi/WebApi.csproj -c Release -r linux-x64 --self-contained true -o ./publish-linux
```

## 2. Install on the Linux host (first time)

```bash
sudo mkdir -p /var/www/chemeats /etc/chemeats /var/log/chemeats
# copy ./publish-linux/* to /var/www/chemeats (scp / rsync)
sudo chmod +x /var/www/chemeats/WebApi
sudo chown -R www-data:www-data /var/www/chemeats /var/log/chemeats   # App_Data/company-settings.json is written at runtime

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
# copy new files over /var/www/chemeats, keep App_Data/company-settings.json
sudo chmod +x /var/www/chemeats/WebApi && sudo chown -R www-data:www-data /var/www/chemeats
sudo systemctl start chemeats
```

## Notes

- Users are created by an admin (Admin panel → User Management). On an empty DB set `BootstrapAdmin__Abbreviation` once to get the first admin.
- JWT is issued by the app itself (`Jwt__Issuer` / `Jwt__Secret`); the external Employee service is no longer used.
- Firebird: the .NET provider is fully managed, no `fbclient` needed. Database path in the connection string must be the Linux path on the Firebird server.

## 4. Deploy script (one-time install, interactive — run by the admin)

Routine deploys use `/usr/local/sbin/chemeats-deploy` (from `chemeats-deploy.sh`), which the `chemist` user may run via sudo **without a password** (`chemeats-deploy.sudoers`); no other sudo command is password-free.

```powershell
scp deploy/linux/chemeats-deploy.sh deploy/linux/chemeats-deploy.sudoers chemist@10.58.168.1:/tmp/
ssh -t chemist@10.58.168.1
```
```bash
sudo install -o root -g root -m 0755 /tmp/chemeats-deploy.sh /usr/local/sbin/chemeats-deploy
sudo install -o root -g root -m 0440 /tmp/chemeats-deploy.sudoers /etc/sudoers.d/chemeats-deploy
sudo visudo -cf /etc/sudoers.d/chemeats-deploy      # must print "parsed OK"
rm /tmp/chemeats-deploy.sh /tmp/chemeats-deploy.sudoers
sudo -k; sudo -n /usr/local/sbin/chemeats-deploy --check   # no password prompt; "/tmp/chemeats.tgz not found" is fine here
```

Repeat the first `install` line whenever `chemeats-deploy.sh` changes.

Usage: `sudo chemeats-deploy` (deploys `/tmp/chemeats.tgz`), `--check`, `--rollback`. Never overwrites `App_Data/` or `appsettings.Production.json`; keeps the previous version in `/var/www/chemeats.old`.

Behavior:
- The deploy/rollback runs as a **detached worker**: a dropped ssh connection or Ctrl+C does not interrupt it. Its output is streamed to the caller and written to `/var/log/chemeats/deploy.log`. Do not start a second deploy while one runs (a lock rejects it); check the log or `systemctl is-active chemeats` instead.
- On failure it recovers by itself: before files changed → starts the current version again; while replacing files → restores `/var/www/chemeats.old`. The backup is built aside and swapped in, so it is never half-written.
- Files are compared by content (`rsync --checksum`).

## 5. Packing the release on Windows

Windows `tar` cannot set the executable bit, and the deploy script rejects an archive whose `WebApi` is not executable. Pack with:

```powershell
python deploy/linux/pack-release.py publish-linux chemeats.tgz
```
