#pragma once

#include <windows.h>
#include <cstddef>

struct winampGeneralPurposePlugin
{
    int version;
    char* description;
    int (*init)();
    void (*config)();
    void (*quit)();
    HWND hwndParent;
    HINSTANCE hDllInstance;
};

constexpr int GPPHDR_VER = 0x10;

constexpr UINT WM_WA_IPC = WM_USER;

constexpr LPARAM IPC_ISPLAYING = 104;
constexpr LPARAM IPC_GETOUTPUTTIME = 105;
constexpr LPARAM IPC_GETLISTLENGTH = 124;
constexpr LPARAM IPC_GETLISTPOS = 125;
constexpr LPARAM IPC_GETINFO = 126;

constexpr LPARAM IPC_GETPLAYLISTFILE = 211;
constexpr LPARAM IPC_GETPLAYLISTTITLE = 212;
constexpr LPARAM IPC_GETPLAYLISTTITLEW = 213;
constexpr LPARAM IPC_GETPLAYLISTFILEW = 214;
constexpr LPARAM IPC_GET_PLAYING_FILENAME = 3031;
constexpr LPARAM IPC_GET_PLAYING_TITLE = 3034;

constexpr LPARAM IPC_GET_EXTENDED_FILE_INFO = 290;
constexpr LPARAM IPC_GET_EXTENDED_FILE_INFO_HOOKABLE = 296;
constexpr LPARAM IPC_GET_EXTENDED_FILE_INFOW = 3026;
constexpr LPARAM IPC_GET_EXTENDED_FILE_INFOW_HOOKABLE = 3027;

constexpr LPARAM IPC_GETSADATAFUNC = 800;
constexpr LPARAM IPC_GETVUDATAFUNC = 801;

struct extendedFileInfoStruct
{
    const char* filename;
    const char* metadata;
    char* ret;
    std::size_t retlen;
};

struct extendedFileInfoStructW
{
    const wchar_t* filename;
    const wchar_t* metadata;
    wchar_t* ret;
    std::size_t retlen;
};
