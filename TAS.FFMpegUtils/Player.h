#pragma once
#include "stdafx.h"
#include "Input.h"
#include "VideoDecoder.h"
#include "DirectXRendererManager.h"

namespace TAS {
	namespace FFMpegUtils {


		public enum PLAY_STATE
		{
			IDLE,
			PLAYING,
			PAUSED,
		};

		typedef void(__stdcall *tickProc)();
#pragma region Unmanaged code

		void CALLBACK TimerCallback(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);

		class _Player
		{
		private:
			int _width;
			int _height;
			HDC _device;
			Input * _input;
			VideoDecoder * _videoDecoder;
			PLAY_STATE _playState;
			int64_t _currentFrame;
			DirectXRendererManager *_RenderManager = NULL;
			void _closeTimer();
		public:
			_Player();
			~_Player();
			void SetVideoDevice(HDC device, int width, int height);
			void Play();
			void Seek(int64_t frame);
			void Pause();
			void Open(char* fileName);
			void Close();
			PLAY_STATE GetPlayState() const;
			int64_t GetCurrentFrame() const;
			int64_t GetFramesCount() const;
			IDirect3DSurface9* GetDXBackBufferNoRef();
			// should be internal
			tickProc _timerTickProc;
			int _timerId;
		};
#pragma endregion Unmanaged code

#pragma region Managed code
using namespace System;
using namespace System::Runtime::InteropServices;

		public ref class Player
		{
		public:
			Player();
			~Player();
			void Open(String ^ fileName);
			void Play();
			event System::EventHandler ^ TimerTick;
			IntPtr GetDXBackBufferNoRef();
		private:
			String^ _fileName;
			_Player * _player;
			void _timerTickProc();
		};
#pragma endregion Managed code
	}
}