function(git_shorttag rev_id)
    find_package(Git)
    if(GIT_FOUND)
        execute_process(
            WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}
            COMMAND ${GIT_EXECUTABLE} rev-parse --short HEAD
            RESULT_VARIABLE _result
            OUTPUT_VARIABLE GIT_OUT OUTPUT_STRIP_TRAILING_WHITESPACE
        )
        if("${_result}" STREQUAL "0")
            set(${rev_id} "${GIT_OUT}" PARENT_SCOPE)
        endif()
    endif()
endfunction()

function(git_append_shorttag var_name)
    git_shorttag(rev_id)
    if(NOT "${rev_id}" STREQUAL "")
        set(${var_name} "${${var_name}}-git${rev_id}" PARENT_SCOPE)
    endif()
endfunction()
