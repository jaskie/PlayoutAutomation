#pragma once
#include "Stdafx.h"
#include "AVFrameRenderer.h"

AVFrameRenderer::AVFrameRenderer(): DirectXRenderer()
{
}


HRESULT AVFrameRenderer::Create(IDirect3D9 * pD3D, IDirect3D9Ex * pD3DEx, HWND hwnd, UINT uAdapter, DirectXRenderer ** ppRenderer)
{
	HRESULT hr = S_OK;

	AVFrameRenderer *pRenderer = new AVFrameRenderer();
	IFCOOM(pRenderer);

	IFC(pRenderer->Init(pD3D, pD3DEx, hwnd, uAdapter));

	*ppRenderer = pRenderer;
	pRenderer = NULL;

Cleanup:
	delete pRenderer;

	return hr;
}

AVFrameRenderer::~AVFrameRenderer()
{
}

HRESULT AVFrameRenderer::Render(AVFrame * const frame)
{
	HRESULT hr = S_OK;

	IFC(m_pd3dDevice->BeginScene());
	IFC(m_pd3dDevice->Clear(
		0,
		NULL,
		D3DCLEAR_TARGET,
		D3DCOLOR_ARGB(0xFF, 0, 0, 0x7F),  // NOTE: Premultiplied alpha!
		1.0f,
		0
		));
	if (m_pd3dRTS)
	{
		D3DLOCKED_RECT * surfacePixels;
		if (m_pd3dRTS->LockRect(surfacePixels, NULL, 0) == D3D_OK)
		{
			FillMemory(surfacePixels->pBits, 1000, 0);
			m_pd3dRTS->UnlockRect();
		}
	}

	//IFC(m_pd3dDevice->DrawPrimitive(D3DPT_TRIANGLELIST, 0, 1));

	IFC(m_pd3dDevice->EndScene());

Cleanup:
	return hr;
}

HRESULT AVFrameRenderer::CreateSurface(UINT uWidth, UINT uHeight, bool fUseAlpha, UINT m_uNumSamples)
{
	return DirectXRenderer::CreateSurface(uWidth, uHeight, fUseAlpha, m_uNumSamples);
}

HRESULT AVFrameRenderer::Init(IDirect3D9 * pD3D, IDirect3D9Ex * pD3DEx, HWND hwnd, UINT uAdapter)
{
	HRESULT hr = S_OK;
	// Call base to create the device and render target
	IFC(DirectXRenderer::Init(pD3D, pD3DEx, hwnd, uAdapter));

	// Set up the VB
Cleanup:
	return hr;
}
