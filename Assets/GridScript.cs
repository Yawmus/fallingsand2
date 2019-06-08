using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GridScript : MonoBehaviour
{

    public int WIDTH = 20, HEIGHT = 20;

    // Garbage collection
    int DISPOSE_HEIGHT = 3, TOTAL_HEIGHT;

    public List<int> ids;
    float tick = 0;

    public Dictionary<int, ParticleData> particleData;
    public ComputeShader shader;
    Texture2D tex;
    Color[] bkgPixels, drawPixels;

    public enum PARTICLE_TYPE
    {
        SAND,
        WALL,
        WATER
    }

    public struct ParticleData
    {
        public int id;
        public Vector3 position;
        public Color color;
        public int properties;
    }

    public struct ParticleProperties
    {
        public Color color;
        public int properties;
    }

    public const float P_FPS = 40;
    public const float S_FPS = 40;
    public const float SPAWN_RATE = 50;
    public const float GARBAGE_COLLECTION_RATE = 40;


    // Use this for initialization
    void Start()
    {
        TOTAL_HEIGHT = HEIGHT + DISPOSE_HEIGHT;
        ids = new List<int>();
        particleData = new Dictionary<int, ParticleData>();

        drawPixels = new Color[WIDTH * HEIGHT];
        bkgPixels = new Color[WIDTH * HEIGHT];

        for (int i = 0; i < WIDTH * HEIGHT; i++)
        {
            bkgPixels[i] = new Color(0, 0, 0, 1);
        }

        DrawTestEnv("1");


        //AddParticle(PARTICLE_TYPE.SAND, 0, 10);

        tex = new Texture2D(WIDTH, HEIGHT);
        tex.filterMode = FilterMode.Point;
        GetComponent<Renderer>().material.mainTexture = tex;

        shader.SetInt("width", WIDTH);
        shader.SetInt("height", HEIGHT);
        shader.SetInt("colGridWidth", WIDTH + 2);
        shader.SetInt("colGridHeight", HEIGHT + 1);
        InvokeRepeating("SpawnParticles", 0f, 1/SPAWN_RATE);
        InvokeRepeating("SimulatePhysics", 0f, 1/P_FPS);
        InvokeRepeating("UpdateScreen", 0f, 1/S_FPS);
    }
    void SpawnParticles()
    {
        SpawnTestEnv("1");
        //AddParticle(GetRandom(), UnityEngine.Random.Range(0, WIDTH), HEIGHT - 1);
    }

    void AddParticle(PARTICLE_TYPE pt, int x, int y)
    {
        int id = UnityEngine.Random.Range(1, int.MaxValue);
        Vector3 position = new Vector3(x, y, 0);
        ParticleProperties pp = GetProperties(pt);
        ParticleData data = new ParticleData
        {
            id = id,
            position = position,
            color = pp.color,
            properties = pp.properties
        };

        particleData.Add(id, data);
        ids.Add(id);
    }

    void RemoveParticle(int x, int y)
    {
        int id = 0;
        bool found = false;
        foreach(ParticleData p in particleData.Values)
        {
            if((int) p.position.x == x && (int) p.position.y == y)
            {
                id = p.id;
                found = true;
                break;
            }
        }
        if (!found)
        {
            Debug.Log("Couldn't find particle at x:" + x + ", y:" + y);
            return;
        }
        RemoveParticle(id);
    }

    void RemoveParticle(int id)
    {
        particleData.Remove(id);
        ids.Remove(id);
    }

    // Update is called once per frame
    async void Update()
    {
        tick += Time.deltaTime;
    }

    async Task SimulatePhysics()
    {
        if (ids.Count == 0)
        {
            return;
        }

        //Debug.Log("1");
        int[] gridData = await CreateGrid();
        //Debug.Log("3");
        ParticleData[] particleOutput = await ApplyPhysics(gridData);
        //Debug.Log("6");

        // Updating positions and removing garbage
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
                particleData[data.id] = new ParticleData
                {
                    id = data.id,
                    position = data.position,
                    color = data.color,
                    properties = data.properties
                };
            }
        }
    }

    // Used for collision detection
    async Task<int[]> CreateGrid()
    {
        // Grid properties:
        // blocked + direction
        // 1 + 2
        int colGridWidth = (WIDTH + 2);
        int colGridHeight = (HEIGHT + 1);
        ComputeBuffer inputGrid = new ComputeBuffer(colGridWidth * colGridHeight, 4);
        ComputeBuffer particleBuffer = new ComputeBuffer(particleData.Count, 36);
        int[] outputGrid = new int[colGridWidth * colGridHeight];

        int kernel2 = shader.FindKernel("CreateGrid");

        inputGrid.SetData(new int[colGridWidth * colGridHeight]);
        particleBuffer.SetData(particleData.Values.ToList());
        shader.SetBuffer(kernel2, "grid", inputGrid);
        shader.SetBuffer(kernel2, "particleBuffer", particleBuffer);

        shader.Dispatch(kernel2, particleData.Count, 1, 1);
        inputGrid.GetData(outputGrid);

        inputGrid.Dispose();
        particleBuffer.Dispose();

        //Debug.Log("2");
        return outputGrid;
    }

    async Task<ParticleData[]> ApplyPhysics(int[] gridData)
    {
        int colGridWidth = (WIDTH + 2);
        int colGridHeight = (HEIGHT + 1);
        //Debug.Log("4");
        ParticleData[] particleOutput = new ParticleData[particleData.Count];

        // Extra dimensions for padding
        ComputeBuffer inputGrid = new ComputeBuffer(colGridWidth * colGridHeight, 4);
        ComputeBuffer particleBuffer = new ComputeBuffer(particleData.Count, 36);

        int kernel2 = shader.FindKernel("Move");

        inputGrid.SetData(gridData);
        particleBuffer.SetData(particleData.Values.ToList());
        shader.SetBuffer(kernel2, "grid", inputGrid);
        shader.SetBuffer(kernel2, "particleBuffer", particleBuffer);
        shader.Dispatch(kernel2, particleData.Count, 1, 1);
        particleBuffer.GetData(particleOutput);
        //Debug.Log("5");

        particleBuffer.Dispose();

        return particleOutput;
    }

    async Task UpdateScreen()
    {
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
        ComputeBuffer particleBuffer = new ComputeBuffer(particleData.Count, 36);

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



    ParticleProperties GetProperties(PARTICLE_TYPE pt)
    {
        Color color = new Color(1, 0, 0, 1);

        // Grav + Liquid
        // 1 + 2 + 4 + 8
        int properties = 0;
        switch (pt)
        {
            case PARTICLE_TYPE.SAND:
                color = new Color(1, .99f, .86f, 1);
                properties = 1 + 0;
                break;
            case PARTICLE_TYPE.WATER:
                color = new Color(.1f, .1f, 1, 1);
                properties = 1 + 2;
                break;
            case PARTICLE_TYPE.WALL:
                color = new Color(1, 0, 0, 1);
                properties = 0 + 0;
                break;
        }
        return new ParticleProperties
        {
            color = color,
            properties = properties
        };
    }

    PARTICLE_TYPE GetRandom()
    {
        switch (UnityEngine.Random.Range(0, 2))
        {
            case 0:
                return PARTICLE_TYPE.SAND;
            case 1:
                return PARTICLE_TYPE.WATER;
        }
        return PARTICLE_TYPE.SAND;
    }

    void DrawTestEnv(string env)
    {
        switch (env)
        {
            case "1":
                for (int i = 0; i < WIDTH * HEIGHT; i++)
                {
                    int x = i % WIDTH;
                    int y = i / HEIGHT;
                    if (y == 30 && x > 10 && x < 20)
                    {
                        AddParticle(PARTICLE_TYPE.WALL, x, y);
                    }
                    if ((x == 10 || x == 20) && y > 30 && y < 40)
                    {
                        AddParticle(PARTICLE_TYPE.WALL, x, y);
                    }
                    if (y == 30 && x > 60 && x < 70)
                    {
                        AddParticle(PARTICLE_TYPE.WALL, x, y);
                    }

                    if ((x == 60 || x == 70) && y > 30 && y < 40)
                    {
                        AddParticle(PARTICLE_TYPE.WALL, x, y);
                    }
                }
                RemoveParticle(12, 30);

                // Diag top left -> bot right
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(PARTICLE_TYPE.WALL, 21 + i, 40 - i);
                }
                // Diag top right -> bot left
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(PARTICLE_TYPE.WALL, 42 - i, 40 - i);
                }

                for (int i = 0; i < 10; i++)
                {
                    AddParticle(PARTICLE_TYPE.WALL, 22 + i, 16 + i);
                }
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(PARTICLE_TYPE.WALL, 41 - i, 16 + i);
                }
                for (int i = 0; i < 9; i++)
                {
                    AddParticle(PARTICLE_TYPE.WALL, 2 + i, 50 - i);
                }

                break;
        }
    }

    void SpawnTestEnv(string env)
    {
        switch (env)
        {
            case "1":
                AddParticle(PARTICLE_TYPE.WATER, UnityEngine.Random.Range(0, (int)(WIDTH / 2) - 1), HEIGHT - 1);
                AddParticle(PARTICLE_TYPE.WATER, UnityEngine.Random.Range(0, (int)(WIDTH / 2) - 1), HEIGHT - 1);
                AddParticle(PARTICLE_TYPE.SAND, UnityEngine.Random.Range((int)(WIDTH / 2) + 1, WIDTH), HEIGHT - 1);
                break;
        }
    }
}