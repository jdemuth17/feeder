# UniversalFeeder ESP-IDF Firmware
# Replaces nanoFramework implementation with native ESP-IDF C

cmake_minimum_required(VERSION 3.16)

# Set your component requires here
set(EXTRA_COMPONENT_DIRS "")

include($ENV{IDF_PATH}/tools/cmake/project.cmake)

project(universal_feeder)
