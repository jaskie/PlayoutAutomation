#include "stdafx.h"
#include "Player.h"

namespace TAS {
	namespace FFMpegUtils {

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
			_playState = PAUSED;
		}
		void Player::Open(char * fileName)
		{
			if (_playState != IDLE)
				Close();
			_input = new Input(fileName);
			if (_input->InputReady)
			{
				_videoDecoder = new VideoDecoder(_input->FindVideoStream());
				_playState = PAUSED;
			}
			else
				Close();

		}
		void Player::Close()
		{
			if (_input)
				delete _input;
			if (_videoDecoder)
				delete _videoDecoder;
			_input = nullptr;
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