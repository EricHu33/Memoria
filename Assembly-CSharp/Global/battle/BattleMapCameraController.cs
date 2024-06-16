using Memoria.Prime;
using Memoria.Scripts;
using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleMapCameraController : MonoBehaviour
{
	public void SetDefaultPsxCamera0()
	{
		this.mainCam.transform.position = new Vector3(-2200f, 2900f, 4600f);
		this.mainCam.transform.rotation = Quaternion.Euler(new Vector3(25f, 155f, 0f));
		this.mainCam.transform.localScale = new Vector3(1f, -1f, 1f);
		this.mainCam.nearClipPlane = 400f;
		this.mainCam.farClipPlane = 16383f;
	}

	public void SetDefaultPsxCamera1()
	{
		this.mainCam.transform.position = new Vector3(-2100f, 900f, -4500f);
		this.mainCam.transform.rotation = Quaternion.Euler(new Vector3(3f, 30f, 0f));
		this.mainCam.transform.localScale = new Vector3(1f, -1f, 1f);
		this.mainCam.nearClipPlane = 360f;
		this.mainCam.farClipPlane = 16383f;
	}

	public void SetDefaultPsxCamera2()
	{
		this.mainCam.transform.position = new Vector3(0f, 1000f, -4500f);
		this.mainCam.transform.rotation = Quaternion.Euler(new Vector3(10f, 0f, 0f));
		this.mainCam.transform.localScale = new Vector3(1f, -1f, 1f);
		this.mainCam.nearClipPlane = 300f;
		this.mainCam.farClipPlane = 16383f;
	}

	private void SetDefaultCamera(Int32 camID)
	{
		switch (camID)
		{
		case 0:
			this.SetDefaultPsxCamera0();
			break;
		case 1:
			this.SetDefaultPsxCamera1();
			break;
		case 2:
			this.SetDefaultPsxCamera2();
			break;
		default:
			global::Debug.Log("Default Camera ID " + camID + " not found");
			break;
		}
	}

	public void SetNextDefaultCamera()
	{
		this.defaultCamID++;
		if (this.defaultCamID >= 3)
		{
			this.defaultCamID = 0;
		}
		this.SetDefaultCamera(this.defaultCamID);
	}

	public void SetPrevDefaultCamera()
	{
		this.defaultCamID--;
		if (this.defaultCamID < 0)
		{
			this.defaultCamID = 2;
		}
		this.SetDefaultCamera(this.defaultCamID);
	}

	public Int32 GetCurrDefaultCamID()
	{
		return this.defaultCamID;
	}

    private Material _postEffectMat;
    private RenderTexture m_dummyRT;
    private RenderTexture m_dummyRT2;
    private Matrix4x4 viewMatrix;
    private Matrix4x4 projectionMatrix;
    private Material copyDepthMat;
	private void Awake()
	{
		this.defaultCamID = UnityEngine.Random.Range(0, 3);
		this.psxCam = new PsxCamera();
		this.mainCam = base.GetComponent<Camera>();
		this.rainRenderer = base.GetComponent<BattleRainRenderer>();
		this.SetDefaultCamera(this.defaultCamID);
        this._postEffectMat = new Material(ShadersLoader.Find("PSX/PostEffect"));
        this.copyDepthMat = new Material(ShadersLoader.Find("PSX/CopyGlobalDepth"));
        if (m_dummyRT == null)
            m_dummyRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGBFloat);
        if (m_dummyRT2 == null)
            m_dummyRT2 = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGBFloat);
        string[] formats = System.Enum.GetNames (typeof(RenderTextureFormat));
        for (int i = 0; i < formats.Length; i++)
        {
            var format = (RenderTextureFormat)Enum.Parse(typeof(RenderTextureFormat), formats[i]);
            CheckSupport(format);
        }
    }
    private void CheckSupport(RenderTextureFormat format)
    {
        Log.Message(format + "support status  = "+SystemInfo.SupportsRenderTextureFormat(format));
    }
	private void Update()
	{
        var originFar = this.mainCam.farClipPlane;
        var originNear = this.mainCam.nearClipPlane; 
        var originRT = this.mainCam.targetTexture != null ? this.mainCam.targetTexture : null;
        
        this.mainCam.nearClipPlane = 0.01f;
        this.mainCam.farClipPlane = 300;
        this.mainCam.targetTexture = m_dummyRT;
        this.mainCam.Render();
        
        this.mainCam.targetTexture = originRT;
        viewMatrix = this.mainCam.worldToCameraMatrix;
        projectionMatrix =  GL.GetGPUProjectionMatrix(this.mainCam.projectionMatrix, false);
        
        this.mainCam.farClipPlane = originFar;
        this.mainCam.nearClipPlane = originNear;
        //this.mainCam.forceIntoRenderTexture = false;
	}

	private void LateUpdate()
	{
		SFX.LateUpdatePlugin();
	}

    private void OnPreRender()
    {
        if (this.mainCam.targetTexture == m_dummyRT)
        {
            this.mainCam.depthTextureMode = DepthTextureMode.Depth;
        }
        else
        {
            this.mainCam.depthTextureMode = DepthTextureMode.DepthNormals;
        }
    }

    private void OnPostRender()
	{
        if (this.mainCam.targetTexture == m_dummyRT)
        {
            return;
        }
		this.rainRenderer.nf_BbgRain();
		SFX.PostRender();
		UnifiedBattleSequencer.LoopRender();
	}
    

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
        if (dest == m_dummyRT)
        {
            Graphics.Blit(src, dest, copyDepthMat);
            return;
        }
       // PSXTextureMgr.PostBlur(src, dest);
        
        // Combine the view and projection matrices
        Matrix4x4 viewProjectionMatrix = projectionMatrix * viewMatrix;

        // Invert the combined matrix to get the inverse view projection matrix
        Matrix4x4 inverseViewProjectionMatrix = viewProjectionMatrix.inverse;
        //_postEffectMat.SetMatrix("_InvVP", inverseViewProjectionMatrix);
        _postEffectMat.SetMatrix("_InvP",  projectionMatrix.inverse);
       // _postEffectMat.SetTexture("_BlueNoise", _blueNoise);
        _postEffectMat.SetFloat("_aoStrength", 3.5f);
        _postEffectMat.SetFloat("_aoPower", 2);
        _postEffectMat.SetFloat("_radius", 100);
        _postEffectMat.SetFloat("_bias", 0.05f);
        _postEffectMat.SetFloat("_debug", 1);
        _postEffectMat.SetTexture("_CustomViewZMap", m_dummyRT);
        RenderTexture tempRT = RenderTexture.GetTemporary(src.width, src.height);
        PSXTextureMgr.PostBlur(src, dest);
        Graphics.Blit(dest, tempRT);
        Graphics.Blit(tempRT, dest, _postEffectMat);
        RenderTexture.ReleaseTemporary(tempRT);
	}

	private void OnGUI()
	{
		SFX.DebugOnGUI();
	}

	private const Int32 numDefaultCam = 3;

	public Camera mainCam;

	public PsxCamera psxCam;

	private Int32 defaultCamID;

	private BattleRainRenderer rainRenderer;
}
