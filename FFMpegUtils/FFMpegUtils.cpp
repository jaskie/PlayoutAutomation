// This is the main DLL file.
#include "stdafx.h"
#include "FFMpegUtils.h"


namespace TAS {
	namespace FFMpegUtils {

		// unmanaged opbject
		_FFMpegWrapper::_FFMpegWrapper(char* fileName)
		{
			av_register_all();
			av_log_set_level(AV_LOG_DEBUG);
			pFormatCtx=NULL;
			if (avformat_open_input(&pFormatCtx, fileName, NULL, NULL) == 0)
				avformat_find_stream_info(pFormatCtx, NULL);
		};

		_FFMpegWrapper::~_FFMpegWrapper()
		{
			if (pFormatCtx)
				avformat_close_input(&pFormatCtx); 
		};

		int64_t _FFMpegWrapper::getFrameCount()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
				{
					if(pFormatCtx->streams[i]->codec->codec_type==AVMEDIA_TYPE_VIDEO) 
					{
						if (pFormatCtx->streams[i]->nb_frames > 0)
						{
							if (pFormatCtx->streams[i]->r_frame_rate.num == 50 && pFormatCtx->streams[i]->r_frame_rate.den == 1)
								return pFormatCtx->streams[i]->nb_frames/2; //hack to vegas mp4 files
							else
								return pFormatCtx->streams[i]->nb_frames; 
						}
						else
							return 0;//countFrames(i);
					}
				}
			} 
			// if not found
			return 0; 
		}

		int64_t _FFMpegWrapper::countFrames(unsigned int streamIndex)
		{
			int64_t frameCount = 0;
			if (pFormatCtx)
			{
				AVPacket *pOutput;
				pOutput = (AVPacket *) av_malloc(sizeof(AVPacket));
				while (av_read_frame(pFormatCtx, pOutput) >= 0)
				{
					if (pOutput->stream_index == streamIndex)
						frameCount++;
					av_free_packet(pOutput);
				}
			}
			return frameCount; 
				//((int64_t)stream->duration * stream->time_base.num * stream->r_frame_rate.num)/(stream->time_base.den * stream->r_frame_rate.den); 
		}

		int64_t _FFMpegWrapper::getAudioDuration()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
					if(pFormatCtx->streams[i]->codec->codec_type==AVMEDIA_TYPE_AUDIO) 
						//result is in AV_TIME_BASE units
						return (pFormatCtx->streams[i]->duration * pFormatCtx->streams[i]->time_base.num * AV_TIME_BASE) / pFormatCtx->streams[i]->time_base.den; 
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
					if(pFormatCtx->streams[i]->codec->codec_type==AVMEDIA_TYPE_VIDEO) 
						return pFormatCtx->streams[i]->codec->coded_height;
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
					if(pFormatCtx->streams[i]->codec->codec_type==AVMEDIA_TYPE_VIDEO) 
						return pFormatCtx->streams[i]->codec->coded_width;
				} 
			}
			return 0; 
		}

		AVFieldOrder _FFMpegWrapper::getFieldOrder()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
				{
					if(pFormatCtx->streams[i]->codec->codec_type==AVMEDIA_TYPE_VIDEO) 
						return pFormatCtx->streams[i]->codec->field_order;
				} 
			}
			return AV_FIELD_UNKNOWN;
		}

		AVRational _FFMpegWrapper::getAspectRatio()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
				{
					if(pFormatCtx->streams[i]->codec->codec_type==AVMEDIA_TYPE_VIDEO) 
						return pFormatCtx->streams[i]->codec->sample_aspect_ratio;
				} 
			}
			return av_make_q(0, 0);
		}

		AVRational _FFMpegWrapper::getFrameRate()
		{
			return av_make_q(0, 0);
		}

		// managed object

		FFMpegWrapper::FFMpegWrapper(String^ fileName)
		{
			char* fn = (char*) Marshal::StringToHGlobalAnsi(fileName).ToPointer();
			wrapper = new _FFMpegWrapper(fn);
			Marshal::FreeHGlobal(IntPtr((void*)fn));
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

		Rational^ FFMpegWrapper::GetAspectRatio()
		{
			AVRational val = wrapper->getAspectRatio();
			Rational ^ ret = gcnew Rational();
			ret->Num = val.num;
			ret->Den = val.den;
			return ret;
		}

		Rational^ FFMpegWrapper::GetFrameRate()
		{
			return gcnew Rational();
		}

		bool FFMpegWrapper::GetFrame(TimeSpan fromTime, Bitmap^ destBitmap)
		{
			if (destBitmap == nullptr)
				return false;
			return true;
		}

	}
}
