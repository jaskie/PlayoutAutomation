#include "stdafx.h"
#include "VideoDecoder.h"

#define MAX_GOP_SIZE 300

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
			if (DecoderReady)
				return Stream->nb_frames;
			return 0;
		}

		AVFrame * VideoDecoder::DecodeNextFrame()
		{
			AVFrame * decodedFrame = av_frame_alloc();
			AVPacket * packet;
			int frameReady = 0;
			while (!frameReady && (packet = _input->GetPacket(Stream->index)))
			{
				_readed_pts = packet->pts;
				int bytesRead = avcodec_decode_video2(Stream->codec, decodedFrame, &frameReady, packet);
				av_free_packet(packet);
				if (bytesRead <= 0)
					return nullptr;
			}
			return decodedFrame;
		}

		bool VideoDecoder::openCodec()
		{
			if (Stream)
			{
				AVCodec * codec = avcodec_find_decoder(Stream->codec->codec_id);
				if (codec)
					return avcodec_open2(Stream->codec, codec, NULL) == 0;
			}
			return false;
		}
		
		VideoDecoder::VideoDecoder(Input * const input)
			: _input(input)
			, Stream(input->FindVideoStream())
			, FrameCount(getFrameCount())
			, FrameRate(av_stream_get_r_frame_rate(Stream))
			, DecoderReady(openCodec())
		{

		}
		
		VideoDecoder::~VideoDecoder()
		{
		}

		bool VideoDecoder::SeekTo(int64_t frameNo)
		{
			clearBuffer();
			int64_t pts = Stream->start_time + ((frameNo * Stream->time_base.den * FrameRate.den) / (Stream->time_base.num * FrameRate.num));
			int64_t ptsToInputSeek = pts + Stream->first_dts - 1024; // Why?
			if (_input->Seek(Stream->index, ptsToInputSeek))
			{
				AVFrame * frame = nullptr;
				int frameReadCount = 0;
				do
				{
					frame = DecodeNextFrame();
					if (!frame)
						return false;
					frameReadCount++;
				} while (frameReadCount < MAX_GOP_SIZE && frame->display_picture_number != static_cast<int>(frameNo));
			}
		}
	}
}