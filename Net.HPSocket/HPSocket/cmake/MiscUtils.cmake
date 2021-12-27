function(group_by_folder relative_to_folder files)
    foreach(core_SOURCE ${files})
        # Get the path of the file relative to the current source directory
        file(RELATIVE_PATH core_SOURCE_relative "${relative_to_folder}" "${core_SOURCE}")

        # Get the relative folder path
        get_filename_component(core_SOURCE_dir "${core_SOURCE_relative}" PATH)

        # Convert forward slashes to backslashes to get source group identifiers
        string(REPLACE "/" "\\" core_SOURCE_group "${core_SOURCE_dir}")

        source_group("${core_SOURCE_group}" FILES "${core_SOURCE}")
    endforeach()
endfunction()

# Detects whether a preprocessor symbol is defined by the current C compiler
function(is_symbol_defined output_variable symbol)
    enable_language(C)

set(is_symbol_defined_code "
#if defined(${symbol})
int main() { return 0; }
#endif
")

    file(WRITE "${CMAKE_BINARY_DIR}/is_symbol_defined.c" "${is_symbol_defined_code}")

    try_compile(is_symbol_defined_result "${CMAKE_BINARY_DIR}" "${CMAKE_BINARY_DIR}/is_symbol_defined.c")

    if(is_symbol_defined_result)
        set(${output_variable} TRUE PARENT_SCOPE)
    else()
        set(${output_variable} FALSE PARENT_SCOPE)
    endif()
endfunction()

macro (my_add_path _map_var)
    foreach(_map_src ${ARGN})
		if(IS_ABSOLUTE "${_map_src}")
            file(RELATIVE_PATH _map_src "${CMAKE_SOURCE_DIR}" "${_map_src}")
            #message(STATUS "src = ${_map_src}")
        else()
            get_filename_component(_map_src "${_map_src}" REALPATH)
            file(RELATIVE_PATH _map_src "${CMAKE_SOURCE_DIR}" "${_map_src}")
        endif()

        list (APPEND ${_map_var} "${_map_src}")
    endforeach()

    file(RELATIVE_PATH _map_relSrcDirPath "${CMAKE_SOURCE_DIR}" "${CMAKE_CURRENT_SOURCE_DIR}")
    if(_map_relSrcDirPath)
        # propagate ${_map_var} to parent directory
        set (${_map_var} ${${_map_var}} PARENT_SCOPE)
    endif()
endmacro()

macro(add_subdirectory_optional _aso_dir SUCCESS_VAR)
   get_filename_component(_aso_fullPath "${_aso_dir}" ABSOLUTE)
   if(EXISTS "${_aso_fullPath}/CMakeLists.txt")
      add_subdirectory("${_aso_dir}")
      set(${SUCCESS_VAR} 1)
   else()
      set(${SUCCESS_VAR} 0)
   endif()
endmacro()

macro(my_add_subdirs)
    foreach (_mas_path ${ARGN})
    	add_subdirectory_optional("${_mas_path}" _mas_success)
    	if(_mas_success)
            message(STATUS "Found CMake subdir under ${_mas_path}")
    	else()
    	    file(GLOB_RECURSE _mas_sourceFiles
    	                      "${_mas_path}/*.c"
    	                      "${_mas_path}/*.c++"
    	                      "${_mas_path}/*.cc"
    	                      "${_mas_path}/*.cpp"
    	                      "${_mas_path}/*.cxx"
    	                      "${_mas_path}/*.h"
    	                      "${_mas_path}/*.h++"
    	                      "${_mas_path}/*.hpp"
    	                      "${_mas_path}/*.hxx")

            my_add_path(MY_SRCS ${_mas_sourceFiles})
        endif()
    endforeach()
endmacro()

macro(add_cxx_flag_if_supported _tgt_var FLAG)
    check_cxx_compiler_flag("${FLAG}" HAVE_FLAG_${FLAG})

    if(HAVE_FLAG_${FLAG})
        target_compile_options(${${_tgt_var}} PRIVATE ${FLAG})
    endif()
endmacro()
