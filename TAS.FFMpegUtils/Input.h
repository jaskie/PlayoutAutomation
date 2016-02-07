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
			/**
			* retrieves next packet with specified stream index and creates queue that acumulates this stream packets 
			*/
			AVPacket * GetPacket(int streamIndex);
			/**
			* creates queue that will acumulate not used yet this stream packets
			*/
			void AddQueue(const int streamIndex);
			bool Seek(const int streamIndex, const int64_t pts);
		};
	}
}