// This is the main DLL file.
#include "stdafx.h"
#include "FFMpegUtils.h"

namespace TAS {
	namespace FFMpegUtils {

		AVFormatContext * open_file(char * fileName)
		{
			AVFormatContext * ctx = nullptr;
			if (avformat_open_input(&ctx, fileName, NULL, NULL) == 0)
				avformat_find_stream_info(ctx, NULL);
			return ctx;
		}

		AVCodecContext * open_codec(AVCodecContext * codec)
		{
			if (avcodec_open2(codec, NULL, NULL) == 0)
				return codec;
			return nullptr;
		}

		// unmanaged object
		_FFMpegWrapper::_FFMpegWrapper(char* fileName)
		{
			av_register_all();			
			pFormatCtx = std::unique_ptr<AVFormatContext, std::function<void(AVFormatContext *)>>(open_file(fileName), ([](AVFormatContext * ctx)
			{			
				avformat_close_input(&ctx);
			}));
		};

		int64_t _FFMpegWrapper::getFrameCount()
		{
			if (pFormatCtx)
				for (unsigned int i = 0; i < pFormatCtx->nb_streams; i++)
				{
					AVStream* stream = pFormatCtx->streams[i];
					if (stream->codecpar->codec_type == AVMEDIA_TYPE_VIDEO)
						if (stream->nb_frames > 0)
							return stream->nb_frames;
						else
							return (stream->duration * stream->time_base.num * stream->r_frame_rate.num) / (stream->time_base.den * stream->r_frame_rate.den);
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
						return pFormatCtx->streams[i]->codec->width;
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
					AVCodecContext *codecCtx = pFormatCtx->streams[i]->codec;
					if (codecCtx->codec_type == AVMEDIA_TYPE_VIDEO)
					{
						std::unique_ptr<AVFrame, std::function<void(AVFrame *)>> picture(av_frame_alloc(), [](AVFrame *frame) { av_frame_free(&frame); });
						std::unique_ptr<AVPacket, std::function<void(AVPacket *)>> packet(av_packet_alloc(), [](AVPacket *p) { av_packet_free(&p); });
						std::unique_ptr<AVCodecContext, std::function<void(AVCodecContext *)>> opened_context(open_codec(codecCtx), [](AVCodecContext * ctx) {
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
							AVCodec * codec = avcodec_find_decoder(codecCtx->codec_id);
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
					}
					else
						return codecCtx->field_order;

				}
			}
			return AV_FIELD_UNKNOWN;
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
						return av_stream_get_r_frame_rate(pFormatCtx->streams[i]);
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

		// managed object

		FFMpegWrapper::FFMpegWrapper(String^ fileName)
		{
			_fileName = fileName;
			IntPtr fn = Marshal::StringToHGlobalAnsi(fileName);
			wrapper = new _FFMpegWrapper((char *)fn.ToPointer());
			Marshal::FreeHGlobal(fn);
		};

		FFMpegWrapper::~FFMpegWrapper()
		{
			delete wrapper;
		};

		Int64 FFMpegWrapper::GetFrameCount()
		{
			return wrapper->getFrameCount();
		}

		TimeSpan^ FFMpegWrapper::GetAudioDuration()
		{
			return gcnew TimeSpan(wrapper->getAudioDuration() * 10);
		}

		int FFMpegWrapper::GetHeight()
		{
			return wrapper->getHeight();
		}

		int FFMpegWrapper::GetWidth()
		{
			return wrapper->getWidth();
		}

		FieldOrder FFMpegWrapper::GetFieldOrder()
		{
			return (FieldOrder)(wrapper->getFieldOrder());
		}

		Rational^ FFMpegWrapper::GetSAR()
		{
			AVRational val = wrapper->getSAR();
			Rational ^ ret = gcnew Rational();
			ret->Num = val.num;
			ret->Den = val.den;
			return ret;
		}

		Rational^ FFMpegWrapper::GetFrameRate()
		{
			AVRational val = wrapper->getFrameRate();
			Rational ^ ret = gcnew Rational();
			ret->Num = val.num;
			ret->Den = val.den;
			return ret;
		}

		array<StreamInfo^>^ FFMpegWrapper::GetStreamInfo()
		{
			auto ret = gcnew array<StreamInfo^>(wrapper->getStreamCount());
			for (int i = 0; i < ret->Length; i++)
				ret[i] = wrapper->getStreamInfo(i);
			return ret;
		}

		String^ FFMpegWrapper::GetTimeCode()
		{
			char* tc = wrapper->getTimeCode();
			if (tc)
				return gcnew String(tc);
			else
				return nullptr;
		}
	}
}
