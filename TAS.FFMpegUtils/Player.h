#pragma once
#include "stdafx.h"
#include "Input.h"
#include "VideoDecoder.h"
#include "FrameEventArgs.h"

namespace TAS {
	namespace FFMpegUtils {


		public enum PLAY_STATE
		{
			IDLE,
			PLAYING,
			PAUSED,
		};

		typedef void(__stdcall *tickProc)(const int64_t);
#pragma region Unmanaged code
		void CALLBACK FrameTickTimerCallback(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);

		class _Player
		{
		private:
			int _width;
			int _height;
			Input * _input;
			VideoDecoder * _videoDecoder;
			PLAY_STATE _playState;
			AVFrame * _lastFrame = NULL;
			HRESULT _ensureRenderManager();
			void _closeTimer();
		public:
			_Player();
			~_Player();
			void SetSize(int width, int height);
			void Play();
			void Seek(int64_t frame);
			void Pause();
			void Open(char* fileName);
			void Close();
			PLAY_STATE GetPlayState() const;
			int64_t GetFramesCount() const;
			// should be internal
			tickProc _frameTickTimerProc;
			int _frameTickTimerId;
			int64_t _currentFrame;
		};
#pragma endregion Unmanaged code

#pragma region Managed code

		using namespace System;
		using namespace System::Runtime::InteropServices;
		
		public delegate void TickDelegate(const int64_t);
		public ref class Player
		{
		public:
			Player();
			~Player();
			void Open(String ^ fileName);
			void Play();
			event System::EventHandler<FrameEventArgs^> ^ TimerTick;
			property int64_t FrameCount { int64_t get(); }
			void SetSize(int width, int height);
		private:
			String^ _fileName;
			_Player * _player;
			GCHandle _frameTickTimerProcHandle; //this handle prevents method relocation on GC
			void _frameTickTimerProc(const int64_t frameNumber);
		};
#pragma endregion Managed code
	}
}