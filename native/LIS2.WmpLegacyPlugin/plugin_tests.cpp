// Der COM-Test bildet den WMP-Medienvertrag ab und prüft den regulären Metadatenpfad.
#include "plugin.cpp"
#include <iostream>
#include <stdexcept>

namespace
{
void Require(bool valid, const char* message)
{
    if (!valid) throw std::runtime_error(message);
}

class TestMedia final : public IDispatch
{
    ULONG references = 1;
public:
    std::wstring source;
    std::wstring station;
    bool fail = false;
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override
    {
        if (!result) return E_POINTER;
        *result = nullptr;
        if (iid != IID_IUnknown && iid != IID_IDispatch) return E_NOINTERFACE;
        *result = static_cast<IDispatch*>(this);
        AddRef();
        return S_OK;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++references; }
    ULONG STDMETHODCALLTYPE Release() override { return --references; }
    HRESULT STDMETHODCALLTYPE GetTypeInfoCount(UINT* count) override { *count = 0; return S_OK; }
    HRESULT STDMETHODCALLTYPE GetTypeInfo(UINT, LCID, ITypeInfo**) override { return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE GetIDsOfNames(REFIID, LPOLESTR* names, UINT count, LCID, DISPID* ids) override
    {
        if (fail) return E_FAIL;
        if (count != 1) return DISP_E_UNKNOWNNAME;
        if (!_wcsicmp(names[0], L"sourceURL")) { ids[0] = 1; return S_OK; }
        if (!_wcsicmp(names[0], L"getItemInfo")) { ids[0] = 2; return S_OK; }
        return DISP_E_UNKNOWNNAME;
    }
    HRESULT STDMETHODCALLTYPE Invoke(DISPID id, REFIID, LCID, WORD flags, DISPPARAMS* args,
        VARIANT* result, EXCEPINFO*, UINT*) override
    {
        if (fail) return E_FAIL;
        std::wstring value;
        if (id == 1 && flags == DISPATCH_PROPERTYGET) value = source;
        else if (id == 2 && flags == DISPATCH_METHOD && args->cArgs == 1 && args->rgvarg[0].vt == VT_BSTR)
        {
            if (std::wstring(args->rgvarg[0].bstrVal) == L"WM/RadioStationName") value = station;
        }
        else return DISP_E_MEMBERNOTFOUND;
        result->vt = VT_BSTR;
        result->bstrVal = SysAllocString(value.c_str());
        return result->bstrVal ? S_OK : E_OUTOFMEMORY;
    }
};

// Der Hintergrund-Host übergibt nur den Kern. Unerwartete direkte API-Aufrufe scheitern ausdrücklich.
class TestCore final : public IWMPCore
{
public:
    ULONG references = 1;
    unsigned queries = 0;
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override
    {
        if (!result) return E_POINTER;
        *result = nullptr;
        if (iid != IID_IUnknown && iid != IID_IDispatch && iid != __uuidof(IWMPCore)) return E_NOINTERFACE;
        *result = static_cast<IWMPCore*>(this);
        AddRef();
        return S_OK;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++references; }
    ULONG STDMETHODCALLTYPE Release() override { return --references; }
    HRESULT STDMETHODCALLTYPE GetTypeInfoCount(UINT* count) override { *count = 0; return S_OK; }
    HRESULT STDMETHODCALLTYPE GetTypeInfo(UINT, LCID, ITypeInfo**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE GetIDsOfNames(REFIID, LPOLESTR* names, UINT count, LCID, DISPID* ids) override
    {
        ++queries;
        if (count == 1 && !_wcsicmp(names[0], L"playState")) { ids[0] = 1; return S_OK; }
        return DISP_E_UNKNOWNNAME;
    }
    HRESULT STDMETHODCALLTYPE Invoke(DISPID id, REFIID, LCID, WORD flags, DISPPARAMS*,
        VARIANT* result, EXCEPINFO*, UINT*) override
    {
        if (id != 1 || flags != DISPATCH_PROPERTYGET) return DISP_E_MEMBERNOTFOUND;
        result->vt = VT_I4;
        result->lVal = wmppsPlaying;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE close() override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_URL(BSTR*) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE put_URL(BSTR) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_openState(WMPOpenState*) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_playState(WMPPlayState*) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_controls(IWMPControls**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_settings(IWMPSettings**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_currentMedia(IWMPMedia**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE put_currentMedia(IWMPMedia*) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_mediaCollection(IWMPMediaCollection**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_playlistCollection(IWMPPlaylistCollection**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_versionInfo(BSTR*) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE launchURL(BSTR) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_network(IWMPNetwork**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_currentPlaylist(IWMPPlaylist**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE put_currentPlaylist(IWMPPlaylist*) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_cdromCollection(IWMPCdromCollection**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_closedCaption(IWMPClosedCaption**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_isOnline(VARIANT_BOOL*) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_error(IWMPError**) override { return E_UNEXPECTED; }
    HRESULT STDMETHODCALLTYPE get_status(BSTR*) override { return E_UNEXPECTED; }
};

void PumpMessages(DWORD duration)
{
    const ULONGLONG deadline = GetTickCount64() + duration;
    while (GetTickCount64() < deadline)
    {
        MSG message;
        while (PeekMessageW(&message, nullptr, 0, 0, PM_REMOVE))
        {
            TranslateMessage(&message);
            DispatchMessageW(&message);
        }
        MsgWaitForMultipleObjects(0, nullptr, FALSE, 10, QS_ALLINPUT);
    }
}

void CheckBackgroundLifecycle()
{
    struct Pipe
    {
        HANDLE handle = CreateNamedPipeW(PipeName, PIPE_ACCESS_INBOUND,
            PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_NOWAIT, 1, 4096, 4096, 0, nullptr);
        ~Pipe() { if (handle != INVALID_HANDLE_VALUE) CloseHandle(handle); }
    } pipe;
    Require(pipe.handle != INVALID_HANDLE_VALUE, "private telemetry pipe creation");
    const auto readSnapshot = [&pipe]()
    {
        char buffer[4096]{};
        DWORD read = 0;
        Require(ReadFile(pipe.handle, buffer, sizeof(buffer), &read, nullptr) && read > 0,
            "background SetCore must send telemetry without Create");
        const std::string snapshot(buffer, read);
        Require(snapshot.find("\"state\":\"playing\"") != std::string::npos && snapshot.back() == '\n',
            "background pipe contains a complete playback snapshot");
    };
    ConnectNamedPipe(pipe.handle, nullptr);
    TestCore core;
    {
        WmpLegacyPlugin plugin;
        Require(SUCCEEDED(plugin.SetCore(&core)), "background core attachment");
        PumpMessages(700);
        readSnapshot();
        Require(core.references == 2, "attached core retained once");
        HWND first = nullptr;
        HWND second = nullptr;
        Require(SUCCEEDED(plugin.Create(nullptr, &first)) && first != nullptr, "optional host Create");
        Require(SUCCEEDED(plugin.Create(nullptr, &second)) && second == first, "Create does not duplicate timer windows");
        Require(SUCCEEDED(plugin.SetCore(&core)) && core.references == 2, "same-core replacement preserves ownership");
        Require(SUCCEEDED(plugin.SetCore(nullptr)) && core.references == 1, "core detachment releases ownership");
        const unsigned stoppedQueries = core.queries;
        PumpMessages(350);
        Require(core.queries == stoppedQueries, "detached background stops polling");
        Require(!IsWindow(first), "detachment destroys timer window");
        DisconnectNamedPipe(pipe.handle);
        ConnectNamedPipe(pipe.handle, nullptr);
        Require(SUCCEEDED(plugin.SetCore(&core)), "background reconnect");
        PumpMessages(350);
        readSnapshot();
    }
    Require(core.references == 1, "destruction releases core");
    const unsigned disposedQueries = core.queries;
    PumpMessages(350);
    Require(core.queries == disposedQueries, "destruction stops polling");
}
}

int main()
{
    try
    {
        g_module = GetModuleHandleW(nullptr);
        CheckBackgroundLifecycle();
        std::cout << "WMP Legacy background SetCore, real pipe, detach, reconnect and destruction: OK\n";
        TestMedia media;
        media.source = L"https://example.invalid/radio";
        media.station = L"Rádio 東京";
        auto stream = ReadStreamMetadata(&media);
        Require(stream.isStream && stream.name == media.station && stream.url == media.source, "radio metadata");
        std::ostringstream json;
        AppendStreamMetadata(json, stream);
        Require(json.str().find(u8"Rádio 東京") != std::string::npos, "Unicode station JSON");
        Require(json.str().find("\"isStream\":true") != std::string::npos, "stream JSON flag");

        media.station.clear();
        stream = ReadStreamMetadata(&media);
        Require(stream.isStream && stream.name.empty(), "missing station remains empty");
        for (const auto* source : {L"C:\\音楽\\track.mp3", L"file:///C:/track.mp3", L"FILE:///C:/track.mp3", L""})
        {
            media.source = source;
            media.station = L"Old station";
            stream = ReadStreamMetadata(&media);
            Require(!stream.isStream && stream.name.empty() && stream.url.empty(), "file clears stream fields");
        }
        media.source = L"mms://example.invalid/radio";
        Require(ReadStreamMetadata(&media).isStream, "MMS stream");
        media.fail = true;
        stream = ReadStreamMetadata(&media);
        Require(!stream.isStream && stream.name.empty() && stream.url.empty(), "unavailable COM metadata");
        Require(!ReadStreamMetadata(nullptr).isStream, "no current media");
        std::cout << "WMP Legacy COM radio metadata, Unicode, missing fields and file transitions: OK\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << error.what() << '\n';
        return 1;
    }
}
