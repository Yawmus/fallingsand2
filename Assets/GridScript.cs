using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GridScript : MonoBehaviour
{

    public int WIDTH = 20, HEIGHT = 20;
    public List<int> ids;
    float tick = 0;

    public Dictionary<int, ParticleData> particleData;
    public ComputeShader shader;
    Texture2D tex;
    Color[] bkgPixels, drawPixels;

    public struct ParticleData
    {
        public int id;
        public Vector3 position;
        public Color color;
    }

    public const float P_FPS = 5;
    public const float S_FPS = 30;
    public const float SPAWN_RATE = 1f;


    // Use this for initialization
    void Start()
    {
        ids = new List<int>();
        particleData = new Dictionary<int, ParticleData>();

        drawPixels = new Color[WIDTH * HEIGHT];
        bkgPixels = new Color[WIDTH * HEIGHT];
        for (int i = 0; i < WIDTH * HEIGHT; i++)
        {
            bkgPixels[i] = new Color(0, 0, 0, 1);
        }

        tex = new Texture2D(WIDTH, HEIGHT);
        tex.filterMode = FilterMode.Point;
        GetComponent<Renderer>().material.mainTexture = tex;

        shader.SetInt("width", WIDTH);
        shader.SetInt("height", HEIGHT);
    }
    IEnumerator SpawnParticles()
    {
        while (enabled)
        {
            yield return new WaitForSeconds(1 / SPAWN_RATE);
            AddParticle(UnityEngine.Random.Range(0, WIDTH), HEIGHT);

        }
    }
    void AddParticle(int x, int y)
    {
        int id = UnityEngine.Random.Range(1, int.MaxValue);
        Vector3 position = new Vector3(x, y, 0);
        Color color = new Color(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f), 1);
        particleData.Add(id, new ParticleData { id = id, position = position, color = color });
        ids.Add(id);
    }
    void RemoveParticle(int id)
    {
        particleData.Remove(id);
        ids.Remove(id);
    }

    bool frame = false;

    // Update is called once per frame
    void Update()
    {
        tick += (Time.unscaledDeltaTime - Time.deltaTime) * .1f;

        if (!frame)
            {
            frame = true;
            Debug.Log("First");
            SimulatePhysics();
            Debug.Log("Second");
            UpdateScreen();
        }
    }

    async Task SimulatePhysics()
    {
        if (ids.Count == 0)
        {
            return;
        }

        ParticleData[] particleOutput = await ApplyGravity();
        Debug.Log("Third");

        // Collision
        for (int i = 0; i < particleOutput.Length; i++)
        {
            ParticleData data = particleOutput[i];
            ParticleData p = particleData[data.id];
            if (data.position.y < 0)
            {
                    RemoveParticle(data.id);
            }
            else
            {
                particleData.Remove(data.id);
                particleData[data.id] = new ParticleData { id = data.id, position = data.position, color = data.color };
            }
        }
    }

    async Task<ParticleData[]> ApplyGravity()
    {
        ParticleData[] particleOutput = new ParticleData[particleData.Count];
        ComputeBuffer particleBuffer = new ComputeBuffer(particleData.Count, 32);

        int kernel2 = shader.FindKernel("Move");

        particleBuffer.SetData(particleData.Values.ToList());
        shader.SetBuffer(kernel2, "particleBuffer", particleBuffer);
        shader.Dispatch(kernel2, particleData.Count, 1, 1);
        particleBuffer.GetData(particleOutput);

        particleBuffer.Dispose();

        return particleOutput;
    }

    async Task UpdateScreen()
    {
        shader.SetInt("width", WIDTH);
        shader.SetInt("height", HEIGHT);

        await DrawBkg();

        if (ids.Count == 0)
        {
            tex.SetPixels(drawPixels);
            tex.Apply();
            return;
        }

        await DrawParticles();
    }

    async Task DrawBkg()
    {
        ComputeBuffer bkgColor = new ComputeBuffer(bkgPixels.Length, 16);
        //Color[] frameOutput = new Color[bkgPixels.Length];

        int kernel = shader.FindKernel("DrawBkg");

        shader.SetBuffer(kernel, "frameColor", bkgColor);
        shader.Dispatch(kernel, bkgPixels.Length, 1, 1);
        bkgColor.GetData(drawPixels);
        bkgColor.Dispose();
    }

    async Task DrawParticles()
    {
        // Draw particles
        ComputeBuffer frameBuffer = new ComputeBuffer(drawPixels.Length, 16);
        ComputeBuffer particleBuffer = new ComputeBuffer(particleData.Count, 32);

        int kernel3 = shader.FindKernel("DrawParticle");

        particleBuffer.SetData(particleData.Values.ToList());
        frameBuffer.SetData(drawPixels);

        shader.SetBuffer(kernel3, "particleBuffer", particleBuffer);
        shader.SetBuffer(kernel3, "frameColor", frameBuffer);
        shader.Dispatch(kernel3, particleData.Count, 1, 1);

        frameBuffer.GetData(drawPixels);
        tex.SetPixels(drawPixels);
        tex.Apply();

        particleBuffer.Dispose();
        frameBuffer.Dispose();
    }

}