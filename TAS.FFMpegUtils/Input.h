#pragma once
#include "stdafx.h"
#include <map>
#include <queue>

namespace TAS {
	namespace FFMpegUtils {
		
		class Input
		{
		private:
			AVFormatContext * pFormatCtx;
			std::map<int, std::queue<AVPacket*>> queues;
			bool readPacket();
			void clearQueues();
		public:
			Input(const char * fileName);
			~Input();
			const bool InputReady;
			AVStream * FindVideoStream();
			AVPacket * GetPacket(int streamIndex);
			bool Seek(int streamIndex, int64_t timestamp);
		};
	}
}