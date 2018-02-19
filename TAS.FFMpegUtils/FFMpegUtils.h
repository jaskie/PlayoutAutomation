// FFMpegUtils.h

#pragma once

#using <mscorlib.dll>

using namespace System;
using namespace System::Runtime::InteropServices;

struct AVDictionary {};

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
		std::unique_ptr<AVFormatContext, std::function<void(AVFormatContext *)>> pFormatCtx;
		int64_t countFrames(unsigned int streamIndex);
		AVFrame* decodeFirstFrame();
	public:
		_FFMpegWrapper(char* fileName);
		int64_t getFrameCount();
		int64_t getAudioDuration();
		int getHeight();
		int getWidth();
		int getStreamCount();
		StreamInfo ^ getStreamInfo(unsigned int streamIndex);
		AVFieldOrder getFieldOrder();
		AVRational getSAR();
		AVRational getFrameRate();
		char* _FFMpegWrapper::getTimeCode();
	};

	// managed code
	public ref class FFMpegWrapper: IDisposable
		{
		private: 
			_FFMpegWrapper* wrapper;
			 String^ _fileName;
		public:
			FFMpegWrapper(String^ fileName)
			{
				_fileName = fileName;
				IntPtr fn = Marshal::StringToHGlobalAnsi(fileName);
				wrapper = new _FFMpegWrapper((char *)fn.ToPointer());
				Marshal::FreeHGlobal(fn);
			}

			~FFMpegWrapper() {
				delete wrapper;
			}


			Int64 GetFrameCount() {
				return wrapper->getFrameCount();
			}

			int GetHeight() {
				return wrapper->getHeight();
			}
			int GetWidth() {
				return wrapper->getWidth();
			}
			TimeSpan^ GetAudioDuration() {
				return gcnew TimeSpan(wrapper->getAudioDuration() * 10);
			}

			FieldOrder GetFieldOrder() {
				return (FieldOrder)(wrapper->getFieldOrder());
			}

			Rational^ GetFrameRate()
			{
				AVRational val = wrapper->getFrameRate();
				Rational ^ ret = gcnew Rational();
				ret->Num = val.num;
				ret->Den = val.den;
				return ret;
			}

			Rational^ GetSAR()
			{
				AVRational val = wrapper->getSAR();
				Rational ^ ret = gcnew Rational();
				ret->Num = val.num;
				ret->Den = val.den;
				return ret;
			}
			String^ GetTimeCode()
			{
				char* tc = wrapper->getTimeCode();
				if (tc)
					return gcnew String(tc);
				else
					return nullptr;
			}
			array<StreamInfo^>^ GetStreamInfo()
			{
				auto ret = gcnew array<StreamInfo^>(wrapper->getStreamCount());
				for (int i = 0; i < ret->Length; i++)
					ret[i] = wrapper->getStreamInfo(i);
				return ret;
			}

		};
	}
}
