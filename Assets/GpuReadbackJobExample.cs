using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class GpuReadbackJobExample : MonoBehaviour
{
	[SerializeField]
	RenderTexture m_FlatTexture;
	
	WebCamTexture m_CamTexture;

	[SerializeField] Renderer m_SecondRenderer;

	AsyncGPUReadbackRequest m_ReadbackRequest;

	JobHandle m_PostReadbackJobHandle;
	
	NativeArray<Color32> m_ReadbackData;

	Texture2D m_LoadBuffer;

	
	void Start ()
	{
		m_LoadBuffer = new Texture2D(1280, 720);
		m_FlatTexture = new RenderTexture(1280, 720, 1);
		m_FlatTexture.enableRandomWrite = true;
		
		m_CamTexture = new WebCamTexture(1280, 720);
		GetComponent<Renderer>().material.mainTexture = m_FlatTexture;
		m_SecondRenderer.material.mainTexture = m_LoadBuffer;
		m_CamTexture.Play();
		
		Graphics.Blit(m_CamTexture, m_FlatTexture);
		m_ReadbackRequest = AsyncGPUReadback.Request(m_FlatTexture, 0, request =>
		{
			m_ReadbackData = request.GetData<Color32>();
			Debug.Log("first readback done, texture data length: " + m_ReadbackData.Length);
		});
	}

	void Update ()
	{
		Graphics.Blit(m_CamTexture, m_FlatTexture);
		if (!m_PostReadbackJobHandle.IsCompleted)
		{
			m_PostReadbackJobHandle.Complete();
			//m_LoadBuffer.LoadRawTextureData(m_ReadbackData);
			//m_LoadBuffer.Apply();
		}
		
		if (!m_ReadbackRequest.done)
			return;

		if (m_ReadbackRequest.hasError)
		{
			Debug.LogError("error during readback ??");
		}

		m_ReadbackRequest = AsyncGPUReadback.Request(m_FlatTexture, 0, request =>
		{
			m_ReadbackData = request.GetData<Color32>();
			var job = new PostReadJob()
			{
				pixels = m_ReadbackData
			};

			m_PostReadbackJobHandle = job.Schedule(m_ReadbackData.Length, 1024);
		});
	}

	[BurstCompile]
	struct PostReadJob : IJobParallelFor
	{
		public NativeArray<Color32> pixels;
		
		public void Execute(int index)
		{
			var pixel = pixels[index];
			if (pixel.g > 0x069)
			{
				pixel.g = 0;
			}
		}
	}
}
