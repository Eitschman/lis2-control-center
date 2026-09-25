cmake_minimum_required(VERSION 3.20)

project(lis2_wmp_legacy LANGUAGES CXX)

if(NOT WIN32)
    message(FATAL_ERROR "The Windows Media Player Legacy plugin can only be built on Windows.")
endif()

add_library(lis2_wmp_legacy SHARED
    plugin.cpp
    lis2_wmp_legacy.def
)

target_compile_features(lis2_wmp_legacy PRIVATE cxx_std_17)

target_compile_definitions(lis2_wmp_legacy PRIVATE
    WIN32_LEAN_AND_MEAN
    NOMINMAX
    UNICODE
    _UNICODE
)

if(MSVC)
    target_compile_options(lis2_wmp_legacy PRIVATE /W4 /permissive- /EHsc)
endif()

target_link_libraries(lis2_wmp_legacy PRIVATE
    ole32
    oleaut32
    user32
    advapi32
)

set_target_properties(lis2_wmp_legacy PROPERTIES
    PREFIX ""
    OUTPUT_NAME "lis2_wmp_legacy"
)
