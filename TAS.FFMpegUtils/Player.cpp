#include "stdafx.h"
#include "Player.h"

namespace TAS {
	namespace FFMpegUtils {

		int64_t GetTimeFromFrameNumber(const AVRational frameRate, const int64_t frameNo)
		{
			return AV_TIME_BASE * (frameNo * frameRate.den) / (frameRate.num);
		}

		int64_t GetFrameNumberFromTime(const int64_t time, const AVRational frameRate)
		{
			return time * frameRate.num / (AV_TIME_BASE * frameRate.den);
		}

		Player::Player()
		{
			av_register_all();
		}

		Player::~Player()
		{
			Close();
		}
		void Player::SetVideoDevice(HDC device, int width, int height)
		{
			_device = device;
			_width = width;
			_height = height;
		}
		void Player::Play()
		{
			_playState = PLAYING;
		}
		
		void Player::Seek(int64_t frame)
		{
			if (_videoDecoder
				&& _videoDecoder->DecoderReady
				&& _videoDecoder->SeekTo(frame))
				_playState = PAUSED;
			else
				Close();
		}

		void Player::Open(char * fileName)
		{
			if (_playState != IDLE)
				Close();
			_input = new Input(fileName);
			if (_input->InputReady)
			{
				_videoDecoder = new VideoDecoder(_input);
				if (_videoDecoder->DecoderReady)
				{
					_playState = PAUSED;
					return;
				}
			}
			Close();
		}

		void Player::Close()
		{
			if (_input)
				delete _input;
			if (_videoDecoder)
				delete _videoDecoder;
			_input = nullptr;
			_videoDecoder = nullptr;
			_playState = IDLE;
		}
		PLAY_STATE Player::GetPlayState() const
		{
			return _playState;
		}
		int64_t Player::GetCurrentFrame() const
		{
			return _currentFrame;
		}
		int64_t Player::GetFramesCount() const
		{
			if (_playState != IDLE && _videoDecoder)
				return _videoDecoder->FrameCount;
			return 0;
		}
	}
}