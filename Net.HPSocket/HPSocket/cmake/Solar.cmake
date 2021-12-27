include(${CMAKE_CURRENT_LIST_DIR}/ForceOutBuild.cmake)
include(${CMAKE_CURRENT_LIST_DIR}/GitUtils.cmake)
include(${CMAKE_CURRENT_LIST_DIR}/MiscUtils.cmake)

# Check if the C/C++ compiler is Clang/LLVM
is_symbol_defined(CMAKE_COMPILER_IS_CLANGXX __clang__)
