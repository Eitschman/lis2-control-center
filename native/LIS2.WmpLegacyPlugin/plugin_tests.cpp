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
}

int main()
{
    try
    {
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
