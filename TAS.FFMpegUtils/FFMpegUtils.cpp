// This is the main DLL file.
#include "stdafx.h"
#include "FFMpegUtils.h"

namespace TAS {
	namespace FFMpegUtils {

		AVFormatContext * open_file(char * fileName)
		{
			AVFormatContext * ctx = nullptr;
			int ret = avformat_open_input(&ctx, fileName, NULL, NULL);
			if (ret == 0)
			{
				ret = avformat_find_stream_info(ctx, NULL);
				if (ret < 0)
					OutputDebugString(L"avformat_find_stream_info failed");
			}
			else
				OutputDebugString(L"avformat_open_input failed");
			return ctx;
		}

		AVCodecContext * open_codec(AVCodecContext * ctx)
		{
			AVCodec * codec = avcodec_find_decoder(ctx->codec_id);
			int ret = avcodec_open2(ctx, codec, NULL);
			if (ret == 0)
				return ctx;
			return nullptr;
		}

		// unmanaged object
		_FFMpegWrapper::_FFMpegWrapper(char* fileName)
		{
			pFormatCtx = std::unique_ptr<AVFormatContext, std::function<void(AVFormatContext *)>>(open_file(fileName), ([](AVFormatContext * ctx)
			{			
				avformat_close_input(&ctx);
			}));
		};

		int64_t _FFMpegWrapper::getVideoDuration()
		{
			if (pFormatCtx)
				for (unsigned int i = 0; i < pFormatCtx->nb_streams; i++)
				{
					AVRational time_base = pFormatCtx->streams[i]->time_base;
					int64_t duration = pFormatCtx->streams[i]->duration;
					return av_rescale(duration * AV_TIME_BASE, time_base.num, time_base.den);
				}
			// if not found
			return 0; 
		}

		int64_t _FFMpegWrapper::countFrames(unsigned int streamIndex)
		{
			int64_t frameCount = 0;
			if (pFormatCtx)
			{
				std::unique_ptr<AVPacket, std::function<void(AVPacket *)>> packet(av_packet_alloc(), [](AVPacket *p) { av_packet_free(&p); });
				while (av_read_frame(pFormatCtx.get(), packet.get()) >= 0)
				{
					if (packet->stream_index == streamIndex)
						frameCount++;
				}
			}
			return frameCount; 
		}

		int64_t _FFMpegWrapper::getAudioDuration()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
					if (pFormatCtx->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_AUDIO)
						//result is in AV_TIME_BASE units
					{
						AVRational time_base = pFormatCtx->streams[i]->time_base;
						int64_t duration = pFormatCtx->streams[i]->duration;
						return av_rescale(duration * AV_TIME_BASE, time_base.num, time_base.den);
					}
			} 
			// if not found
			return 0; 
		}

		int64_t _FFMpegWrapper::getFileDuration()
		{
			if (pFormatCtx)
			{
				return pFormatCtx->duration;
			}
			// if not found
			return 0;
		}

		int _FFMpegWrapper::getHeight()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
				{
					if(pFormatCtx->streams[i]->codecpar->codec_type==AVMEDIA_TYPE_VIDEO) 
						return pFormatCtx->streams[i]->codecpar->height;
				} 
			}
			return 0; 
		}
		
		int _FFMpegWrapper::getWidth()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
				{
					if(pFormatCtx->streams[i]->codecpar->codec_type==AVMEDIA_TYPE_VIDEO) 
						return pFormatCtx->streams[i]->codecpar->width;
				} 
			}
			return 0; 
		}

		AVFrame* _FFMpegWrapper::decodeFirstFrame()
		{
			if (pFormatCtx)
			{
				for (unsigned int i = 0; i < pFormatCtx->nb_streams; i++)
				{
					AVCodecParameters *codecCtx = pFormatCtx->streams[i]->codecpar;
					if (codecCtx->codec_type == AVMEDIA_TYPE_VIDEO)
					{
						std::unique_ptr<AVFrame, std::function<void(AVFrame *)>> picture(av_frame_alloc(), [](AVFrame *frame) { av_frame_free(&frame); });
						std::unique_ptr<AVPacket, std::function<void(AVPacket *)>> packet(av_packet_alloc(), [](AVPacket *p) { av_packet_free(&p); });
						
						std::unique_ptr<AVCodecContext, std::function<void(AVCodecContext *)>> opened_context(open_codec(pFormatCtx->streams[i]->codec), [](AVCodecContext * ctx) {
							avcodec_close(ctx);
						});
						if (!opened_context)
						{
							return NULL; // unable to open codec
						}
						bool readSuccess = true;
						int frameFinished = 0;
						int bytesDecoded = 0;
						do
						{
							readSuccess = (av_read_frame(pFormatCtx.get(), packet.get()) == 0);
							if (readSuccess
								&& packet->stream_index == i
								&& packet->size > 0
								&& (bytesDecoded = avcodec_decode_video2(opened_context.get(), picture.get(), &frameFinished, packet.get())) > 0)
							{
								if (frameFinished)
									return picture.get();
							}
						} while (!frameFinished && readSuccess);
						return NULL;
					}
				}
			}
			return NULL;
		}

		AVFieldOrder _FFMpegWrapper::getFieldOrder()
		{
			if (pFormatCtx)
			{
				for (unsigned int i = 0; i < pFormatCtx->nb_streams; i++)
				{
					AVCodecContext *codecCtx = pFormatCtx->streams[i]->codec;
					if (codecCtx->codec_type == AVMEDIA_TYPE_VIDEO)
					{
						if (codecCtx->field_order == AV_FIELD_UNKNOWN)
						{
							std::unique_ptr<AVFrame, std::function<void(AVFrame *)>> picture(av_frame_alloc(), [](AVFrame *frame) {
								av_frame_free(&frame); }
							);
							std::unique_ptr<AVPacket, std::function<void(AVPacket *)>> packet(av_packet_alloc(), [](AVPacket *p) {
								av_packet_free(&p);
							});
							std::unique_ptr<AVCodecContext, std::function<void(AVCodecContext *)>> opened_context(open_codec(codecCtx), [](AVCodecContext * ctx) {
								avcodec_close(ctx);
							});
							if (!opened_context)
								return AV_FIELD_UNKNOWN; // unable to open codec
							bool readSuccess = true;
							int frameFinished = 0;
							int bytesDecoded = 0;
							do
							{
								readSuccess = (av_read_frame(pFormatCtx.get(), packet.get()) == 0);
								if (readSuccess
									&& packet->stream_index == i
									&& packet->size > 0
									&& (bytesDecoded = avcodec_decode_video2(opened_context.get(), picture.get(), &frameFinished, packet.get())) > 0)
								{
									if (frameFinished)
									{
										if (picture->interlaced_frame)
											if (picture->top_field_first)
												return AV_FIELD_TT;
											else
												return AV_FIELD_BB;
										else
											return AV_FIELD_PROGRESSIVE;
									}
								}
							} while (!frameFinished && readSuccess);
							return AV_FIELD_UNKNOWN;
						}
						else
							return codecCtx->field_order;
					}
				}
			}
			return AV_FIELD_UNKNOWN;
		}

		bool _FFMpegWrapper::getHasTransparency()
		{
			if (pFormatCtx)
			{
				for (unsigned int i = 0; i < pFormatCtx->nb_streams; i++)
				{
					AVCodecParameters* codecPar = pFormatCtx->streams[i]->codecpar;
					if (codecPar->codec_type == AVMEDIA_TYPE_VIDEO)
					{
						const AVPixFmtDescriptor* pix_fmt_desc = av_pix_fmt_desc_get((AVPixelFormat)codecPar->format);
						return pix_fmt_desc ? pix_fmt_desc->flags & AV_PIX_FMT_FLAG_ALPHA : false;
					}
				}
			}
			return false;
		}

		AVRational _FFMpegWrapper::getSAR()
		{
			if (pFormatCtx)
			{
				for (unsigned int i = 0; i<pFormatCtx->nb_streams; i++)
				{
					if (pFormatCtx->streams[i]->codec->codec_type == AVMEDIA_TYPE_VIDEO)
						return pFormatCtx->streams[i]->sample_aspect_ratio;
				}
			}
			return av_make_q(0, 0);
		}

		AVRational _FFMpegWrapper::getFrameRate()
		{
			if (pFormatCtx)
			{
				for (unsigned int i = 0; i<pFormatCtx->nb_streams; i++)
				{
					if (pFormatCtx->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO)
						return pFormatCtx->streams[i]->r_frame_rate;
				}
			}
			return av_make_q(0, 0);
		}

		int _FFMpegWrapper::getStreamCount()
		{
			if (pFormatCtx)
				return pFormatCtx->nb_streams;
			return 0;
		}

		char* _FFMpegWrapper::getTimeCode()
		{
			if (pFormatCtx)
			{
				AVDictionaryEntry* entry = av_dict_get(pFormatCtx->metadata, "timecode", NULL, 0);
				if (entry)
					return entry->value;
			}
			return NULL;
		}

		StreamInfo ^ _FFMpegWrapper::getStreamInfo(unsigned int streamIndex) 
		{
			StreamInfo ^ ret = gcnew StreamInfo();
			if (pFormatCtx && pFormatCtx->nb_streams > streamIndex)
			{
				ret->StreamType = (StreamType)pFormatCtx->streams[streamIndex]->codec->codec_type;
				ret->Id = pFormatCtx->streams[streamIndex]->id;
				ret->Index = pFormatCtx->streams[streamIndex]->index;
				ret->ChannelCount = pFormatCtx->streams[streamIndex]->codecpar->channels;
			}
			return ret;
		}
	}
}
