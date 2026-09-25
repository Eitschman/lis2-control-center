cmake_minimum_required(VERSION 3.20)

project(lis2_wmp_legacy_visualization LANGUAGES CXX)

if(NOT WIN32)
    message(FATAL_ERROR "The WMP Legacy visualization can only be built on Windows.")
endif()

add_library(lis2_wmp_legacy_visualization SHARED
    visualization.cpp
    lis2_wmp_legacy_visualization.def
)

target_compile_features(lis2_wmp_legacy_visualization PRIVATE cxx_std_17)

target_compile_definitions(lis2_wmp_legacy_visualization PRIVATE
    WIN32_LEAN_AND_MEAN
    NOMINMAX
    UNICODE
    _UNICODE
)

if(MSVC)
    target_compile_options(lis2_wmp_legacy_visualization PRIVATE /W4 /permissive- /EHsc)
endif()

target_link_libraries(lis2_wmp_legacy_visualization PRIVATE
    ole32
    oleaut32
    user32
    gdi32
    advapi32
)

set_target_properties(lis2_wmp_legacy_visualization PROPERTIES
    PREFIX ""
    OUTPUT_NAME "lis2_wmp_legacy_visualization"
)
