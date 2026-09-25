#include <windows.h>
#include <oleauto.h>
#include <wmp.h>
#include <effects.h>

#include <algorithm>
#include <array>
#include <atomic>
#include <cstdint>
#include <sstream>
#include <string>

namespace
{
constexpr wchar_t PipeName[] =
    L"\\\\.\\pipe\\LIS2ControlCenter.WmpLegacy.Visualization";
constexpr int OutputBins = 20;
constexpr ULONGLONG PublishIntervalMs = 100;

// {7D7C910D-5D80-4A75-A995-4B3D6A4C2D62}
const CLSID CLSID_Lis2WmpLegacyVisualization =
{ 0x7d7c910d, 0x5d80, 0x4a75, { 0xa9, 0x95, 0x4b, 0x3d, 0x6a, 0x4c, 0x2d, 0x62 } };

std::atomic<long> g_objectCount{0};
std::atomic<long> g_lockCount{0};
HMODULE g_module = nullptr;

HANDLE g_pipe = INVALID_HANDLE_VALUE;

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

bool WritePipeLine(const std::string& payload)
{
    if (!EnsurePipe())
        return false;

    std::string line = payload;
    line.push_back('\n');

    DWORD written = 0;
    const BOOL ok = WriteFile(
        g_pipe,
        line.data(),
        static_cast<DWORD>(line.size()),
        &written,
        nullptr);

    if (!ok || written != line.size())
    {
        ClosePipe();
        return false;
    }

    return true;
}

int WaveformPeak(const unsigned char* waveform)
{
    int peak = 0;
    for (int index = 0; index < SA_BUFFER_SIZE; ++index)
    {
        const int centered = static_cast<int>(waveform[index]) - 128;
        peak = std::max(peak, std::abs(centered));
    }

    return std::clamp(peak * 2, 0, 255);
}

std::array<int, OutputBins> DownsampleSpectrum(const TimedLevel& levels)
{
    std::array<int, OutputBins> result{};

    for (int output = 0; output < OutputBins; ++output)
    {
        const int begin = output * SA_BUFFER_SIZE / OutputBins;
        const int end = (output + 1) * SA_BUFFER_SIZE / OutputBins;

        int peak = 0;
        for (int source = begin; source < end; ++source)
        {
            peak = std::max(
                peak,
                static_cast<int>(levels.frequency[0][source]));
            peak = std::max(
                peak,
                static_cast<int>(levels.frequency[1][source]));
        }

        result[output] = std::clamp(peak, 0, 255);
    }

    return result;
}

void PublishLevels(const TimedLevel* levels)
{
    if (levels == nullptr)
        return;

    static ULONGLONG lastPublish = 0;
    const ULONGLONG now = GetTickCount64();

    if (lastPublish != 0 && now - lastPublish < PublishIntervalMs)
        return;

    lastPublish = now;

    const bool playing = levels->state == play_state;
    const int vuLeft = playing ? WaveformPeak(levels->waveform[0]) : 0;
    const int vuRight = playing ? WaveformPeak(levels->waveform[1]) : 0;
    const auto spectrum = playing
        ? DownsampleSpectrum(*levels)
        : std::array<int, OutputBins>{};

    std::ostringstream json;
    json << "{\"type\":\"visualization\","
         << "\"vuLeft\":" << vuLeft << ','
         << "\"vuRight\":" << vuRight << ','
         << "\"spectrum\":[";

    for (int index = 0; index < OutputBins; ++index)
    {
        if (index > 0)
            json << ',';
        json << spectrum[index];
    }

    json << "]}";
    WritePipeLine(json.str());
}

void DrawTelemetrySurface(HWND hwnd, HDC providedDc, RECT* providedRect)
{
    HDC dc = providedDc;
    bool releaseDc = false;
    RECT rect{};

    if (providedRect != nullptr)
        rect = *providedRect;
    else if (hwnd != nullptr)
        GetClientRect(hwnd, &rect);
    else
        return;

    if (dc == nullptr && hwnd != nullptr)
    {
        dc = GetDC(hwnd);
        releaseDc = dc != nullptr;
    }

    if (dc == nullptr)
        return;

    HBRUSH background = CreateSolidBrush(RGB(15, 18, 20));
    FillRect(dc, &rect, background);
    DeleteObject(background);

    SetBkMode(dc, TRANSPARENT);
    SetTextColor(dc, RGB(120, 230, 160));

    const wchar_t text[] = L"LIS2 Control Center telemetry";
    DrawTextW(
        dc,
        text,
        -1,
        &rect,
        DT_CENTER | DT_VCENTER | DT_SINGLELINE);

    if (releaseDc)
        ReleaseDC(hwnd, dc);
}

class WmpLegacyVisualization final : public IWMPEffects2
{
public:
    WmpLegacyVisualization()
    {
        ++g_objectCount;
    }

    ~WmpLegacyVisualization()
    {
        if (_core != nullptr)
            _core->Release();
        ClosePipe();
        --g_objectCount;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID riid,
        void** object) override
    {
        if (object == nullptr)
            return E_POINTER;

        *object = nullptr;

        if (riid == IID_IUnknown ||
            riid == __uuidof(IWMPEffects) ||
            riid == __uuidof(IWMPEffects2))
        {
            *object = static_cast<IWMPEffects2*>(this);
        }

        if (*object == nullptr)
            return E_NOINTERFACE;

        AddRef();
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE AddRef() override
    {
        return static_cast<ULONG>(InterlockedIncrement(&_references));
    }

    ULONG STDMETHODCALLTYPE Release() override
    {
        const LONG count = InterlockedDecrement(&_references);
        if (count == 0)
            delete this;
        return static_cast<ULONG>(count);
    }

    HRESULT STDMETHODCALLTYPE Render(
        TimedLevel* levels,
        HDC dc,
        RECT* rect) override
    {
        PublishLevels(levels);
        DrawTelemetrySurface(nullptr, dc, rect);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE MediaInfo(
        LONG,
        LONG,
        BSTR) override
    {
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetCapabilities(
        DWORD* capabilities) override
    {
        if (capabilities == nullptr)
            return E_POINTER;

        *capabilities = 0;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetTitle(
        BSTR* title) override
    {
        if (title == nullptr)
            return E_POINTER;

        *title = SysAllocString(L"LIS2 Control Center");
        return *title != nullptr ? S_OK : E_OUTOFMEMORY;
    }

    HRESULT STDMETHODCALLTYPE GetPresetTitle(
        LONG preset,
        BSTR* title) override
    {
        if (title == nullptr)
            return E_POINTER;
        if (preset != 0)
            return E_INVALIDARG;

        *title = SysAllocString(L"Telemetry");
        return *title != nullptr ? S_OK : E_OUTOFMEMORY;
    }

    HRESULT STDMETHODCALLTYPE GetPresetCount(
        LONG* count) override
    {
        if (count == nullptr)
            return E_POINTER;

        *count = 1;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE SetCurrentPreset(
        LONG preset) override
    {
        return preset == 0 ? S_OK : E_INVALIDARG;
    }

    HRESULT STDMETHODCALLTYPE GetCurrentPreset(
        LONG* preset) override
    {
        if (preset == nullptr)
            return E_POINTER;

        *preset = 0;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE DisplayPropertyPage(
        HWND owner) override
    {
        MessageBoxW(
            owner,
            L"This visualization forwards WMP Legacy VU and spectrum telemetry "
            L"to LIS2 Control Center. It does not access LIS2 hardware.",
            L"LIS2 Control Center",
            MB_OK | MB_ICONINFORMATION);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GoFullscreen(BOOL) override
    {
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE RenderFullScreen(
        TimedLevel* levels) override
    {
        PublishLevels(levels);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE SetCore(
        IWMPCore* core) override
    {
        if (_core != nullptr)
        {
            _core->Release();
            _core = nullptr;
        }

        if (core != nullptr)
        {
            core->AddRef();
            _core = core;
        }

        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE Create(
        HWND parent) override
    {
        _parent = parent;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE Destroy() override
    {
        _parent = nullptr;
        ClosePipe();
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE NotifyNewMedia(
        IWMPMedia*) override
    {
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE OnWindowMessage(
        UINT,
        WPARAM,
        LPARAM,
        LRESULT*) override
    {
        return S_FALSE;
    }

    HRESULT STDMETHODCALLTYPE RenderWindowed(
        TimedLevel* levels,
        BOOL requiredRender) override
    {
        PublishLevels(levels);

        if (requiredRender || levels == nullptr || levels->state != play_state)
            DrawTelemetrySurface(_parent, nullptr, nullptr);

        return S_OK;
    }

private:
    LONG _references = 1;
    IWMPCore* _core = nullptr;
    HWND _parent = nullptr;
};

class ClassFactory final : public IClassFactory
{
public:
    HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID riid,
        void** object) override
    {
        if (object == nullptr)
            return E_POINTER;

        *object = nullptr;
        if (riid == IID_IUnknown || riid == IID_IClassFactory)
            *object = static_cast<IClassFactory*>(this);

        if (*object == nullptr)
            return E_NOINTERFACE;

        AddRef();
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE AddRef() override
    {
        return static_cast<ULONG>(InterlockedIncrement(&_references));
    }

    ULONG STDMETHODCALLTYPE Release() override
    {
        const LONG count = InterlockedDecrement(&_references);
        if (count == 0)
            delete this;
        return static_cast<ULONG>(count);
    }

    HRESULT STDMETHODCALLTYPE CreateInstance(
        IUnknown* outer,
        REFIID riid,
        void** object) override
    {
        if (outer != nullptr)
            return CLASS_E_NOAGGREGATION;

        auto* visualization = new (std::nothrow) WmpLegacyVisualization();
        if (visualization == nullptr)
            return E_OUTOFMEMORY;

        const HRESULT hr = visualization->QueryInterface(riid, object);
        visualization->Release();
        return hr;
    }

    HRESULT STDMETHODCALLTYPE LockServer(BOOL lock) override
    {
        if (lock)
            ++g_lockCount;
        else
            --g_lockCount;
        return S_OK;
    }

private:
    LONG _references = 1;
};

std::wstring GuidString(REFGUID guid)
{
    wchar_t buffer[64]{};
    StringFromGUID2(guid, buffer, static_cast<int>(std::size(buffer)));
    return buffer;
}

HRESULT SetRegistryString(
    HKEY root,
    const std::wstring& path,
    const wchar_t* name,
    const std::wstring& value)
{
    HKEY key = nullptr;
    const LONG opened = RegCreateKeyExW(
        root,
        path.c_str(),
        0,
        nullptr,
        0,
        KEY_WRITE,
        nullptr,
        &key,
        nullptr);

    if (opened != ERROR_SUCCESS)
        return HRESULT_FROM_WIN32(opened);

    const LONG result = RegSetValueExW(
        key,
        name,
        0,
        REG_SZ,
        reinterpret_cast<const BYTE*>(value.c_str()),
        static_cast<DWORD>((value.size() + 1) * sizeof(wchar_t)));

    RegCloseKey(key);
    return result == ERROR_SUCCESS
        ? S_OK
        : HRESULT_FROM_WIN32(result);
}
}

extern "C" BOOL WINAPI DllMain(
    HINSTANCE instance,
    DWORD reason,
    LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        g_module = instance;
        DisableThreadLibraryCalls(instance);
    }

    return TRUE;
}

STDAPI DllCanUnloadNow()
{
    return g_objectCount.load() == 0 && g_lockCount.load() == 0
        ? S_OK
        : S_FALSE;
}

STDAPI DllGetClassObject(
    REFCLSID clsid,
    REFIID riid,
    LPVOID* object)
{
    if (clsid != CLSID_Lis2WmpLegacyVisualization)
        return CLASS_E_CLASSNOTAVAILABLE;

    auto* factory = new (std::nothrow) ClassFactory();
    if (factory == nullptr)
        return E_OUTOFMEMORY;

    const HRESULT hr = factory->QueryInterface(riid, object);
    factory->Release();
    return hr;
}

STDAPI DllRegisterServer()
{
    wchar_t modulePath[MAX_PATH]{};
    if (GetModuleFileNameW(
            g_module,
            modulePath,
            static_cast<DWORD>(std::size(modulePath))) == 0)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    const std::wstring clsid =
        GuidString(CLSID_Lis2WmpLegacyVisualization);
    const std::wstring classPath = L"CLSID\\" + clsid;
    const std::wstring inprocPath = classPath + L"\\InprocServer32";

    HRESULT hr = SetRegistryString(
        HKEY_CLASSES_ROOT,
        classPath,
        nullptr,
        L"LIS2 Control Center WMP Legacy Visualization");
    if (FAILED(hr)) return hr;

    hr = SetRegistryString(
        HKEY_CLASSES_ROOT,
        inprocPath,
        nullptr,
        modulePath);
    if (FAILED(hr)) return hr;

    hr = SetRegistryString(
        HKEY_CLASSES_ROOT,
        inprocPath,
        L"ThreadingModel",
        L"Apartment");
    if (FAILED(hr)) return hr;

    const std::wstring effectPath =
        L"SOFTWARE\\Microsoft\\MediaPlayer\\Objects\\Effects\\"
        L"LIS2ControlCenter";

    hr = SetRegistryString(
        HKEY_LOCAL_MACHINE,
        effectPath,
        L"classid",
        clsid);
    if (FAILED(hr)) return hr;

    const std::wstring propertiesPath = effectPath + L"\\Properties";

    hr = SetRegistryString(
        HKEY_LOCAL_MACHINE,
        propertiesPath,
        L"classid",
        clsid);
    if (FAILED(hr)) return hr;

    hr = SetRegistryString(
        HKEY_LOCAL_MACHINE,
        propertiesPath,
        L"name",
        L"LIS2 Control Center");
    if (FAILED(hr)) return hr;

    hr = SetRegistryString(
        HKEY_LOCAL_MACHINE,
        propertiesPath,
        L"description",
        L"Publishes WMP Legacy VU and spectrum telemetry to LIS2 Control Center.");
    return hr;
}

STDAPI DllUnregisterServer()
{
    const std::wstring clsid =
        GuidString(CLSID_Lis2WmpLegacyVisualization);
    const std::wstring classPath = L"CLSID\\" + clsid;
    const std::wstring effectPath =
        L"SOFTWARE\\Microsoft\\MediaPlayer\\Objects\\Effects\\"
        L"LIS2ControlCenter";

    RegDeleteTreeW(HKEY_CLASSES_ROOT, classPath.c_str());
    RegDeleteTreeW(HKEY_LOCAL_MACHINE, effectPath.c_str());

    ClosePipe();
    return S_OK;
}
