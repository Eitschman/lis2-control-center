// Das Testfenster bildet Winamps IPC-Vertrag ab, ohne Player oder Hardware zu benötigen.
#include "plugin.cpp"
#include <iostream>
#include <map>
#include <stdexcept>

namespace
{
std::wstring testFile, testTitle;
std::map<std::wstring, std::wstring> testTags;
int testState = 1;
int metadataReads = 0;
bool changeDuringRead = false;
DWORD windowThread = 0;
bool wrongThread = false;
int audioLevel = 0;

int __cdecl TestVu(int channel)
{
    if (channel == 0) audioLevel = audioLevel % 200 + 1;
    return channel == 0 ? audioLevel : 255 - audioLevel;
}

char* __cdecl TestSpectrum(char* buffer)
{
    std::fill_n(buffer, SpectrumSourceBins, static_cast<char>(audioLevel));
    return buffer;
}

void Require(bool valid, const char* message)
{
    if (!valid) throw std::runtime_error(message);
}

LRESULT CALLBACK TestWindowProc(HWND window, UINT message, WPARAM argument, LPARAM command)
{
    if (message != WM_WA_IPC) return DefWindowProcW(window, message, argument, command);
    if (GetCurrentThreadId() != windowThread) wrongThread = true;
    switch (command)
    {
    case IPC_ISPLAYING: return testState;
    case IPC_GETLISTPOS: return 0;
    case IPC_GETLISTLENGTH: return 1;
    case IPC_GET_PLAYING_FILENAME: return reinterpret_cast<LRESULT>(testFile.c_str());
    case IPC_GET_PLAYING_TITLE: return reinterpret_cast<LRESULT>(testTitle.c_str());
    case IPC_GET_EXTENDED_FILE_INFOW_HOOKABLE:
    {
        auto& info = *reinterpret_cast<extendedFileInfoStructW*>(argument);
        ++metadataReads;
        const auto found = testTags.find(info.metadata);
        if (changeDuringRead) { testFile = L"C:\\changed.mp3"; changeDuringRead = false; }
        if (found == testTags.end()) return 0;
        wcsncpy_s(info.ret, info.retlen, found->second.c_str(), _TRUNCATE);
        return 1;
    }
    default: return 0;
    }
}

void Has(const std::string& json, const std::string& field, const std::string& value)
{
    Require(json.find("\"" + field + "\":\"" + value + "\"") != std::string::npos, field.c_str());
}

void CheckMetadata(HWND window)
{
    testFile = L"C:\\音楽\\track.mp3";
    testTitle = L"Playlist title";
    testTags = {{L"artist", L"Björk"}, {L"title", L"東京"}, {L"album", L"Álbum"}};
    auto snapshot = BuildSnapshotJson(window);
    Has(snapshot, "artist", u8"Björk");
    Has(snapshot, "title", u8"東京");
    Has(snapshot, "album", u8"Álbum");
    Has(snapshot, "titleSource", "FileTags");
    Has(snapshot, "streamName", "");
    Require(snapshot.find("\"isStream\":false") != std::string::npos, "local file classification");
    const auto reads = metadataReads;
    BuildSnapshotJson(window);
    Require(reads == metadataReads, "metadata cache between audio frames");

    testFile = L"http://example.invalid/radio";
    testTitle = L"Radio playlist";
    testTags = {{L"streamname", L"RADIO BOB"}, {L"streamtitle", L"Lacrimosa - Titel"},
        {L"streamgenre", L"Metal"}, {L"streamurl", L"https://example.invalid/"}, {L"title", L"Old title"}};
    snapshot = BuildSnapshotJson(window);
    Has(snapshot, "artist", "");
    Has(snapshot, "album", "");
    Has(snapshot, "streamName", "RADIO BOB");
    Has(snapshot, "streamTitle", "Lacrimosa - Titel");
    Has(snapshot, "title", "Lacrimosa - Titel");
    Has(snapshot, "streamGenre", "Metal");
    Has(snapshot, "streamUrl", "https://example.invalid/");
    Has(snapshot, "titleSource", "StreamTitle");
    Require(snapshot.find("\"isStream\":true") != std::string::npos, "stream classification");

    testTags[L"streamtitle"] = L"Next artist - Next title";
    g_metadata.readAt = 0;
    Has(BuildSnapshotJson(window), "title", "Next artist - Next title");
    testTags.erase(L"streamtitle");
    g_metadata.readAt = 0;
    snapshot = BuildSnapshotJson(window);
    Has(snapshot, "title", "Old title");
    Has(snapshot, "titleSource", "StreamMetadata");
    testTags.clear();
    g_metadata.readAt = 0;
    snapshot = BuildSnapshotJson(window);
    Has(snapshot, "title", "Radio playlist");
    Has(snapshot, "titleSource", "Playlist");

    testFile = L"C:\\untagged.mp3";
    testTitle = L"Untagged";
    snapshot = BuildSnapshotJson(window);
    Has(snapshot, "streamTitle", "");
    Has(snapshot, "streamName", "");
    Has(snapshot, "title", "Untagged");
    testState = 0;
    snapshot = BuildSnapshotJson(window);
    Has(snapshot, "title", "");
    Has(snapshot, "titleSource", "None");

    testState = 1;
    testFile = L"C:\\first.mp3";
    changeDuringRead = true;
    Require(BuildSnapshotJson(window).empty(), "mixed snapshot rejected");
    Has(BuildSnapshotJson(window), "title", "Untagged");
    Require(!wrongThread, "IPC stays on window thread");
}
}

int main(int argc, char** argv)
{
    const bool slowCountercheck = argc == 2 && std::string(argv[1]) == "--slow-audio";
    windowThread = GetCurrentThreadId();
    WNDCLASSW windowClass{};
    windowClass.lpfnWndProc = TestWindowProc;
    windowClass.lpszClassName = L"LIS2WinampMetadataTests";
    windowClass.hInstance = GetModuleHandleW(nullptr);
    RegisterClassW(&windowClass);
    const auto window = CreateWindowW(windowClass.lpszClassName, L"", 0, 0, 0, 0, 0,
        HWND_MESSAGE, nullptr, windowClass.hInstance, nullptr);
    HANDLE telemetryRead = nullptr;
    try
    {
        Require(window != nullptr, "test window creation");
        CheckMetadata(window);
        g_pipe = CreateFileW(L"NUL", GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE,
            nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
        Require(g_pipe != INVALID_HANDLE_VALUE, "test output creation");
        g_latestSnapshot = BuildSnapshotJson(window);
        Require(!g_latestSnapshot.empty(), "captured snapshot available");
        SendSnapshot();
        Require(g_latestSnapshot.empty(), "captured snapshots are consumed only once");
        SendSnapshot();
        Require(g_latestSnapshot.empty(), "no repeated stale snapshot");
        ClosePipe();
        // Eine echte Pipe prüft Erfassung und Worker gemeinsam, ohne einen installierten Player.
        Require(CreatePipe(&telemetryRead, &g_pipe, nullptr, 65536) != FALSE, "audio test pipe");
        g_plugin.hwndParent = window;
        Require(PluginInit() == 0, "plugin initialization");
        const auto timer = g_timer;
        Require(timer != 0, "capture timer installed");
        g_vuGet = TestVu;
        g_saGet = TestSpectrum;
        if (slowCountercheck)
            Require(SetTimer(window, timer, 200, CaptureSnapshot) != 0, "slow countercheck timer");
        std::string received;
        const auto started = GetTickCount64();
        while (GetTickCount64() - started < 900)
        {
            MSG message{};
            while (PeekMessageW(&message, nullptr, 0, 0, PM_REMOVE)) DispatchMessageW(&message);
            DWORD available = 0;
            Require(PeekNamedPipe(telemetryRead, nullptr, 0, nullptr, &available, nullptr) != FALSE,
                "audio pipe remains readable");
            if (available > 0)
            {
                std::vector<char> buffer(available);
                DWORD read = 0;
                Require(ReadFile(telemetryRead, buffer.data(), available, &read, nullptr) != FALSE,
                    "read audio snapshots");
                received.append(buffer.data(), read);
            }
            Sleep(1);
        }
        const auto frames = std::count(received.begin(), received.end(), '\n');
        const bool cadencePassed = frames >= 7;
        Require(slowCountercheck ? !cadencePassed : cadencePassed,
            "audio cadence: require seven snapshots within 900 ms; slow countercheck must fail that limit");
        Require(received.find("\"vuLeft\":1,\"vuRight\":254") != std::string::npos,
            "fresh stereo peaks cross the worker pipe");
        Require(received.find("\"spectrum\":[1,1,1,") != std::string::npos,
            "fresh spectrum crosses the worker pipe");
        PluginQuit();
        CloseHandle(telemetryRead);
        telemetryRead = nullptr;
        const auto reads = metadataReads;
        CaptureSnapshot(window, 0, timer, 0);
        Require(reads == metadataReads, "no capture after shutdown");
        DestroyWindow(window);
        std::cout << "Winamp Unicode, file/stream transitions, title origins, cache, consistency and lifecycle: OK\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << error.what() << '\n';
        PluginQuit();
        if (telemetryRead != nullptr) CloseHandle(telemetryRead);
        if (window) DestroyWindow(window);
        return 1;
    }
}
