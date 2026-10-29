"""Privater MPRIS-/PipeWire-Player mit echtem Testton und steuerbarem Streamneustart."""

import json
import math
import os
import pathlib
import signal
import struct
import subprocess
import sys
import tempfile
import time
import wave

import dbus
import dbus.mainloop.glib
import dbus.service
from gi.repository import GLib


class Player(dbus.service.Object):
    def __init__(self):
        self.bus_name = dbus.service.BusName("org.mpris.MediaPlayer2.lis2audio", dbus.SessionBus())
        super().__init__(dbus.SessionBus(), "/org/mpris/MediaPlayer2", bus_name=self.bus_name)
        self.state = "Playing"
        self.title = "Test tone 1000 Hz"

    @dbus.service.method("org.freedesktop.DBus.Properties", in_signature="s", out_signature="a{sv}")
    def GetAll(self, interface):
        if interface == "org.mpris.MediaPlayer2":
            return {"Identity": "LIS2 audio fixture", "CanQuit": False, "CanRaise": False,
                    "HasTrackList": False, "SupportedUriSchemes": dbus.Array(["file"], signature="s"),
                    "SupportedMimeTypes": dbus.Array(["audio/wav"], signature="s")}
        return {"PlaybackStatus": self.state, "Position": dbus.Int64(0), "Rate": dbus.Double(1),
                "Volume": dbus.Double(1), "MinimumRate": dbus.Double(1), "MaximumRate": dbus.Double(1),
                "CanControl": True, "CanPlay": True, "CanPause": True, "CanSeek": False,
                "CanGoNext": False, "CanGoPrevious": False,
                "Metadata": dbus.Dictionary({"mpris:trackid": dbus.ObjectPath("/track/1"),
                    "xesam:title": self.title, "xesam:artist": dbus.Array(["LIS2 fixture"], signature="s"),
                    "xesam:album": "", "mpris:length": dbus.Int64(120000000)}, signature="sv")}

    @dbus.service.method("org.freedesktop.DBus.Properties", in_signature="ss", out_signature="v")
    def Get(self, interface, name):
        return self.GetAll(interface)[name]


def run_player(directory, pulse=False):
    playback = None
    runtime = pathlib.Path(directory)
    dbus.mainloop.glib.DBusGMainLoop(set_as_default=True)
    player = Player()
    loop = GLib.MainLoop()

    def stop_playback():
        nonlocal playback
        if playback is not None and playback.poll() is None:
            playback.terminate()
            playback.wait(timeout=5)
        playback = None

    def play(frequency):
        nonlocal playback
        stop_playback()
        audio = runtime / "tone.wav"
        samples = b"".join(struct.pack("<hh", int(16000 * math.sin(2 * math.pi * frequency * frame / 44100)),
                                          int(8000 * math.sin(2 * math.pi * frequency * frame / 44100)))
                           for frame in range(44100))
        with wave.open(str(audio), "wb") as output:
            output.setparams((2, 2, 44100, 0, "NONE", "not compressed"))
            output.writeframes(samples * 120)
        arguments = ["paplay", "--device=lis2-test-sink", str(audio)] if pulse else [
            "pw-play", "--target=lis2-test-sink", "--properties={ node.name=lis2-test-player }", str(audio)]
        playback = subprocess.Popen(arguments, stdout=subprocess.DEVNULL, stderr=sys.stderr)

    def command(channel, condition):
        line = channel.readline().strip()
        if line == "restart":
            play(5000)
            player.title = "Test tone 5000 Hz"
            player.state = "Playing"
        elif line in ("pause", "silence"):
            stop_playback()
            if line == "pause": player.state = "Paused"
        elif line == "resume":
            play(1000)
            player.state = "Playing"
        elif line == "stop" or not line:
            loop.quit()
            return False
        print(json.dumps({"command": line}), flush=True)
        return True

    try:
        play(1000)
        GLib.io_add_watch(sys.stdin, GLib.IO_IN | GLib.IO_HUP, command)
        signal.signal(signal.SIGTERM, lambda *_: loop.quit())
        print(json.dumps({"address": os.environ["DBUS_SESSION_BUS_ADDRESS"], "runtime": directory,
                          "pid": os.getpid()}), flush=True)
        loop.run()
    finally:
        stop_playback()


if len(sys.argv) > 1 and sys.argv[1] == "sandbox-player":
    run_player(sys.argv[2], pulse=True)
    sys.exit(0)


with tempfile.TemporaryDirectory(prefix="lis2-private-audio-") as directory:
    runtime = pathlib.Path(directory)
    os.environ["XDG_RUNTIME_DIR"] = str(runtime)
    os.environ["PIPEWIRE_RUNTIME_DIR"] = str(runtime)
    processes = []
    mode = sys.argv[1] if len(sys.argv) > 1 else "native"
    log_path = runtime / "server.log"
    with log_path.open("w") as log:
        def launch(arguments):
            process = subprocess.Popen(arguments, stdout=log, stderr=log)
            processes.append(process)
            return process

        try:
            launch(["pipewire"])
            for attempt in range(100):
                if (runtime / "pipewire-0").exists():
                    break
                time.sleep(0.05)
            else:
                raise RuntimeError("Private PipeWire startup failed: " + log_path.read_text())
            launch(["wireplumber"])
            subprocess.run(["pw-cli", "create-node", "adapter",
                "{ factory.name=support.null-audio-sink node.name=lis2-test-sink media.class=Audio/Sink "
                "object.linger=true audio.position=[ FL FR ] }"], check=True, stdout=log, stderr=log)
            if mode != "native":
                os.environ["PULSE_SERVER"] = "unix:" + str(runtime / "pulse/native")
                launch(["pipewire-pulse"])
                for attempt in range(100):
                    if (runtime / "pulse/native").exists(): break
                    time.sleep(0.05)
                else: raise RuntimeError("Private Pulse server startup failed: " + log_path.read_text())
            if mode == "sandbox":
                host_address = os.environ["DBUS_SESSION_BUS_ADDRESS"]
                marker = runtime / "sandbox-info"
                marker.write_text("[Application]\nname=org.example.Lis2Audio\n[Instance]\ninstance-id=12345678\n[Context]\nsockets=pulseaudio;\n")
                sandbox = ["bwrap", "--die-with-parent", "--ro-bind", "/usr", "/usr", "--ro-bind", "/lib", "/lib",
                    "--ro-bind-try", "/lib64", "/lib64", "--ro-bind", "/etc", "/etc", "--bind", "/tmp", "/tmp",
                    "--ro-bind", str(marker), "/.flatpak-info", "--proc", "/proc", "--dev-bind", "/dev", "/dev"]
                proxy = runtime / "session-bus"
                launch(sandbox + ["--", "xdg-dbus-proxy", os.environ["DBUS_SESSION_BUS_ADDRESS"], str(proxy),
                    "--filter", "--own=org.mpris.MediaPlayer2.lis2audio"])
                for attempt in range(100):
                    if proxy.exists(): break
                    time.sleep(0.05)
                else: raise RuntimeError("Private D-Bus proxy startup failed: " + log_path.read_text())
                os.environ["DBUS_SESSION_BUS_ADDRESS"] = "unix:path=" + str(proxy)
                actor = subprocess.Popen(sandbox + ["--unshare-pid", "--", "/usr/bin/python3", __file__,
                    "sandbox-player", directory], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=log, text=True)
                processes.append(actor)
                ready = json.loads(actor.stdout.readline())
                # Der Host muss den ursprünglichen Bus abfragen, nicht den Sandbox-Proxy.
                ready["address"] = host_address
                print(json.dumps(ready), flush=True)
                for line in sys.stdin:
                    actor.stdin.write(line)
                    actor.stdin.flush()
                    if line.strip() == "stop": break
                    print(actor.stdout.readline().strip(), flush=True)
            else:
                run_player(directory, pulse=mode == "pulse")
        finally:
            for process in reversed(processes):
                if process.poll() is None:
                    process.terminate()
                    try:
                        process.wait(timeout=5)
                    except subprocess.TimeoutExpired:
                        process.kill()
                        process.wait(timeout=5)
