#!/bin/bash
# ChemEats production deploy — installed as root:root 0755 at /usr/local/sbin/chemeats-deploy
# and allowed without password for the deploy user via /etc/sudoers.d/chemeats-deploy.
#
#   sudo chemeats-deploy            deploy /tmp/chemeats.tgz (uploaded by the calling user)
#   sudo chemeats-deploy --rollback restore /var/www/chemeats.old
#   sudo chemeats-deploy --check    preflight only (no changes)
#
# The real work runs detached from the ssh session (it survives a dropped connection or Ctrl+C);
# its output goes to /var/log/chemeats/deploy.log and is streamed to the caller.
# Never overwrites App_Data/ or appsettings.Production.json. Files are compared by content (--checksum).
set -euo pipefail

APP_DIR=/var/www/chemeats
OLD_DIR=/var/www/chemeats.old
ARCHIVE=/tmp/chemeats.tgz
SERVICE=chemeats
APP_USER=www-data
HEALTH_URL=https://127.0.0.1:12345/
LOG=/var/log/chemeats/deploy.log
LOCK=/run/chemeats-deploy.lock

die() { echo "ERROR: $*" >&2; exit 1; }
log() { echo "[$(date '+%F %T')] $*"; }
[[ $EUID -eq 0 ]] || die "run with sudo"

health() {
  local code="" i
  for i in $(seq 1 15); do
    sleep 2
    systemctl is-active --quiet "$SERVICE" || continue
    code=$(curl -sk -o /dev/null -w '%{http_code}' "$HEALTH_URL" || true)
    [[ $code == 200 ]] && { log "OK: $SERVICE active, HTTP $code"; return 0; }
  done
  log "FAILED: $SERVICE state=$(systemctl is-active "$SERVICE" || true), HTTP ${code:-none}"
  journalctl -u "$SERVICE" -n 30 --no-pager || true
  return 1
}

check() {
  command -v rsync >/dev/null || die "rsync missing (apt install rsync)"
  [[ -d $APP_DIR ]] || die "$APP_DIR not found"
  [[ -f $ARCHIVE ]] || die "$ARCHIVE not found — upload it first"
  [[ $(stat -c %U "$ARCHIVE") == "$CALLER" ]] || die "$ARCHIVE is not owned by $CALLER"
}

# ---------------------------------------------------------------- worker (detached)
if [[ ${1:-} == --worker ]]; then
  ACTION=$2 CALLER=$3
  trap '' HUP INT PIPE                       # survive the ssh session going away
  exec 9>"$LOCK"; flock -n 9 || die "another deploy is already running"

  PHASE=none
  recover() {
    local rc=$?
    [[ $rc -eq 0 ]] && return
    case $PHASE in
      stopped)   # app files untouched; backup may be incomplete — just start the current version
        log "RECOVER: failed before files changed — starting the current version again"
        rm -rf "$OLD_DIR.new"
        systemctl start "$SERVICE" || true ;;
      syncing)   # app files partially replaced — restore the complete backup
        log "RECOVER: failed while replacing files — restoring $OLD_DIR"
        rsync -a --checksum --delete "$OLD_DIR/" "$APP_DIR/" && chown -R "$APP_USER:$APP_USER" "$APP_DIR"
        systemctl start "$SERVICE" || true ;;
    esac
    log "EXIT $rc (phase: $PHASE)"
  }
  trap recover EXIT

  if [[ $ACTION == rollback ]]; then
    [[ -d $OLD_DIR ]] || die "$OLD_DIR not found — nothing to roll back to"
    log "Rollback started by $CALLER"
    systemctl stop "$SERVICE"; PHASE=stopped
    rm -rf "$APP_DIR.failed"
    mv "$APP_DIR" "$APP_DIR.failed"
    mv "$OLD_DIR" "$APP_DIR"; PHASE=done
    systemctl start "$SERVICE"
    log "Rolled back. The replaced version is kept in $APP_DIR.failed"
    health; exit $?
  fi

  log "Deploy started by $CALLER"
  check
  STAGE=$(mktemp -d /tmp/chemeats-new.XXXXXX)
  chown "$CALLER" "$STAGE"
  # Extract as the calling user, never as root (the archive is user-supplied).
  runuser -u "$CALLER" -- tar -xzf "$ARCHIVE" -C "$STAGE" --no-same-owner
  [[ -x $STAGE/WebApi && -d $STAGE/wwwroot/_framework ]] || die "archive does not look like a ChemEats linux-x64 publish"

  systemctl stop "$SERVICE"; PHASE=stopped
  log "Service stopped, backing up the current version"
  rm -rf "$OLD_DIR.new"
  cp -a "$APP_DIR" "$OLD_DIR.new"            # build the backup aside, then swap: .old is never half-written
  rm -rf "$OLD_DIR"
  mv "$OLD_DIR.new" "$OLD_DIR"

  PHASE=syncing
  log "Replacing files (App_Data/ and appsettings.Production.json are kept)"
  rsync -a --checksum --delete --exclude 'App_Data/' --exclude 'appsettings.Production.json' "$STAGE/" "$APP_DIR/"
  chmod 755 "$APP_DIR/WebApi"
  chown -R "$APP_USER:$APP_USER" "$APP_DIR"
  PHASE=done

  rm -rf "$STAGE" "$ARCHIVE"
  systemctl start "$SERVICE" || log "systemctl start failed"   # health() below reports state + journal
  log "Deployed. Previous version kept in $OLD_DIR"
  health; exit $?
fi

# ---------------------------------------------------------------- front end (called via sudo)
CALLER=${SUDO_USER:-}
[[ -n $CALLER && $CALLER != root ]] || die "run via sudo from the deploy user, not as root"

case "${1:-}" in
  --check)    check; echo "OK: preflight passed"; exit 0 ;;
  --rollback) ACTION=rollback ;;
  "")         ACTION=deploy ;;
  *)          die "unknown option: $1" ;;
esac

mkdir -p "$(dirname "$LOG")"
: > "$LOG"
setsid "$0" --worker "$ACTION" "$CALLER" </dev/null >>"$LOG" 2>&1 &
WORKER=$!
echo "Running detached (pid $WORKER); log: $LOG — safe to disconnect, it will finish anyway."
tail -n +1 -f --pid="$WORKER" "$LOG" 2>/dev/null || true
wait "$WORKER"
