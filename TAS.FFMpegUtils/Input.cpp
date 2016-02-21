#include "stdafx.h"
#include "Input.h"

namespace TAS {
	namespace FFMpegUtils {
		bool Input::readPacket()
		{
			AVPacket * pPacket = (AVPacket*)av_malloc(sizeof(AVPacket));
			if (av_read_frame(pFormatCtx, pPacket) == 0)
			{
				if (queues.count(pPacket->stream_index) == 1)
				{
					if (pPacket->buf == NULL)
						av_dup_packet(pPacket);
					queues[pPacket->stream_index].push(pPacket);
				}
				else
					av_free_packet(pPacket);
				return true;
			};
			return false;
		}

		void Input::clearQueues()
		{
			auto q = queues;
			for each (auto queue in q)
			{
				while (!queue.second.empty())
				{
					av_free_packet(queue.second.front());
					queue.second.pop();
				}
			}
		}

		Input::Input(const char * fileName):
			pFormatCtx(nullptr)
		{

			InputReady = !avformat_open_input(&pFormatCtx, fileName, NULL, NULL) || !avformat_find_stream_info(pFormatCtx, NULL);
		}

		Input::~Input()
		{
			clearQueues();
			if (pFormatCtx)
				avformat_close_input(&pFormatCtx);
		}

		AVStream * Input::FindVideoStream()
		{
			for (unsigned int i = 0; i < pFormatCtx->nb_streams; i++)
				if (pFormatCtx->streams[i]->codec->codec_type == AVMEDIA_TYPE_VIDEO)
					return pFormatCtx->streams[i];
			return nullptr;
		}

		AVPacket * Input::GetPacket(int streamIndex)
		{
			bool empty;
			AVPacket * packet = nullptr;
			while ((empty = queues[streamIndex].empty()) && readPacket()) {};
			if (!empty)
			{
				packet = queues[streamIndex].front();
				queues[streamIndex].pop();
			}
			return packet;
		}

		void Input::AddQueue(const int streamIndexe)
		{
			queues[streamIndexe].empty();
		}
		
		bool Input::Seek(const int streamIndex, const int64_t pts)
		{
			clearQueues();
			return av_seek_frame(pFormatCtx, streamIndex, pts, 0) >= 0;
		}
	}
}