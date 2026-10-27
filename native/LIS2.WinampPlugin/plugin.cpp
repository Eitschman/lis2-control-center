#include "winamp_sdk_min.h"

#include <algorithm>
#include <array>
#include <cstdint>
#include <sstream>
#include <string>
#include <vector>
#include <mutex>

namespace
{
constexpr wchar_t PipeName[] = L"\\\\.\\pipe\\LIS2ControlCenter.Winamp";
constexpr DWORD PollIntervalMs = 100;
constexpr int SpectrumSourceBins = 75;
constexpr int SpectrumOutputBins = 20;

using SaGetFunc = char* (__cdecl*)(char*);
using SaSetReqFunc = void (__cdecl*)(int);
using VuGetFunc = int (__cdecl*)(int);

HANDLE g_stopEvent = nullptr;
HANDLE g_snapshotReady = nullptr;
HANDLE g_thread = nullptr;
HANDLE g_pipe = INVALID_HANDLE_VALUE;
UINT_PTR g_timer = 0;
std::mutex g_snapshotMutex;
std::string g_latestSnapshot;

SaGetFunc g_saGet = nullptr;
SaSetReqFunc g_saSetReq = nullptr;
VuGetFunc g_vuGet = nullptr;

char g_description[] = "LIS2 Control Center (gen_lis2.dll)";

bool IsValidFunctionPointer(LRESULT raw)
{
    return raw != 0 && raw != 1 && raw != -1;
}

std::string JsonEscape(const std::string& value)
{
    std::ostringstream output;

    for (const unsigned char ch : value)
    {
        switch (ch)
        {
        case '\\':
            output << "\\\\";
            break;
        case '"':
            output << "\\\"";
            break;
        case '\b':
            output << "\\b";
            break;
        case '\f':
            output << "\\f";
            break;
        case '\n':
            output << "\\n";
            break;
        case '\r':
            output << "\\r";
            break;
        case '\t':
            output << "\\t";
            break;
        default:
            if (ch < 0x20)
            {
                static constexpr char Hex[] = "0123456789ABCDEF";
                output << "\\u00"
                       << Hex[(ch >> 4) & 0x0F]
                       << Hex[ch & 0x0F];
            }
            else
            {
                output << static_cast<char>(ch);
            }
            break;
        }
    }

    return output.str();
}

// Winamp liefert UTF-16. Die Umwandlung erfolgt erst für den UTF-8-Pipevertrag.
std::string WideToUtf8(const wchar_t* value)
{
    if (value == nullptr || *value == L'\0') return {};
    const int length = WideCharToMultiByte(CP_UTF8, 0, value, -1, nullptr, 0, nullptr, nullptr);
    if (length <= 1) return {};
    std::string result(static_cast<std::size_t>(length), '\0');
    if (!WideCharToMultiByte(CP_UTF8, 0, value, -1, result.data(), length, nullptr, nullptr)) return {};
    result.pop_back();
    return result;
}

std::wstring ReadCurrentText(HWND window, LPARAM message)
{
    const auto raw = SendMessageW(window, WM_WA_IPC, 0, message);
    return IsValidFunctionPointer(raw) ? std::wstring(reinterpret_cast<const wchar_t*>(raw)) : std::wstring{};
}

std::string GetExtendedInfo(HWND window, const wchar_t* filename, const wchar_t* metadata)
{
    if (filename == nullptr || *filename == L'\0') return {};
    wchar_t buffer[2048]{};
    extendedFileInfoStructW info{filename, metadata, buffer, std::size(buffer)};
    auto result = SendMessageW(window, WM_WA_IPC, reinterpret_cast<WPARAM>(&info), IPC_GET_EXTENDED_FILE_INFOW_HOOKABLE);
    if (result == 0)
        result = SendMessageW(window, WM_WA_IPC, reinterpret_cast<WPARAM>(&info), IPC_GET_EXTENDED_FILE_INFOW);
    return result != 0 ? WideToUtf8(buffer) : std::string{};
}

struct MetadataSnapshot
{
    std::wstring file;
    std::wstring playlistTitle;
    std::string artist, title, album, streamName, streamTitle, streamGenre, streamUrl;
    std::string titleSource = "None";
    bool isStream = false;
    ULONGLONG readAt = 0;
};
MetadataSnapshot g_metadata;

// Der Aufrufer ist Winamps Fensterthread; Zeiger werden vor der nächsten IPC-Abfrage kopiert.
MetadataSnapshot ReadMetadata(HWND window, const std::wstring& file, const std::wstring& playlistTitle)
{
    MetadataSnapshot metadata;
    metadata.file = file;
    metadata.playlistTitle = playlistTitle;
    metadata.readAt = GetTickCount64();
    const auto schemeEnd = file.find(L"://");
    metadata.isStream = schemeEnd != std::wstring::npos && _wcsnicmp(file.c_str(), L"file:", 5) != 0;
    metadata.artist = GetExtendedInfo(window, file.c_str(), L"artist");
    metadata.title = GetExtendedInfo(window, file.c_str(), L"title");
    metadata.album = GetExtendedInfo(window, file.c_str(), L"album");
    if (!metadata.title.empty()) metadata.titleSource = metadata.isStream ? "StreamMetadata" : "FileTags";
    if (metadata.isStream)
    {
        metadata.streamName = GetExtendedInfo(window, file.c_str(), L"streamname");
        metadata.streamTitle = GetExtendedInfo(window, file.c_str(), L"streamtitle");
        metadata.streamGenre = GetExtendedInfo(window, file.c_str(), L"streamgenre");
        metadata.streamUrl = GetExtendedInfo(window, file.c_str(), L"streamurl");
        if (!metadata.streamTitle.empty())
        {
            metadata.title = metadata.streamTitle;
            metadata.titleSource = "StreamTitle";
        }
    }
    if (metadata.title.empty() && !playlistTitle.empty())
    {
        metadata.title = WideToUtf8(playlistTitle.c_str());
        metadata.titleSource = "Playlist";
    }
    return metadata;
}

const char* PlaybackStateName(int state)
{
    switch (state)
    {
    case 1:
        return "playing";
    case 3:
        return "paused";
    default:
        return "stopped";
    }
}

void AppendJsonString(
    std::ostringstream& json,
    const char* name,
    const std::string& value)
{
    json << ",\"" << name << "\":\""
         << JsonEscape(value)
         << "\"";
}

std::array<int, SpectrumOutputBins> ReadSpectrum()
{
    std::array<int, SpectrumOutputBins> result{};

    if (g_saGet == nullptr)
        return result;

    std::array<char, SpectrumSourceBins * 2 + 8> buffer{};
    const char* data = g_saGet(buffer.data());
    if (data == nullptr)
        return result;

    for (int output = 0; output < SpectrumOutputBins; ++output)
    {
        const int begin = output * SpectrumSourceBins / SpectrumOutputBins;
        const int end = (output + 1) * SpectrumSourceBins / SpectrumOutputBins;

        int peak = 0;
        for (int source = begin; source < end; ++source)
            peak = std::max(
                peak,
                static_cast<int>(
                    static_cast<unsigned char>(data[source])));

        result[output] = std::clamp(peak, 0, 255);
    }

    return result;
}

int ReadVu(int channel)
{
    if (g_vuGet == nullptr)
        return -1;

    const int value = g_vuGet(channel);
    return value < 0 ? -1 : std::clamp(value, 0, 255);
}

std::string BuildSnapshotJson(HWND hwndWinamp)
{
    const int state = static_cast<int>(
        SendMessage(hwndWinamp, WM_WA_IPC, 0, IPC_ISPLAYING));

    const int playlistPosition = static_cast<int>(
        SendMessage(hwndWinamp, WM_WA_IPC, 0, IPC_GETLISTPOS));

    const int playlistCount = static_cast<int>(
        SendMessage(hwndWinamp, WM_WA_IPC, 0, IPC_GETLISTLENGTH));

    const int elapsedMs = static_cast<int>(
        SendMessage(hwndWinamp, WM_WA_IPC, 0, IPC_GETOUTPUTTIME));

    const int durationSeconds = static_cast<int>(
        SendMessage(hwndWinamp, WM_WA_IPC, 1, IPC_GETOUTPUTTIME));

    const int bitrateKbps = static_cast<int>(
        SendMessage(hwndWinamp, WM_WA_IPC, 1, IPC_GETINFO));

    const int sampleRateHz = static_cast<int>(
        SendMessage(hwndWinamp, WM_WA_IPC, 5, IPC_GETINFO));

    const auto file = ReadCurrentText(hwndWinamp, IPC_GET_PLAYING_FILENAME);
    const auto playlistTitle = ReadCurrentText(hwndWinamp, IPC_GET_PLAYING_TITLE);
    if (state == 0 || file.empty())
        g_metadata = {};
    else if (g_metadata.file != file || g_metadata.playlistTitle != playlistTitle ||
             GetTickCount64() - g_metadata.readAt >= 1000)
        g_metadata = ReadMetadata(hwndWinamp, file, playlistTitle);

    // Ein reentrant ausgelöster Quellenwechsel verwirft den gemischten Snapshot.
    if (file != ReadCurrentText(hwndWinamp, IPC_GET_PLAYING_FILENAME) ||
        playlistTitle != ReadCurrentText(hwndWinamp, IPC_GET_PLAYING_TITLE) ||
        playlistPosition != SendMessageW(hwndWinamp, WM_WA_IPC, 0, IPC_GETLISTPOS) ||
        state != SendMessageW(hwndWinamp, WM_WA_IPC, 0, IPC_ISPLAYING))
    {
        g_metadata = {};
        return {};
    }

    const int vuLeft = state == 1 ? ReadVu(0) : 0;
    const int vuRight = state == 1 ? ReadVu(1) : 0;
    const auto spectrum = state == 1
        ? ReadSpectrum()
        : std::array<int, SpectrumOutputBins>{};

    std::ostringstream json;
    json << "{\"type\":\"snapshot\",\"state\":\""
         << PlaybackStateName(state)
         << "\"";

    AppendJsonString(json, "artist", g_metadata.artist);
    AppendJsonString(json, "title", g_metadata.title);
    AppendJsonString(json, "album", g_metadata.album);
    AppendJsonString(json, "streamName", g_metadata.streamName);
    AppendJsonString(json, "streamTitle", g_metadata.streamTitle);
    AppendJsonString(json, "streamGenre", g_metadata.streamGenre);
    AppendJsonString(json, "streamUrl", g_metadata.streamUrl);
    AppendJsonString(json, "titleSource", g_metadata.titleSource);
    json << ",\"isStream\":" << (g_metadata.isStream ? "true" : "false");

    if (playlistPosition >= 0)
        json << ",\"playlistPosition\":" << (playlistPosition + 1);

    if (playlistCount >= 0)
        json << ",\"playlistCount\":" << playlistCount;

    if (elapsedMs >= 0)
        json << ",\"elapsedSeconds\":" << (elapsedMs / 1000.0);

    if (durationSeconds >= 0)
        json << ",\"durationSeconds\":" << durationSeconds;

    if (bitrateKbps > 0)
        json << ",\"bitrateKbps\":" << bitrateKbps;

    if (sampleRateHz > 0)
        json << ",\"sampleRateHz\":" << sampleRateHz;

    if (vuLeft >= 0)
        json << ",\"vuLeft\":" << vuLeft;

    if (vuRight >= 0)
        json << ",\"vuRight\":" << vuRight;

    json << ",\"spectrum\":[";
    for (int index = 0; index < SpectrumOutputBins; ++index)
    {
        if (index > 0)
            json << ',';

        json << spectrum[index];
    }
    json << "]}";

    return json.str();
}

void ClosePipe()
{
    if (g_pipe == INVALID_HANDLE_VALUE)
        return;

    CloseHandle(g_pipe);
    g_pipe = INVALID_HANDLE_VALUE;
}

bool EnsurePipe()
{
    if (g_pipe != INVALID_HANDLE_VALUE)
        return true;

    g_pipe = CreateFileW(
        PipeName,
        GENERIC_WRITE,
        0,
        nullptr,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        nullptr);

    return g_pipe != INVALID_HANDLE_VALUE;
}

// Metadaten und Pegel werden im Fensterthread erfasst; Pipe-I/O blockiert ihn nicht.
void CALLBACK CaptureSnapshot(HWND window, UINT, UINT_PTR timer, DWORD)
{
    // Bereits zugestellte Timerbotschaften dürfen nach PluginQuit nicht mehr lesen.
    if (timer != 0 && timer != g_timer) return;
    auto snapshot = BuildSnapshotJson(window);
    {
        std::lock_guard<std::mutex> lock(g_snapshotMutex);
        g_latestSnapshot = std::move(snapshot);
    }
    // Neue Audiowerte wecken den Sender sofort, ohne einen zweiten Polling-Takt.
    if (g_snapshotReady != nullptr) SetEvent(g_snapshotReady);
}

void SendSnapshot()
{
    if (!EnsurePipe())
        return;

    std::string payload;
    {
        std::lock_guard<std::mutex> lock(g_snapshotMutex);
        // Jeder erfasste Stand wird höchstens einmal gesendet, damit ein hängender Player als veraltet erkennbar bleibt.
        payload.swap(g_latestSnapshot);
    }
    if (payload.empty()) return;
    payload.push_back('\n');

    DWORD written = 0;

    const BOOL ok = WriteFile(
        g_pipe,
        payload.data(),
        static_cast<DWORD>(payload.size()),
        &written,
        nullptr);

    if (!ok || written != payload.size())
        ClosePipe();
}

DWORD WINAPI WorkerThread(void*)
{
    const HANDLE events[] = {g_stopEvent, g_snapshotReady};
    while (true)
    {
        if (WaitForMultipleObjects(2, events, FALSE, INFINITE) != WAIT_OBJECT_0 + 1)
            break;
        SendSnapshot();
    }

    ClosePipe();
    return 0;
}

int PluginInit();
void PluginConfig();
void PluginQuit();

winampGeneralPurposePlugin g_plugin
{
    GPPHDR_VER,
    g_description,
    PluginInit,
    PluginConfig,
    PluginQuit,
    nullptr,
    nullptr
};

int PluginInit()
{
    if (g_stopEvent != nullptr || g_thread != nullptr)
        return 0;

    const LRESULT saGet = SendMessage(
        g_plugin.hwndParent,
        WM_WA_IPC,
        2,
        IPC_GETSADATAFUNC);

    const LRESULT saSetReq = SendMessage(
        g_plugin.hwndParent,
        WM_WA_IPC,
        1,
        IPC_GETSADATAFUNC);

    const LRESULT vuGet = SendMessage(
        g_plugin.hwndParent,
        WM_WA_IPC,
        0,
        IPC_GETVUDATAFUNC);

    if (IsValidFunctionPointer(saGet))
        g_saGet = reinterpret_cast<SaGetFunc>(saGet);

    if (IsValidFunctionPointer(saSetReq))
        g_saSetReq = reinterpret_cast<SaSetReqFunc>(saSetReq);

    if (IsValidFunctionPointer(vuGet))
        g_vuGet = reinterpret_cast<VuGetFunc>(vuGet);

    if (g_saSetReq != nullptr)
        g_saSetReq(1);

    g_stopEvent = CreateEventW(
        nullptr,
        TRUE,
        FALSE,
        nullptr);

    if (g_stopEvent == nullptr)
        return 1;

    g_snapshotReady = CreateEventW(nullptr, FALSE, FALSE, nullptr);
    if (g_snapshotReady == nullptr)
    {
        CloseHandle(g_stopEvent);
        g_stopEvent = nullptr;
        return 1;
    }

    g_metadata = {};
    CaptureSnapshot(g_plugin.hwndParent, 0, 0, 0);
    g_timer = SetTimer(g_plugin.hwndParent, 0, PollIntervalMs, CaptureSnapshot);
    if (g_timer == 0)
    {
        CloseHandle(g_snapshotReady);
        g_snapshotReady = nullptr;
        CloseHandle(g_stopEvent);
        g_stopEvent = nullptr;
        return 1;
    }

    g_thread = CreateThread(
        nullptr,
        0,
        WorkerThread,
        g_plugin.hwndParent,
        0,
        nullptr);

    if (g_thread == nullptr)
    {
        KillTimer(g_plugin.hwndParent, g_timer);
        g_timer = 0;
        CloseHandle(g_snapshotReady);
        g_snapshotReady = nullptr;
        CloseHandle(g_stopEvent);
        g_stopEvent = nullptr;
        return 1;
    }

    return 0;
}

void PluginConfig()
{
    MessageBoxA(
        g_plugin.hwndParent,
        "LIS2 Control Center integration is active.\n\n"
        "Metadata, VU and spectrum data are sent to "
        "\\\\.\\pipe\\LIS2ControlCenter.Winamp.",
        "LIS2 Control Center",
        MB_OK | MB_ICONINFORMATION);
}

void PluginQuit()
{
    if (g_timer != 0)
    {
        KillTimer(g_plugin.hwndParent, g_timer);
        g_timer = 0;
    }
    if (g_stopEvent != nullptr)
        SetEvent(g_stopEvent);

    if (g_thread != nullptr)
    {
        WaitForSingleObject(g_thread, 3000);
        CloseHandle(g_thread);
        g_thread = nullptr;
    }

    if (g_snapshotReady != nullptr)
    {
        CloseHandle(g_snapshotReady);
        g_snapshotReady = nullptr;
    }

    if (g_stopEvent != nullptr)
    {
        CloseHandle(g_stopEvent);
        g_stopEvent = nullptr;
    }

    if (g_saSetReq != nullptr)
        g_saSetReq(0);

    g_saGet = nullptr;
    g_saSetReq = nullptr;
    g_vuGet = nullptr;

    ClosePipe();
}
}

extern "C" __declspec(dllexport)
winampGeneralPurposePlugin* winampGetGeneralPurposePlugin()
{
    return &g_plugin;
}
