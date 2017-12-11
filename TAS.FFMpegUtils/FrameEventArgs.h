#pragma once
#pragma managed
using namespace System;

public ref class FrameEventArgs :
	public EventArgs
{
	const int64_t _frameNumber;
public:
	FrameEventArgs(int64_t frameNumber): _frameNumber(frameNumber)	{}
	property int64_t FrameNumber
	{
		int64_t get() { return _frameNumber; }
	}

};

