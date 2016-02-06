#pragma once
#include "stdafx.h"
#include "Input.h"
#include "VideoDecoder.h"

namespace TAS {
	namespace FFMpegUtils {

		public enum PLAY_STATE
		{
			IDLE,
			PLAYING,
			PAUSED,
		};

		class Player
		{
		private:
				int _width;
				int _height;
				HDC _device;
				Input * _input;
				VideoDecoder * _videoDecoder;
				PLAY_STATE _playState;
				int64_t _currentFrame;
		public:
			Player();
			~Player();
			void SetVideoDevice(HDC device, int width, int height);
			void Play();
			void Seek(int64_t frame);
			void Pause();
			void Open(char* fileName);
			void Close();
			PLAY_STATE GetPlayState() const;
			int64_t GetCurrentFrame() const;
			int64_t GetFramesCount() const;
		};

	}
}