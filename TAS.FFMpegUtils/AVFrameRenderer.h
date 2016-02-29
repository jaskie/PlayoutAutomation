#pragma once
#include "stdafx.h"

class AVFrameRenderer: DirectXRenderer
{
public:
	static HRESULT Create(IDirect3D9 *pD3D, IDirect3D9Ex *pD3DEx, HWND hwnd, UINT uAdapter, DirectXRenderer **ppRenderer);
	~AVFrameRenderer();
	HRESULT Render(AVFrame * const frame);
	virtual HRESULT CreateSurface(UINT uWidth, UINT uHeight, bool fUseAlpha, UINT m_uNumSamples);

protected:
	HRESULT Init(IDirect3D9 *pD3D, IDirect3D9Ex *pD3DEx, HWND hwnd, UINT uAdapter);

private:
	AVFrameRenderer();
	SwsContext * pSWSContext;
};

