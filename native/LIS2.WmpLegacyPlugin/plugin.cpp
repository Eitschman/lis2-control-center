#include <windows.h>
#include <oleauto.h>
#include <wmp.h>
#include <wmpplug.h>

#include <atomic>
#include <iomanip>
#include <sstream>
#include <string>
#include <vector>

namespace
{
constexpr wchar_t PipeName[] = L"\\\\.\\pipe\\LIS2ControlCenter.WmpLegacy";
constexpr wchar_t WindowClassName[] = L"LIS2ControlCenter.WmpLegacy.HiddenWindow";
constexpr UINT_PTR PollTimerId = 1;
constexpr UINT PollIntervalMs = 250;

constexpr DWORD PluginCapabilities =
    0x00000001u | // PLUGIN_TYPE_BACKGROUND
    0x02000000u | // PLUGIN_FLAGS_HIDDEN
    0x40000000u;  // PLUGIN_FLAGS_INSTALLAUTORUN

// {A9BC46E7-8A33-46C9-9FE2-8F7A8921B2D0}
const CLSID CLSID_Lis2WmpLegacy =
{ 0xa9bc46e7, 0x8a33, 0x46c9, { 0x9f, 0xe2, 0x8f, 0x7a, 0x89, 0x21, 0xb2, 0xd0 } };

std::atomic<long> g_objectCount{0};
std::atomic<long> g_lockCount{0};
HMODULE g_module = nullptr;

std::string WideToUtf8(const std::wstring& value)
{
    if (value.empty())
        return {};

    const int size = WideCharToMultiByte(
        CP_UTF8, 0, value.c_str(), static_cast<int>(value.size()),
        nullptr, 0, nullptr, nullptr);

    if (size <= 0)
        return {};

    std::string result(static_cast<std::size_t>(size), '\0');
    WideCharToMultiByte(
        CP_UTF8, 0, value.c_str(), static_cast<int>(value.size()),
        result.data(), size, nullptr, nullptr);
    return result;
}

std::string JsonEscape(const std::string& value)
{
    std::ostringstream output;
    for (const unsigned char ch : value)
    {
        switch (ch)
        {
        case '\\': output << "\\\\"; break;
        case '"': output << "\\\""; break;
        case '\b': output << "\\b"; break;
        case '\f': output << "\\f"; break;
        case '\n': output << "\\n"; break;
        case '\r': output << "\\r"; break;
        case '\t': output << "\\t"; break;
        default:
            if (ch < 0x20)
            {
                static constexpr char Hex[] = "0123456789ABCDEF";
                output << "\\u00"
                       << Hex[(ch >> 4) & 0x0f]
                       << Hex[ch & 0x0f];
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

class Variant final
{
public:
    Variant() { VariantInit(&value); }
    ~Variant() { VariantClear(&value); }

    Variant(const Variant&) = delete;
    Variant& operator=(const Variant&) = delete;

    VARIANT value{};
};

HRESULT GetDispId(IDispatch* dispatch, const wchar_t* name, DISPID* dispId)
{
    if (dispatch == nullptr || name == nullptr || dispId == nullptr)
        return E_POINTER;

    LPOLESTR names[] = { const_cast<LPOLESTR>(name) };
    return dispatch->GetIDsOfNames(
        IID_NULL, names, 1, LOCALE_USER_DEFAULT, dispId);
}

HRESULT GetProperty(
    IDispatch* dispatch,
    const wchar_t* name,
    VARIANT* result)
{
    DISPID dispId{};
    HRESULT hr = GetDispId(dispatch, name, &dispId);
    if (FAILED(hr))
        return hr;

    DISPPARAMS params{};
    return dispatch->Invoke(
        dispId,
        IID_NULL,
        LOCALE_USER_DEFAULT,
        DISPATCH_PROPERTYGET,
        &params,
        result,
        nullptr,
        nullptr);
}

HRESULT CallStringMethod(
    IDispatch* dispatch,
    const wchar_t* name,
    const wchar_t* argument,
    VARIANT* result)
{
    DISPID dispId{};
    HRESULT hr = GetDispId(dispatch, name, &dispId);
    if (FAILED(hr))
        return hr;

    Variant arg;
    arg.value.vt = VT_BSTR;
    arg.value.bstrVal = SysAllocString(argument);
    if (arg.value.bstrVal == nullptr)
        return E_OUTOFMEMORY;

    DISPPARAMS params{};
    params.rgvarg = &arg.value;
    params.cArgs = 1;

    return dispatch->Invoke(
        dispId,
        IID_NULL,
        LOCALE_USER_DEFAULT,
        DISPATCH_METHOD,
        &params,
        result,
        nullptr,
        nullptr);
}

IDispatch* VariantDispatch(const VARIANT& value)
{
    if (value.vt == VT_DISPATCH && value.pdispVal != nullptr)
    {
        value.pdispVal->AddRef();
        return value.pdispVal;
    }

    if (value.vt == VT_UNKNOWN && value.punkVal != nullptr)
    {
        IDispatch* dispatch = nullptr;
        if (SUCCEEDED(value.punkVal->QueryInterface(
                IID_IDispatch,
                reinterpret_cast<void**>(&dispatch))))
        {
            return dispatch;
        }
    }

    return nullptr;
}

std::wstring VariantString(const VARIANT& value)
{
    if (value.vt == VT_BSTR && value.bstrVal != nullptr)
        return std::wstring(value.bstrVal, SysStringLen(value.bstrVal));

    Variant converted;
    if (SUCCEEDED(VariantChangeType(
            &converted.value,
            const_cast<VARIANT*>(&value),
            0,
            VT_BSTR)) &&
        converted.value.bstrVal != nullptr)
    {
        return std::wstring(
            converted.value.bstrVal,
            SysStringLen(converted.value.bstrVal));
    }

    return {};
}

double VariantDouble(const VARIANT& value, double fallback = -1)
{
    Variant converted;
    if (FAILED(VariantChangeType(
            &converted.value,
            const_cast<VARIANT*>(&value),
            0,
            VT_R8)))
    {
        return fallback;
    }
    return converted.value.dblVal;
}

long VariantLong(const VARIANT& value, long fallback = -1)
{
    Variant converted;
    if (FAILED(VariantChangeType(
            &converted.value,
            const_cast<VARIANT*>(&value),
            0,
            VT_I4)))
    {
        return fallback;
    }
    return converted.value.lVal;
}

std::wstring ReadStringProperty(IDispatch* dispatch, const wchar_t* name)
{
    Variant value;
    return SUCCEEDED(GetProperty(dispatch, name, &value.value))
        ? VariantString(value.value)
        : std::wstring{};
}

double ReadDoubleProperty(IDispatch* dispatch, const wchar_t* name)
{
    Variant value;
    return SUCCEEDED(GetProperty(dispatch, name, &value.value))
        ? VariantDouble(value.value)
        : -1;
}

long ReadLongProperty(IDispatch* dispatch, const wchar_t* name)
{
    Variant value;
    return SUCCEEDED(GetProperty(dispatch, name, &value.value))
        ? VariantLong(value.value)
        : -1;
}

std::wstring ReadItemInfo(IDispatch* media, const wchar_t* key)
{
    Variant value;
    return SUCCEEDED(CallStringMethod(
        media,
        L"getItemInfo",
        key,
        &value.value))
        ? VariantString(value.value)
        : std::wstring{};
}

const char* PlaybackStateName(long state)
{
    switch (state)
    {
    case 3: return "playing"; // wmppsPlaying
    case 2: return "paused";  // wmppsPaused
    case 1: return "stopped"; // wmppsStopped
    default: return "unknown";
    }
}

void AppendJsonString(
    std::ostringstream& json,
    const char* name,
    const std::wstring& value)
{
    json << ",\"" << name << "\":\""
         << JsonEscape(WideToUtf8(value))
         << "\"";
}

bool WritePipeLine(const std::string& payload)
{
    HANDLE pipe = CreateFileW(
        PipeName,
        GENERIC_WRITE,
        0,
        nullptr,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        nullptr);

    if (pipe == INVALID_HANDLE_VALUE)
        return false;

    std::string line = payload;
    line.push_back('\n');

    DWORD written = 0;
    const BOOL ok = WriteFile(
        pipe,
        line.data(),
        static_cast<DWORD>(line.size()),
        &written,
        nullptr);

    CloseHandle(pipe);
    return ok && written == line.size();
}

class WmpLegacyPlugin final : public IWMPPluginUI
{
public:
    WmpLegacyPlugin()
    {
        ++g_objectCount;
    }

    ~WmpLegacyPlugin() override
    {
        StopTimer();
        if (_core != nullptr)
            _core->Release();
        --g_objectCount;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID riid,
        void** object) override
    {
        if (object == nullptr)
            return E_POINTER;

        *object = nullptr;

        if (riid == IID_IUnknown || riid == __uuidof(IWMPPluginUI))
            *object = static_cast<IWMPPluginUI*>(this);

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

    HRESULT STDMETHODCALLTYPE SetCore(IWMPCore* core) override
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
        HWND,
        HWND* window) override
    {
        if (window == nullptr)
            return E_POINTER;

        EnsureWindowClass();

        _window = CreateWindowExW(
            0,
            WindowClassName,
            L"",
            0,
            0, 0, 0, 0,
            HWND_MESSAGE,
            nullptr,
            g_module,
            this);

        if (_window == nullptr)
            return HRESULT_FROM_WIN32(GetLastError());

        SetTimer(_window, PollTimerId, PollIntervalMs, nullptr);
        *window = _window;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE Destroy() override
    {
        StopTimer();
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE DisplayPropertyPage(HWND parent) override
    {
        MessageBoxW(
            parent,
            L"LIS2 Control Center Windows Media Player Legacy integration is active.\n\n"
            L"Playback metadata is sent to \\\\.\\pipe\\LIS2ControlCenter.WmpLegacy.",
            L"LIS2 Control Center",
            MB_OK | MB_ICONINFORMATION);
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetProperty(
        LPCWSTR,
        VARIANT*) override
    {
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE SetProperty(
        LPCWSTR,
        const VARIANT*) override
    {
        return E_NOTIMPL;
    }

    HRESULT STDMETHODCALLTYPE TranslateAccelerator(LPMSG) override
    {
        return S_FALSE;
    }

    void SendSnapshot()
    {
        if (_core == nullptr)
            return;

        IDispatch* core = nullptr;
        if (FAILED(_core->QueryInterface(
                IID_IDispatch,
                reinterpret_cast<void**>(&core))) ||
            core == nullptr)
        {
            return;
        }

        const long playState = ReadLongProperty(core, L"playState");

        Variant mediaValue;
        IDispatch* media = nullptr;
        if (SUCCEEDED(GetProperty(core, L"currentMedia", &mediaValue.value)))
            media = VariantDispatch(mediaValue.value);

        Variant controlsValue;
        IDispatch* controls = nullptr;
        if (SUCCEEDED(GetProperty(core, L"controls", &controlsValue.value)))
            controls = VariantDispatch(controlsValue.value);

        Variant playlistValue;
        IDispatch* playlist = nullptr;
        if (SUCCEEDED(GetProperty(core, L"currentPlaylist", &playlistValue.value)))
            playlist = VariantDispatch(playlistValue.value);

        std::wstring title;
        std::wstring artist;
        std::wstring album;
        long trackNumber = -1;
        double duration = -1;

        if (media != nullptr)
        {
            title = ReadItemInfo(media, L"Title");
            if (title.empty())
                title = ReadStringProperty(media, L"name");

            artist = ReadItemInfo(media, L"Author");
            album = ReadItemInfo(media, L"WM/AlbumTitle");

            const auto track = ReadItemInfo(media, L"WM/TrackNumber");
            if (!track.empty())
            {
                wchar_t* end = nullptr;
                const long parsed = wcstol(track.c_str(), &end, 10);
                if (end != track.c_str())
                    trackNumber = parsed;
            }

            duration = ReadDoubleProperty(media, L"duration");
        }

        const double elapsed = controls != nullptr
            ? ReadDoubleProperty(controls, L"currentPosition")
            : -1;

        const long playlistCount = playlist != nullptr
            ? ReadLongProperty(playlist, L"count")
            : -1;

        std::ostringstream json;
        json << "{\"type\":\"snapshot\",\"state\":\""
             << PlaybackStateName(playState)
             << "\"";

        AppendJsonString(json, "artist", artist);
        AppendJsonString(json, "title", title);
        AppendJsonString(json, "album", album);

        if (trackNumber >= 0)
            json << ",\"trackNumber\":" << trackNumber;

        if (playlistCount >= 0)
            json << ",\"playlistCount\":" << playlistCount;

        if (elapsed >= 0)
            json << ",\"elapsedSeconds\":"
                 << std::fixed << std::setprecision(3) << elapsed;

        if (duration >= 0)
            json << ",\"durationSeconds\":"
                 << std::fixed << std::setprecision(3) << duration;

        json << "}";

        WritePipeLine(json.str());

        if (playlist != nullptr) playlist->Release();
        if (controls != nullptr) controls->Release();
        if (media != nullptr) media->Release();
        core->Release();
    }

private:
    LONG _references = 1;
    IWMPCore* _core = nullptr;
    HWND _window = nullptr;

    static LRESULT CALLBACK WindowProc(
        HWND hwnd,
        UINT message,
        WPARAM wParam,
        LPARAM lParam)
    {
        auto* plugin = reinterpret_cast<WmpLegacyPlugin*>(
            GetWindowLongPtrW(hwnd, GWLP_USERDATA));

        if (message == WM_NCCREATE)
        {
            const auto* create =
                reinterpret_cast<const CREATESTRUCTW*>(lParam);
            plugin = static_cast<WmpLegacyPlugin*>(create->lpCreateParams);
            SetWindowLongPtrW(
                hwnd,
                GWLP_USERDATA,
                reinterpret_cast<LONG_PTR>(plugin));
        }

        if (message == WM_TIMER &&
            wParam == PollTimerId &&
            plugin != nullptr)
        {
            plugin->SendSnapshot();
            return 0;
        }

        return DefWindowProcW(hwnd, message, wParam, lParam);
    }

    static void EnsureWindowClass()
    {
        WNDCLASSW windowClass{};
        windowClass.lpfnWndProc = WindowProc;
        windowClass.hInstance = g_module;
        windowClass.lpszClassName = WindowClassName;

        if (!RegisterClassW(&windowClass) &&
            GetLastError() != ERROR_CLASS_ALREADY_EXISTS)
        {
            return;
        }
    }

    void StopTimer()
    {
        if (_window == nullptr)
            return;

        KillTimer(_window, PollTimerId);
        DestroyWindow(_window);
        _window = nullptr;
    }
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

        auto* plugin = new (std::nothrow) WmpLegacyPlugin();
        if (plugin == nullptr)
            return E_OUTOFMEMORY;

        const HRESULT hr = plugin->QueryInterface(riid, object);
        plugin->Release();
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

HRESULT SetRegistryDword(
    HKEY root,
    const std::wstring& path,
    const wchar_t* name,
    DWORD value)
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
        REG_DWORD,
        reinterpret_cast<const BYTE*>(&value),
        sizeof(value));

    RegCloseKey(key);
    return result == ERROR_SUCCESS
        ? S_OK
        : HRESULT_FROM_WIN32(result);
}

void NotifyWmpPluginChange()
{
    PostMessageW(
        HWND_BROADCAST,
        RegisterWindowMessageW(L"WMPlayer_PluginAddRemove"),
        0,
        0);
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

extern "C" __declspec(dllexport)
HRESULT __stdcall DllCanUnloadNow()
{
    return g_objectCount.load() == 0 && g_lockCount.load() == 0
        ? S_OK
        : S_FALSE;
}

extern "C" __declspec(dllexport)
HRESULT __stdcall DllGetClassObject(
    REFCLSID clsid,
    REFIID riid,
    void** object)
{
    if (clsid != CLSID_Lis2WmpLegacy)
        return CLASS_E_CLASSNOTAVAILABLE;

    auto* factory = new (std::nothrow) ClassFactory();
    if (factory == nullptr)
        return E_OUTOFMEMORY;

    const HRESULT hr = factory->QueryInterface(riid, object);
    factory->Release();
    return hr;
}

extern "C" __declspec(dllexport)
HRESULT __stdcall DllRegisterServer()
{
    wchar_t modulePath[MAX_PATH]{};
    if (GetModuleFileNameW(
            g_module,
            modulePath,
            static_cast<DWORD>(std::size(modulePath))) == 0)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    const std::wstring clsid = GuidString(CLSID_Lis2WmpLegacy);
    const std::wstring classPath = L"CLSID\\" + clsid;
    const std::wstring inprocPath = classPath + L"\\InprocServer32";

    HRESULT hr = SetRegistryString(
        HKEY_CLASSES_ROOT,
        classPath,
        nullptr,
        L"LIS2 Control Center WMP Legacy");
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

    const std::wstring pluginPath =
        L"SOFTWARE\\Microsoft\\MediaPlayer\\UIPlugins\\" + clsid;

    hr = SetRegistryString(
        HKEY_LOCAL_MACHINE,
        pluginPath,
        L"FriendlyName",
        L"LIS2 Control Center");
    if (FAILED(hr)) return hr;

    hr = SetRegistryString(
        HKEY_LOCAL_MACHINE,
        pluginPath,
        L"Description",
        L"Publishes Windows Media Player Legacy playback telemetry to LIS2 Control Center.");
    if (FAILED(hr)) return hr;

    hr = SetRegistryDword(
        HKEY_LOCAL_MACHINE,
        pluginPath,
        L"Capabilities",
        PluginCapabilities);
    if (FAILED(hr)) return hr;

    NotifyWmpPluginChange();
    return S_OK;
}

extern "C" __declspec(dllexport)
HRESULT __stdcall DllUnregisterServer()
{
    const std::wstring clsid = GuidString(CLSID_Lis2WmpLegacy);
    const std::wstring classPath = L"CLSID\\" + clsid;
    const std::wstring pluginPath =
        L"SOFTWARE\\Microsoft\\MediaPlayer\\UIPlugins\\" + clsid;

    RegDeleteTreeW(HKEY_CLASSES_ROOT, classPath.c_str());
    RegDeleteTreeW(HKEY_LOCAL_MACHINE, pluginPath.c_str());

    NotifyWmpPluginChange();
    return S_OK;
}
