#pragma once
class AVFrameRenderer: DirectXRenderer
{
public:
	static HRESULT Create(IDirect3D9 *pD3D, IDirect3D9Ex *pD3DEx, HWND hwnd, UINT uAdapter, DirectXRenderer **ppRenderer);
	~AVFrameRenderer();
	HRESULT Render();
protected:
	HRESULT Init(IDirect3D9 *pD3D, IDirect3D9Ex *pD3DEx, HWND hwnd, UINT uAdapter);

private:
	AVFrameRenderer();

};

