project(HPSocket4C LANGUAGES CXX C)
cmake_minimum_required(VERSION 3.18.0)

set(CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake" ${CMAKE_MODULE_PATH})

set(CMAKE_CXX_STANDARD 14)

include(Solar)
include(CheckCXXCompilerFlag)

message(STATUS "CMake version: ${CMAKE_VERSION}")

######################################
git_shorttag(rev_id)
message(STATUS "Git revision ${rev_id}")

######################################
# debug/release
set(CMAKE_BUILD_TYPE RelWithDebInfo 		CACHE STRING "Choose the type of build, options are: None Debug Release RelWithDebInfo MinSizeRel.")
set(CMAKE_VERBOSE_MAKEFILE ON 				CACHE STRING "Verbose makefile")
set(BUILD_SHARED_LIBS ON					CACHE STRING "Build shared libs")

my_add_path(MY_INCL "${CMAKE_CURRENT_SOURCE_DIR}/Include")

my_add_subdirs(
	include
	src
)

add_library(${PROJECT_NAME} ${MY_SRCS} ${MY_INCL})
target_include_directories(${PROJECT_NAME} PRIVATE "${MY_INCL}")

file(GLOB_RECURSE CFILES "${CMAKE_SOURCE_DIR}/*.c")
SET_SOURCE_FILES_PROPERTIES(${CFILES} PROPERTIES LANGUAGE CXX)

target_compile_definitions(${PROJECT_NAME} PRIVATE "$<$<CONFIG:Debug>:_DEBUG>")
target_compile_definitions(${PROJECT_NAME} PRIVATE "$<$<CONFIG:Release,RelWithDebInfo>:NDEBUG>")
target_compile_definitions(${PROJECT_NAME} PRIVATE WIN32 _WINDOWS _USRDLL HPSOCKET_EXPORTS _NOT_USE_PRIVATE_HEAP)

target_precompile_headers(${PROJECT_NAME} PRIVATE Include/stdafx.h)

if (MSVC)
    # warning level 4
	string(REPLACE "/W3" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
	target_compile_options(${PROJECT_NAME} PRIVATE /W4)
else()
    # lots of warnings and all warnings as errors
    add_compile_options(-Wall -Wextra -pedantic)
endif()
