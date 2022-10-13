using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameOptionsUtility;
using GameOptionsUtility.HDRP;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class BlittingScript : MonoBehaviour
{
	private CommandBuffer CmdRunPP;

	private Camera PresentCamera;			// Camera that presents the final image to the screen
	private Camera RenderCamera;			// Camera that uses the offscreen render target for main rendering

	private RenderTexture MainBackBuffer;	// Back Buffer of the RenderCamera that was set by the game from Unity GUI
	private RenderTexture FixedSizeRT;		// The replacement for the RenderCamera target texture of the requested offscreen size

	public int OffscreenWidth = 0; // Width of the offscreen RT size
	public int OffscreenHeight = 0;	 // Width of the offscreen RT size
	private SpaceshipOptions.UpsamplingMethod UpsamplingMethod = SpaceshipOptions.UpsamplingMethod.DLSS;		// Upsampling method that comes from the command line
	private float ScreenPercentage = 50;

	private bool FinalBlit = true;			// Should we do the final blitting from the RenderCamera to PresentCamera. If not - we save some perf, but we don't see what is rendered.

	public static string[] UpsamplingConstants = {
		"CatmullRom",
		"CAS",
		"TAAU",
		"FSR",
		"DLSS"
	};
	public static string[] QualityConstants = {
		"low",
		"high",
		"ultra"
	};
	public static string[] AAConstants = {
		"none",
		"FXAA",
		"TAA",
		"SMAA"
	};
	// Start is called before the first frame update
	void Start()
	{
		bool ShouldApplySettings = false;
		string[] Args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < Args.Length; i++)
		{
			string LowerArgs = Args[i].ToLower();
			switch (LowerArgs)
			{
				case "-benchmark":
				{
					QualitySettings.vSyncCount = 0;
					break;
				}
				case "-offscreenres":
				{
					i++;
					if (i == Args.Length)
						break;
					string NewRes = Args[i];

					switch (NewRes)
					{
						case "4K":
						case "2160p":
						{
							OffscreenWidth = 3840;
							OffscreenHeight = 2160;
							break;
						}
						case "2K":
						case "1440p":
						{
							OffscreenWidth = 2560;
							OffscreenHeight = 1440;
							break;
						}
						case "FHD":
						case "1080p":
						{
							OffscreenWidth = 1920;
							OffscreenHeight = 1080;
							break;
						}
						default:
						{
							string[] WH = NewRes.Split('x', 2, StringSplitOptions.RemoveEmptyEntries);
							if (WH.Length == 2)
							{
								if (!int.TryParse(WH[0], out OffscreenWidth))
									Debug.Log(String.Format("Unable to parse width: '{0}'", WH[0]));

								if (!int.TryParse(WH[1], out OffscreenHeight))
									Debug.Log(String.Format("Unable to parse height: '{0}'", WH[1]));
							}
							else
							{
								Debug.Log(String.Format(
									"Wrong parameter count, expected 2 (width and height), got: '{0}'", WH.Length));
							}

							break;
						}
					}

					break;
					}
				case "-upsamplingmethod":
				{
					i++;
					if (i == Args.Length)
						break;
					string UpsMethod = Args[i].ToLower();

					for (int s = 0; s <= (int)SpaceshipOptions.UpsamplingMethod.DLSS; s++)
					{
						if (UpsMethod == UpsamplingConstants[s].ToLower())
						{
							UpsamplingMethod = (SpaceshipOptions.UpsamplingMethod)s;
							break;
						}
					}
					ShouldApplySettings = true;
					GameOption.Get<SpaceshipOptions>().upsamplingMethod = UpsamplingMethod;
					break;
				}
				case "-quality":
				{
					i++;
					if (i == Args.Length)
						break;
					string QualityMode = Args[i].ToLower();

					for (int q = 0; q <= QualityConstants.Length; q++)
					{
						if (QualityMode == QualityConstants[q].ToLower())
						{
							QualitySettings.SetQualityLevel(q, true);
							break;
						}
					}
					break;
				}
				case "-aa":
				{
					i++;
					if (i == Args.Length)
						break;
					string AAMode = Args[i].ToLower();

					for (int aa = 0; aa <= AAConstants.Length; aa++)
					{
						if (AAMode == AAConstants[aa].ToLower())
						{
							QualitySettings.antiAliasing = aa;
							break;
						}
					}
					break;
				}
				case "-screenpercentage":
				{
					i++;
					if (i == Args.Length)
						break;
					string Perc = Args[i];
					float.TryParse(Perc, out ScreenPercentage);
					ShouldApplySettings = true;
					GameOption.Get<SpaceshipOptions>().screenPercentage = (int)SetDynamicResolutionScale();
					break;
				}

				case "-nofinalblit":
				{
					FinalBlit = false;
					break;
				}
			}
		}
		SetupCameras();
		SetupOnRun();
		
		if (ShouldApplySettings)
			GameOption.Get<SpaceshipOptions>().Apply();

		string DebugText = PresentCamera == null ? 
			String.Format("Scales from: {0}x{1}; Scales to unknown; Upsampling: {4}; screen percentage: {5}", 
				OffscreenWidth, 
				OffscreenHeight, 
				UpsamplingConstants[(int)UpsamplingMethod], 
				GameOption.Get<SpaceshipOptions>().screenPercentage) :
			String.Format("Scales from: {0}x{1}; Scales to {2}x{3}; Upsampling: {4}; screen percentage: {5}",
				OffscreenWidth,
				OffscreenHeight,
				PresentCamera.pixelWidth,
				PresentCamera.pixelHeight,
				UpsamplingConstants[(int)UpsamplingMethod],
				GameOption.Get<SpaceshipOptions>().screenPercentage);
		
		Debug.Log(DebugText);

		Debug.Log(String.Format("Current quality level: {0}", QualitySettings.names[QualitySettings.GetQualityLevel()]));

		PresentCamera.enabled = FinalBlit;
	}
	float SetDynamicResolutionScale()
	{
		return ScreenPercentage;
	}
	void OnDestroy()
	{
		if (RenderCamera != null && MainBackBuffer != null)
			RenderCamera.targetTexture = MainBackBuffer;
	}
	// Update is called once per frame
	private void OnEnable()
	{
		RenderPipelineManager.endCameraRendering += StartCameraRendering;
	}

	// Unity calls this method automatically when it disables this component
	private void OnDisable()
	{
		RenderPipelineManager.endCameraRendering -= StartCameraRendering;
		if (RenderCamera != null && MainBackBuffer != null)
			RenderCamera.targetTexture = MainBackBuffer;
	}
	void StartCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		CheckCameras(context, camera);
	}

	void CheckCameras(ScriptableRenderContext context, Camera camera)
	{
		if (FinalBlit && camera != RenderCamera)
		{
			CmdRunPP.Clear();
			RenderTargetIdentifier id = new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive);
			CmdRunPP.Blit(FixedSizeRT, id);
			context.ExecuteCommandBuffer(CmdRunPP);
			context.Submit();
		}

		if (camera == RenderCamera)
		{
			if (FixedSizeRT != null && camera.targetTexture != FixedSizeRT)
				camera.targetTexture = FixedSizeRT;
			
		}
	}
	private void SetupCameras()
	{
		RenderTexture[] RTs = Resources.FindObjectsOfTypeAll(typeof(RenderTexture)) as RenderTexture[];
		foreach (RenderTexture rt in RTs)
		{
			if (rt.name.StartsWith("BackBuffer Main"))
			{
				MainBackBuffer = rt;
				Debug.Log(String.Format("Found buffer: '{0}' : {1}x{2}", rt.name, rt.width, rt.height));
			}
		}

		Camera[] cams = Resources.FindObjectsOfTypeAll(typeof(Camera)) as Camera[];

		Debug.Log(String.Format("----- Total camera count: '{0}' -----", cams.Length));
		foreach (Camera c in cams)
		{
			if (c.name == "PresentCamera")
			{
				PresentCamera = c;
				PresentCamera.allowMSAA = false;
				PresentCamera.clearFlags = CameraClearFlags.Nothing;
				PresentCamera.cullingMask = 1 << 5;
				PresentCamera.enabled = true;
				GameObject.DontDestroyOnLoad(PresentCamera);
				Debug.Log(String.Format("Presentation camera: '{0}'", c.name));
			}
			else if (c.targetTexture == MainBackBuffer && c.isActiveAndEnabled)
			{
				RenderCamera = c;
				Debug.Log(String.Format("RenderCamera: '{0}'", c.name));
			}
			else
				Debug.Log(String.Format("Camera: '{0}'", c.name));
		}

		CmdRunPP = new CommandBuffer();
		CmdRunPP.name = "Fixed size blitter";
	}

	void SetupOnRun()
	{
		RenderCamera.depth = PresentCamera.depth - 1;

		if (OffscreenWidth == 0)
			OffscreenWidth = MainBackBuffer != null ? MainBackBuffer.width : RenderCamera.pixelWidth;
		if (OffscreenHeight == 0)
			OffscreenHeight = MainBackBuffer != null ? MainBackBuffer.height : RenderCamera.pixelHeight;

		RenderTextureDescriptor Desc = MainBackBuffer != null ? MainBackBuffer.descriptor : new RenderTextureDescriptor(OffscreenWidth, OffscreenHeight, RenderTextureFormat.ARGB32);
		if (FixedSizeRT != null)
		{
			FixedSizeRT.Release();
		}

		Desc.width = OffscreenWidth;
		Desc.height = OffscreenHeight;
		FixedSizeRT = new RenderTexture(Desc);
		FixedSizeRT.name = "FixedSizeRT";
		FixedSizeRT.Create();
		RenderCamera.targetTexture = FixedSizeRT;
	}
}
