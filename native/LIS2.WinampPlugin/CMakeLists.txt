cmake_minimum_required(VERSION 3.20)

project(gen_lis2 LANGUAGES CXX)

if(NOT WIN32)
    message(FATAL_ERROR "The Winamp plugin can only be built on Windows.")
endif()

if(NOT CMAKE_SIZEOF_VOID_P EQUAL 4)
    message(FATAL_ERROR "Classic Winamp general-purpose plugins must be built as 32-bit (Win32).")
endif()

add_library(gen_lis2 SHARED
    plugin.cpp
    gen_lis2.def
    winamp_sdk_min.h
)

target_compile_features(gen_lis2 PRIVATE cxx_std_17)

target_compile_definitions(gen_lis2 PRIVATE
    WIN32_LEAN_AND_MEAN
    NOMINMAX
    _CRT_SECURE_NO_WARNINGS
)

if(MSVC)
    target_compile_options(gen_lis2 PRIVATE /W4 /permissive- /EHsc)
endif()

target_link_libraries(gen_lis2 PRIVATE user32)

set_target_properties(gen_lis2 PROPERTIES
    PREFIX ""
    OUTPUT_NAME "gen_lis2"
)

enable_testing()
add_executable(winamp_metadata_tests plugin_tests.cpp)
target_compile_features(winamp_metadata_tests PRIVATE cxx_std_17)
target_compile_definitions(winamp_metadata_tests PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX _CRT_SECURE_NO_WARNINGS)
target_link_libraries(winamp_metadata_tests PRIVATE user32)
if(MSVC)
    target_compile_options(winamp_metadata_tests PRIVATE /W4 /permissive- /EHsc /utf-8)
endif()
add_test(NAME winamp_metadata COMMAND winamp_metadata_tests)
add_test(NAME winamp_audio_slow_countercheck COMMAND winamp_metadata_tests --slow-audio)
