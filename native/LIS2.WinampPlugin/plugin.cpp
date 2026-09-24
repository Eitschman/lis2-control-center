#include "winamp_sdk_min.h"

#include <algorithm>
#include <array>
#include <cstdint>
#include <sstream>
#include <string>
#include <vector>

namespace
{
constexpr wchar_t PipeName[] = L"\\\\.\\pipe\\LIS2ControlCenter.Winamp";
constexpr DWORD PollIntervalMs = 200;
constexpr int SpectrumSourceBins = 75;
constexpr int SpectrumOutputBins = 20;

using SaGetFunc = const unsigned char* (__cdecl*)();
using SaSetReqFunc = void (__cdecl*)(int);
using VuGetFunc = int (__cdecl*)(int);

HANDLE g_stopEvent = nullptr;
HANDLE g_thread = nullptr;
HANDLE g_pipe = INVALID_HANDLE_VALUE;

SaGetFunc g_saGet = nullptr;
SaSetReqFunc g_saSetReq = nullptr;
VuGetFunc g_vuGet = nullptr;

char g_description[] = "LIS2 Control Center (gen_lis2.dll)";

bool IsValidFunctionPointer(LRESULT raw) =>
    raw != 0 && raw != 1 && raw != -1;

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

std::string AnsiToUtf8(const char* value)
{
    if (value == nullptr || *value == '\0')
        return {};

    const int wideLength = MultiByteToWideChar(
        CP_ACP,
        0,
        value,
        -1,
        nullptr,
        0);

    if (wideLength <= 1)
        return {};

    std::vector<wchar_t> wide(static_cast<std::size_t>(wideLength));

    if (MultiByteToWideChar(
            CP_ACP,
            0,
            value,
            -1,
            wide.data(),
            wideLength) == 0)
    {
        return {};
    }

    const int utf8Length = WideCharToMultiByte(
        CP_UTF8,
        0,
        wide.data(),
        -1,
        nullptr,
        0,
        nullptr,
        nullptr);

    if (utf8Length <= 1)
        return {};

    std::vector<char> utf8(static_cast<std::size_t>(utf8Length));

    if (WideCharToMultiByte(
            CP_UTF8,
            0,
            wide.data(),
            -1,
            utf8.data(),
            utf8Length,
            nullptr,
            nullptr) == 0)
    {
        return {};
    }

    return std::string(utf8.data());
}

std::string GetExtendedInfo(
    HWND hwndWinamp,
    const char* filename,
    const char* metadata)
{
    if (filename == nullptr || metadata == nullptr)
        return {};

    char buffer[2048]{};

    extendedFileInfoStruct info
    {
        filename,
        metadata,
        buffer,
        sizeof(buffer)
    };

    LRESULT result = SendMessage(
        hwndWinamp,
        WM_WA_IPC,
        reinterpret_cast<WPARAM>(&info),
        IPC_GET_EXTENDED_FILE_INFO_HOOKABLE);

    if (result == 0)
    {
        result = SendMessage(
            hwndWinamp,
            WM_WA_IPC,
            reinterpret_cast<WPARAM>(&info),
            IPC_GET_EXTENDED_FILE_INFO);
    }

    return result != 0
        ? AnsiToUtf8(buffer)
        : std::string{};
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

    const unsigned char* data = g_saGet();
    if (data == nullptr)
        return result;

    for (int output = 0; output < SpectrumOutputBins; ++output)
    {
        const int begin = output * SpectrumSourceBins / SpectrumOutputBins;
        const int end = (output + 1) * SpectrumSourceBins / SpectrumOutputBins;

        int peak = 0;
        for (int source = begin; source < end; ++source)
            peak = std::max(peak, static_cast<int>(data[source]));

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

    const char* playlistFile = playlistPosition >= 0
        ? reinterpret_cast<const char*>(
            SendMessage(
                hwndWinamp,
                WM_WA_IPC,
                static_cast<WPARAM>(playlistPosition),
                IPC_GETPLAYLISTFILE))
        : nullptr;

    const char* playlistTitle = playlistPosition >= 0
        ? reinterpret_cast<const char*>(
            SendMessage(
                hwndWinamp,
                WM_WA_IPC,
                static_cast<WPARAM>(playlistPosition),
                IPC_GETPLAYLISTTITLE))
        : nullptr;

    std::string artist = GetExtendedInfo(
        hwndWinamp,
        playlistFile,
        "artist");

    std::string title = GetExtendedInfo(
        hwndWinamp,
        playlistFile,
        "title");

    std::string album = GetExtendedInfo(
        hwndWinamp,
        playlistFile,
        "album");

    if (title.empty())
        title = AnsiToUtf8(playlistTitle);

    const int vuLeft = state == 1 ? ReadVu(0) : 0;
    const int vuRight = state == 1 ? ReadVu(1) : 0;
    const auto spectrum = state == 1
        ? ReadSpectrum()
        : std::array<int, SpectrumOutputBins>{};

    std::ostringstream json;
    json << "{\"type\":\"snapshot\",\"state\":\""
         << PlaybackStateName(state)
         << "\"";

    AppendJsonString(json, "artist", artist);
    AppendJsonString(json, "title", title);
    AppendJsonString(json, "album", album);

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

void SendSnapshot(HWND hwndWinamp)
{
    if (!EnsurePipe())
        return;

    std::string payload = BuildSnapshotJson(hwndWinamp);
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

DWORD WINAPI WorkerThread(void* parameter)
{
    const auto hwndWinamp = static_cast<HWND>(parameter);

    while (true)
    {
        SendSnapshot(hwndWinamp);

        if (WaitForSingleObject(
                g_stopEvent,
                PollIntervalMs) != WAIT_TIMEOUT)
        {
            break;
        }
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
        0,
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

    g_thread = CreateThread(
        nullptr,
        0,
        WorkerThread,
        g_plugin.hwndParent,
        0,
        nullptr);

    if (g_thread == nullptr)
    {
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
    if (g_stopEvent != nullptr)
        SetEvent(g_stopEvent);

    if (g_thread != nullptr)
    {
        WaitForSingleObject(g_thread, 3000);
        CloseHandle(g_thread);
        g_thread = nullptr;
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
