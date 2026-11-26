#!/bin/bash
# ChemEats production deploy — installed as root:root 0755 at /usr/local/sbin/chemeats-deploy
# and allowed without password for the deploy user via /etc/sudoers.d/chemeats-deploy.
#
#   sudo chemeats-deploy            deploy /tmp/chemeats.tgz (uploaded by the calling user)
#   sudo chemeats-deploy --rollback restore /var/www/chemeats.old
#   sudo chemeats-deploy --check    preflight only (no changes)
#
# Never overwrites App_Data/ or appsettings.Production.json.
set -euo pipefail

APP_DIR=/var/www/chemeats
OLD_DIR=/var/www/chemeats.old
ARCHIVE=/tmp/chemeats.tgz
SERVICE=chemeats
APP_USER=www-data
HEALTH_URL=https://127.0.0.1:12345/

die() { echo "ERROR: $*" >&2; exit 1; }
[[ $EUID -eq 0 ]] || die "run with sudo"
CALLER=${SUDO_USER:-}
[[ -n $CALLER && $CALLER != root ]] || die "run via sudo from the deploy user, not as root"

health() {
  local code i
  for i in $(seq 1 15); do
    sleep 2
    systemctl is-active --quiet "$SERVICE" || continue
    code=$(curl -sk -o /dev/null -w '%{http_code}' "$HEALTH_URL" || true)
    [[ $code == 200 ]] && { echo "OK: $SERVICE active, HTTP $code"; return 0; }
  done
  echo "FAILED: $SERVICE state=$(systemctl is-active "$SERVICE" || true), HTTP ${code:-none}" >&2
  journalctl -u "$SERVICE" -n 30 --no-pager >&2 || true
  return 1
}

check() {
  command -v rsync >/dev/null || die "rsync missing (apt install rsync)"
  [[ -d $APP_DIR ]] || die "$APP_DIR not found"
  [[ -f $ARCHIVE ]] || die "$ARCHIVE not found — upload it first"
  [[ $(stat -c %U "$ARCHIVE") == "$CALLER" ]] || die "$ARCHIVE is not owned by $CALLER"
}

case "${1:-}" in
  --check)
    check; echo "OK: preflight passed"; exit 0 ;;
  --rollback)
    [[ -d $OLD_DIR ]] || die "$OLD_DIR not found — nothing to roll back to"
    systemctl stop "$SERVICE"
    rm -rf "$APP_DIR.failed"
    mv "$APP_DIR" "$APP_DIR.failed"
    mv "$OLD_DIR" "$APP_DIR"
    systemctl start "$SERVICE"
    echo "Rolled back. The broken version is kept in $APP_DIR.failed"
    health; exit $? ;;
  "") ;;
  *) die "unknown option: $1" ;;
esac

check
STAGE=$(mktemp -d /tmp/chemeats-new.XXXXXX)
chown "$CALLER" "$STAGE"
trap 'rm -rf "$STAGE"' EXIT

# Extract as the calling user, never as root (the archive is user-supplied).
runuser -u "$CALLER" -- tar -xzf "$ARCHIVE" -C "$STAGE" --no-same-owner
[[ -x $STAGE/WebApi && -d $STAGE/wwwroot/_framework ]] || die "archive does not look like a ChemEats linux-x64 publish"

systemctl stop "$SERVICE"
rm -rf "$OLD_DIR"
cp -a "$APP_DIR" "$OLD_DIR"
rsync -a --delete --exclude 'App_Data/' --exclude 'appsettings.Production.json' "$STAGE/" "$APP_DIR/"
chmod 755 "$APP_DIR/WebApi"
chown -R "$APP_USER:$APP_USER" "$APP_DIR"
systemctl start "$SERVICE"
rm -f "$ARCHIVE"

echo "Deployed. Previous version kept in $OLD_DIR"
health
