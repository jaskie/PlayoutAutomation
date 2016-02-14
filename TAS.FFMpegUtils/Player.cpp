#include "stdafx.h"
#include "Player.h"

namespace TAS {
	namespace FFMpegUtils {

#pragma region Umanaged code

		int64_t GetTimeFromFrameNumber(const AVRational frameRate, const int64_t frameNo)
		{
			return AV_TIME_BASE * (frameNo * frameRate.den) / (frameRate.num);
		}

		int64_t GetFrameNumberFromTime(const int64_t time, const AVRational frameRate)
		{
			return time * frameRate.num / (AV_TIME_BASE * frameRate.den);
		}
		
		void CALLBACK TimerCallback(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2)
		{
			if (((_Player*)dwUser)->_timerId == uTimerID)
			{

			}
		}

		void _Player::_closeTimer()
		{
			if (_timerId)
			{
				timeKillEvent(_timerId);
				_timerId = 0;
			}

		}

		_Player::_Player()
		{
			av_register_all();
			timeGetDevCaps(&_timerCaps, sizeof(_timerCaps));
			_timerId = NULL;
		}

		_Player::~_Player()
		{
			Close();
		}

		void _Player::SetVideoDevice(HDC device, int width, int height)
		{
			_device = device;
			_width = width;
			_height = height;
		}

		void _Player::Play()
		{
			_playState = PLAYING;
			_timerId = timeSetEvent(40, 0, TimerCallback, (DWORD)this, TIME_PERIODIC);
		}
		
		void _Player::Seek(int64_t frame)
		{
			if (_videoDecoder
				&& _videoDecoder->DecoderReady
				&& _videoDecoder->SeekTo(frame))
				_playState = PAUSED;
			else
				Close();
		}

		void _Player::Open(char * fileName)
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

		void _Player::Pause()
		{
			_closeTimer();
			_playState = PAUSED;
		}

		void _Player::Close()
		{
			_closeTimer();
			if (_input)
				delete _input;
			if (_videoDecoder)
				delete _videoDecoder;
			_input = nullptr;
			_videoDecoder = nullptr;
			_playState = IDLE;
		}

		PLAY_STATE _Player::GetPlayState() const
		{
			return _playState;
		}
		int64_t _Player::GetCurrentFrame() const
		{
			return _currentFrame;
		}
		int64_t _Player::GetFramesCount() const
		{
			if (_playState != IDLE && _videoDecoder)
				return _videoDecoder->FrameCount;
			return 0;
		}

		IDirect3DSurface9* _Player::GetDXBackBufferNoRef()
		{
			if (!_RenderManager) DirectXRendererManager::Create(&_RenderManager);
			return nullptr;

		}

#pragma endregion Umanaged code

#pragma region Managed code
		Player::Player()
		{
			_player = new _Player();
		}

		Player::~Player()
		{
			delete _player;
		}

		void Player::Open(String^ fileName)
		{
			_fileName = fileName;
			char* fn = (char*)Marshal::StringToHGlobalAnsi(fileName).ToPointer();
			_player->Open(fn);
			Marshal::FreeHGlobal(IntPtr((void*)fn));
		}

		void Player::Play()
		{
			_player->Play();
		}

		IntPtr Player::GetDXBackBufferNoRef()
		{
			return (IntPtr)_player->GetDXBackBufferNoRef();
		}

#pragma endregion Managed code

	}
}