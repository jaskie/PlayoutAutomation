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
		
		void CALLBACK FrameTickTimerCallback(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2)
		{
			_Player* player = (_Player*)dwUser;
			if (player->_frameTickTimerId == uTimerID)
			{
				player->_currentFrame++;
				player->_frameTickTimerProc(player->_currentFrame);
			}
		}

		HRESULT _Player::_ensureRenderManager()
		{
			if (!_renderManager)
			{
				if (FAILED(DirectXRendererManager::Create(&_renderManager)))
					return NULL;
			}
			return S_OK;
		}

		void _Player::_closeTimer()
		{
			if (_frameTickTimerId)
			{
				timeKillEvent(_frameTickTimerId);
				_frameTickTimerId = 0;
			}

		}

		_Player::_Player() :
			  _currentFrame(0L)
			, _playState(IDLE)
		{
			av_register_all();
			_frameTickTimerId = NULL;
		}

		_Player::~_Player()
		{
			Close();
		}

		void _Player::SetSize(int width, int height)
		{
			_width = width;
			_height = height;
			if (_ensureRenderManager() == S_OK)
				_renderManager->SetSize(_width, _height);
		}

		void _Player::Play()
		{
			_playState = PLAYING;
			_frameTickTimerId = timeSetEvent(40, 0, FrameTickTimerCallback, (DWORD_PTR)this, TIME_PERIODIC);
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
					_lastFrame = _videoDecoder->DecodeNextFrame();
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
		int64_t _Player::GetFramesCount() const
		{
			if (_playState != IDLE && _videoDecoder)
				return _videoDecoder->FrameCount;
			return 0;
		}

		IDirect3DSurface9* _Player::GetDXBackBufferNoRef()
		{
			if (FAILED(_ensureRenderManager()))
				return nullptr;
			IDirect3DSurface9* result;
			if (FAILED(_renderManager->GetBackBufferNoRef(&result)))
				return nullptr;
			if (FAILED(_renderManager->Render(_lastFrame)))
				return nullptr;
			return result;
		}

#pragma endregion Umanaged code

#pragma region Managed code
		static void tproc() {

		}

		Player::Player()
		{
			_player = new _Player();
			// providing timer tick callback to unmanaged class
			TickDelegate^ timerTickAction = gcnew TickDelegate(this, &Player::_frameTickTimerProc);
			_frameTickTimerProcHandle = GCHandle::Alloc(timerTickAction);
			IntPtr _timerTickPointer = Marshal::GetFunctionPointerForDelegate(timerTickAction);
			_player->_frameTickTimerProc = static_cast<tickProc>(_timerTickPointer.ToPointer());
		}

		Player::~Player()
		{
			delete _player;
			_frameTickTimerProcHandle.Free();
		}

		void Player::Open(String^ fileName)
		{
			_fileName = fileName;
			char* fn = (char*)Marshal::StringToHGlobalAnsi(fileName).ToPointer();
			_player->Open(fn);
			Marshal::FreeHGlobal(IntPtr((void*)fn));
		}

		void Player::_frameTickTimerProc(const int64_t frameNumber)
		{
			TimerTick(this, gcnew FrameEventArgs(frameNumber));
		}

		void Player::Play()
		{
			_player->Play();
		}

		IntPtr Player::GetDXBackBufferNoRef()
		{
			return (IntPtr)_player->GetDXBackBufferNoRef();
		}

		int64_t Player::FrameCount::get() { return _player->GetFramesCount(); }

		void Player::SetSize(int width, int height) 
		{
			_player->SetSize(width, height);
		}


#pragma endregion Managed code

	}
}