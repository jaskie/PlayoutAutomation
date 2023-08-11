// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#include <memory>
#include <functional>

// FFMpeg
extern "C" 
{
#include <libavcodec\avcodec.h>
#include <libavformat\avformat.h>
#include <libavutil\rational.h>
#include <libavutil\mathematics.h>
}

// Windows Header Files:

