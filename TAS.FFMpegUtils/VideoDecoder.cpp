#include "stdafx.h"
#include "VideoDecoder.h"

namespace TAS {
	namespace FFMpegUtils {
		void VideoDecoder::clearBuffer()
		{
			for each (auto frame in _buffer)
				av_frame_free(&frame.second);
			_buffer.clear();
		}

		int64_t VideoDecoder::getFrameCount() const
		{
			if (Stream->nb_frames > 0)
			{
				if (Stream->r_frame_rate.num == 50 && Stream->r_frame_rate.den == 1)
					return Stream->nb_frames / 2; //hack to vegas mp4 files
				else
					return Stream->nb_frames;
			}
			else
				return 0;
		}
		
		VideoDecoder::VideoDecoder(AVStream * stream)
			: Stream(stream)
			, FrameCount(getFrameCount())
		{

		}
		
		VideoDecoder::~VideoDecoder()
		{
		}
	}
}