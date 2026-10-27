"""Private MPRIS-Quelle für echte playerctl-Abfragen ohne Desktop-Player."""

import json
import os

import dbus
import dbus.mainloop.glib
import dbus.service
from gi.repository import GLib


class Player(dbus.service.Object):
    """Ein unveränderlicher Player auf dem privaten Testbus."""

    def __init__(self, suffix, artist, title):
        bus = dbus.SessionBus(private=True)
        self.bus_name = dbus.service.BusName("org.mpris.MediaPlayer2." + suffix, bus)
        super().__init__(bus, "/org/mpris/MediaPlayer2", bus_name=self.bus_name)
        self.root = {
            "Identity": "LIS2 MPRIS fixture",
            "CanQuit": False,
            "CanRaise": False,
            "HasTrackList": False,
            "SupportedUriSchemes": dbus.Array(["file", "http", "https"], signature="s"),
            "SupportedMimeTypes": dbus.Array(["audio/mpeg"], signature="s"),
        }
        self.player = {
            "PlaybackStatus": "Playing",
            "Metadata": dbus.Dictionary({
                "mpris:trackid": dbus.ObjectPath("/org/mpris/MediaPlayer2/track/1"),
                "xesam:artist": dbus.Array([artist] if artist else [], signature="s"),
                "xesam:title": title,
                "xesam:album": "",
                "mpris:length": dbus.Int64(0),
            }, signature="sv"),
            "Position": dbus.Int64(0),
            "Rate": dbus.Double(1),
            "Volume": dbus.Double(1),
            "MinimumRate": dbus.Double(1),
            "MaximumRate": dbus.Double(1),
            "CanControl": True,
            "CanPlay": True,
            "CanPause": True,
            "CanSeek": False,
            "CanGoNext": False,
            "CanGoPrevious": False,
        }

    def properties(self, interface):
        if interface == "org.mpris.MediaPlayer2":
            return self.root
        if interface == "org.mpris.MediaPlayer2.Player":
            return self.player
        raise dbus.exceptions.DBusException("Unknown interface")

    @dbus.service.method("org.freedesktop.DBus.Properties", in_signature="ss", out_signature="v")
    def Get(self, interface, name):
        return self.properties(interface)[name]

    @dbus.service.method("org.freedesktop.DBus.Properties", in_signature="s", out_signature="a{sv}")
    def GetAll(self, interface):
        return self.properties(interface)


dbus.mainloop.glib.DBusGMainLoop(set_as_default=True)
players = [
    Player("lis2radio", "RADIO BOB - Symphonic Metal", "Eleine - We Are Legion"),
    Player("lis2file", "Björk", "Jóga & <Live>"),
    Player("lis2empty", "", "Artist - Combined stream title"),
]
print(json.dumps({"address": os.environ["DBUS_SESSION_BUS_ADDRESS"]}), flush=True)
GLib.MainLoop().run()
