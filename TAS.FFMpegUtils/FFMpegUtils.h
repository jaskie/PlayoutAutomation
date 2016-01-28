// FFMpegUtils.h

#pragma once

#using <mscorlib.dll>
#include <windows.h>

using namespace System;
using namespace System::Runtime::InteropServices;


namespace TAS {
	namespace FFMpegUtils {

	public enum class FieldOrder 
	{
		    UNKNOWN,
			PROGRESSIVE,
			TT,          //< Top coded_first, top displayed first
			BB,          //< Bottom coded first, bottom displayed first
			TB,          //< Top coded first, bottom displayed first
			BT,          //< Bottom coded first, top displayed first
	};

	public ref struct Rational
	{
		int Num;
		int Den;
	};

	public enum class StreamType {
		UNKNOWN = -1,  ///< Usually treated as AVMEDIA_TYPE_DATA
		VIDEO,
		AUDIO,
		DATA,          ///< Opaque data information usually continuous
		SUBTITLE,
		ATTACHMENT,    ///< Opaque data information usually sparse
		NB
	};

	public ref struct StreamInfo
	{
		int Id;
		int Index;
		int ChannelCount;
		StreamType StreamType;
	};

	// native code
	class _FFMpegWrapper
	{
	private:
		AVFormatContext	*pFormatCtx;
		int64_t countFrames(unsigned int streamIndex);
		void renderFrame(AVFrame* frame);
		AVFrame *decodeFirstFrame();
		HWND hWnd;
	public:
		_FFMpegWrapper(char* fileName);
		_FFMpegWrapper(char* fileName, HWND hWnd);
		~_FFMpegWrapper();
		int64_t getFrameCount();
		int64_t getAudioDuration();
		int getHeight();
		int getWidth();
		int getStreamCount();
		StreamInfo ^ getStreamInfo(unsigned int streamIndex);
		AVFieldOrder getFieldOrder();
		AVRational getSAR();
		AVRational getFrameRate();
		bool readNextPacket(AVPacket* packetToRead);
//		AVPacket* readedPacket;
		bool Seek(int64_t position);
	};

	// managed code
	public ref class FFMpegWrapper
		{
		private: 
			_FFMpegWrapper* wrapper;
			 String^ _fileName;
			 IntPtr _windowHandle;
		public:
			FFMpegWrapper(String^ fileName);
			FFMpegWrapper(String^ fileName, IntPtr hWnd);
			~FFMpegWrapper();
			Int64 GetFrameCount();
			int GetHeight();
			int GetWidth();
			TimeSpan^ GetAudioDuration();
			FieldOrder GetFieldOrder();
			Rational^ GetFrameRate();
			Rational^ GetSAR();
//			bool GetFrame(TimeSpan fromTime, Bitmap^ destBitmap);
			array<StreamInfo^>^ GetStreamInfo();
			bool Seek(TimeSpan time);
		};
	}
}
