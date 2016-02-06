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
		public:
			VideoDecoder(AVStream * stream);
			~VideoDecoder();
			const AVStream * Stream;
			const int64_t FrameCount;
		};
	}
}
