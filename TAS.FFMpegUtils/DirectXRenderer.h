#pragma once
class DirectXRenderer
{
protected:
	DirectXRenderer();

	virtual HRESULT Init(IDirect3D9 *pD3D, IDirect3D9Ex *pD3DEx, HWND hwnd, UINT uAdapter);

	IDirect3DDevice9   *m_pd3dDevice;
	IDirect3DDevice9Ex *m_pd3dDeviceEx;

	IDirect3DSurface9 *m_pd3dRTS;
public:
	virtual ~DirectXRenderer();
	HRESULT CheckDeviceState();
	virtual HRESULT CreateSurface(UINT uWidth, UINT uHeight, bool fUseAlpha, UINT m_uNumSamples);

	virtual HRESULT Render(AVFrame * const frame) = 0;
	
	IDirect3DSurface9 *GetSurfaceNoRef() { return m_pd3dRTS; }
};

