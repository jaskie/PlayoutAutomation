#pragma once
#include "stdafx.h"
#include "Input.h"

namespace TAS {
	namespace FFMpegUtils {
		class VideoDecoder
		{
		private:
			std::map<int, AVFrame*> _buffer;
			void clearBuffer();
			int64_t getFrameCount() const;
			bool openCodec();
			Input * const _input;
			int64_t _readed_pts;
		public:
			VideoDecoder(Input * const input);
			~VideoDecoder();
			AVStream * const Stream;
			const int64_t FrameCount;
			const AVRational FrameRate;
			AVFrame * DecodeNextFrame();
			bool SeekTo(int64_t frameNo);
			const bool DecoderReady;
		};
	}
}
