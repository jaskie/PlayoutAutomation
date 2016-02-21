#include "stdafx.h"
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

HRESULT AVFrameRenderer::Render()
{
	return E_NOTIMPL;
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
