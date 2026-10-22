#!/bin/sh
# Prüft das installierte Paket als gewöhnlicher Benutzer in einem frischen Debian-Container.
set -eu
package=$1
test "$(dpkg-deb --field "$package" Architecture)" = "$(dpkg --print-architecture)"
uname -m
cat /etc/os-release
export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y "$package" xvfb xauth dbus desktop-file-utils gnome-keyring
dpkg-query -W libc6 libfontconfig1 libx11-6 libsecret-tools playerctl
desktop-file-validate /usr/share/applications/lis2-control-center.desktop
test -f /usr/share/icons/hicolor/32x32/apps/lis2-control-center.png
useradd --create-home --shell /bin/sh lis2-smoke
runuser -u lis2-smoke -- sh -e <<'SMOKE'
  cd /home/lis2-smoke
  mkdir -p .local/share/LIS2ControlCenter/Avalonia
  printf "%s\n" "preserve-user-settings" > .local/share/LIS2ControlCenter/Avalonia/package-removal-probe
  export XDG_RUNTIME_DIR="$(mktemp -d)"
  dbus-run-session -- sh -e <<'SESSION'
    printf "%s" "lis2-isolated-package-password" | gnome-keyring-daemon --unlock --components=secrets
    printf "%s" "lis2-package-test" | secret-tool store --label="LIS2 package smoke" app lis2-package-smoke
    test "$(secret-tool lookup app lis2-package-smoke)" = "lis2-package-test"
    secret-tool clear app lis2-package-smoke
    timeout 90s xvfb-run -a lis2-control-center --desktop-smoke desktop-smoke --minimized
SESSION
  test -f desktop-smoke/result.txt
  cat desktop-smoke/result.txt
SMOKE
apt-get remove -y lis2-control-center
test ! -e /usr/bin/lis2-control-center
test ! -e /usr/lib/lis2-control-center/LIS2.App.Avalonia
test ! -e /usr/share/applications/lis2-control-center.desktop
test ! -e /usr/share/icons/hicolor/32x32/apps/lis2-control-center.png
grep -qx preserve-user-settings /home/lis2-smoke/.local/share/LIS2ControlCenter/Avalonia/package-removal-probe
echo 'PASS: Debian installation, desktop entry, non-root GUI smoke, removal, preserved user settings.'
