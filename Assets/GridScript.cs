using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GridScript : MonoBehaviour
{

    public int WIDTH = 20, HEIGHT = 20;
    public bool DEBUG = true;
    public string TEST_ENV = "2";

    // Garbage collection
    int DISPOSE_HEIGHT = 3, TOTAL_HEIGHT;

    public List<int> ids;
    float tick = 0;

    public ComputeShader shader;
    Texture2D tex;

    public enum CHARACTER
    {
        HUMAN,
        WALL,
        GRASS,
        DIRT,
        WATER
    }

    public int[] grid;
    public int[] debugGrid;

    public struct ParticleProperties
    {
        public Color color;
        public int properties;
    }

    public const float P_FPS = 30;
    public const float FPS = 30;
    public const float SPAWN_RATE = 30;
    public const float GARBAGE_COLLECTION_RATE = 40;


    // Use this for initialization
    void Start()
    {
        TOTAL_HEIGHT = HEIGHT + DISPOSE_HEIGHT;
        ids = new List<int>();
        grid = new int[(WIDTH + 2) * (HEIGHT + 2)]; // 0-nothing, 1-sand, 2-water, 3-gas


        DrawTestEnv(TEST_ENV);

        //AddParticle(PARTICLE_TYPE.SAND, 0, 10);

        tex = new Texture2D(WIDTH, HEIGHT);
        tex.filterMode = FilterMode.Point;
        GetComponent<Renderer>().material.mainTexture = tex;

        shader.SetInt("width", WIDTH);
        shader.SetInt("height", HEIGHT);
        shader.SetInt("debug", DEBUG ? 1 : 0);
        InvokeRepeating("SpawnParticles", 0f, 1/SPAWN_RATE);
        InvokeRepeating("UpdateGame", 0f, 1 / FPS);
    }
    void SpawnParticles()
    {
        SpawnTestEnv(TEST_ENV);
        //AddParticle(GetRandom(), UnityEngine.Random.Range(0, WIDTH), HEIGHT - 1);
    }

    void AddParticle(CHARACTER pt, int x, int y)
    {
        grid[WIDTH * y + x] = GetProperties(pt);
    }

    // Update is called once per frame
    async void Update()
    {
        tick += Time.deltaTime;
    }


    async Task UpdateGame()
    {
        await ApplyPhysicsAndDraw();
    }


    async Task ApplyPhysicsAndDraw()
    {
        Color[] drawPixels = new Color[grid.Length];
        int[] debugGrid = new int[grid.Length];

        ComputeBuffer gridBuffer = new ComputeBuffer(grid.Length, 4);
        ComputeBuffer frameBuffer = new ComputeBuffer(grid.Length, 16);
        ComputeBuffer debugVars = new ComputeBuffer(grid.Length, 4);

        int kernel2 = shader.FindKernel("ApplyPhysicsAndDraw");

        debugVars.SetData(debugGrid);
        gridBuffer.SetData(grid);
        shader.SetBuffer(kernel2, "grid", gridBuffer);
        shader.SetBuffer(kernel2, "frameColor", frameBuffer);
        shader.SetBuffer(kernel2, "debugVars", debugVars);
        shader.Dispatch(kernel2, grid.Length, 1, 1);
        frameBuffer.GetData(drawPixels);

        tex.SetPixels(drawPixels);
        tex.Apply();

        gridBuffer.Dispose();
        frameBuffer.Dispose();
        debugVars.Dispose();
    }



    int GetProperties(CHARACTER pt)
    {

        const int EXISTS = 1, GRAV = 2;
        // Properties 8 bits
        // Exists + Grav + (Solid|0, Liquid|4, Gas|8, Plasma|12) + ...
        // 1 + 2 + (4 + 8) + 16 + 32 + 64 + 128
        int properties = 0;

        // Type 8 bits
        // (Empty|0, Sand|256, Water|512, Fire|768, Wall|1024)
        // (256 + 512 + 1024)
        int type = 0;

        switch (pt)
        {
            case CHARACTER.HUMAN:
                properties = 1 + 2 + 0;
                type = 256;
                break;
            case CHARACTER.GRASS:
                properties = 1 + 2 + 4;
                type = 512;
                break;
            case CHARACTER.WATER:
                properties = 1 + 2 + 4;
                type = 512;
                break;
            case CHARACTER.DIRT:
                properties = 1 + 2 + 8; 
                type = 768;
                break;
            case CHARACTER.WALL:
                properties = 1 + 0 + 0;
                type = 1024;
                break;
        }
        return properties + type;
    }

    CHARACTER GetRandom()
    {
        switch (UnityEngine.Random.Range(0, 3))
        {
            case 0:
                return CHARACTER.GRASS;
            case 1:
                return CHARACTER.WATER;
        }
        return CHARACTER.DIRT;
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
                        AddParticle(CHARACTER.WALL, x, y);
                    }
                    if ((x == 10 || x == 20) && y > 30 && y < 40)
                    {
                        AddParticle(CHARACTER.WALL, x, y);
                    }
                    if (y == 30 && x > 60 && x < 70)
                    {
                        AddParticle(CHARACTER.WALL, x, y);
                    }

                    if ((x == 60 || x == 70) && y > 30 && y < 40)
                    {
                        AddParticle(CHARACTER.WALL, x, y);
                    }
                }

                // Diag top left -> bot right
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(CHARACTER.WALL, 21 + i, 40 - i);
                }
                // Diag top right -> bot left
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(CHARACTER.WALL, 42 - i, 40 - i);
                }

                for (int i = 0; i < 10; i++)
                {
                    AddParticle(CHARACTER.WALL, 22 + i, 16 + i);
                }
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(CHARACTER.WALL, 41 - i, 16 + i);
                }
                for (int i = 0; i < 9; i++)
                {
                    AddParticle(CHARACTER.WALL, 2 + i, 50 - i);
                }

                AddParticle(CHARACTER.WALL, 0, 50);
                AddParticle(CHARACTER.WALL, 1, 51);

                // Diag top left -> bot right
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(CHARACTER.WALL, 55 + (int)Mathf.Ceil(i * .5f), 70 - i);
                }

                break;
            case "2":
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(CHARACTER.WALL, 21 + i, 40);
                }
                // Diag top left -> bot right
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(CHARACTER.WALL, 45 + i, 40 - i);
                }
                // Diag top right -> bot left
                for (int i = 0; i < 10; i++)
                {
                    AddParticle(CHARACTER.WALL, 42 - i, 40 - i);
                }
                break;
            case "3":
                AddParticle(CHARACTER.WALL, 0, 0);
                AddParticle(CHARACTER.WALL, WIDTH - 1, 0);
                AddParticle(CHARACTER.WALL, WIDTH - 1, HEIGHT - 1);
                AddParticle(CHARACTER.WALL, 0, HEIGHT - 1);
                break;
            case "4":
                for (int i = 0; i < WIDTH * HEIGHT; i++)
                {
                    int x = i % WIDTH;
                    int y = i / HEIGHT;
                    if (y == 30 && x > 10 && x < 20)
                    {
                        AddParticle(CHARACTER.WALL, x, y);
                    }
                    if ((x == 10 || x == 20) && y > 30 && y < 40)
                    {
                        AddParticle(CHARACTER.WALL, x, y);
                    }
                    if (y == 30 && x > 60 && x < 70)
                    {
                        AddParticle(CHARACTER.WALL, x, y);
                    }

                    if ((x == 60 || x == 70) && y > 30 && y < 40)
                    {
                        AddParticle(CHARACTER.WALL, x, y);
                    }
                }
                AddParticle(CHARACTER.WATER, UnityEngine.Random.Range(10, 20), HEIGHT - 5);
                break;
        }
    }

    void SpawnTestEnv(string env)
    {
        switch (env)
        {
            case "1":
                AddParticle(CHARACTER.WATER, UnityEngine.Random.Range(0, (int)(WIDTH / 2) - 1), HEIGHT - 1);
                AddParticle(CHARACTER.WATER, UnityEngine.Random.Range(0, (int)(WIDTH / 2) - 1), HEIGHT - 1);
                AddParticle(CHARACTER.WATER, UnityEngine.Random.Range((int)(WIDTH / 2) + 1, WIDTH - 20), HEIGHT - 1);
                AddParticle(CHARACTER.WATER, UnityEngine.Random.Range(WIDTH - 20, WIDTH - 1), 0);
                //AddParticle(PARTICLE_TYPE.FIRE, UnityEngine.Random.Range(15, 60), 0);
                break;
            case "2":
                AddParticle(CHARACTER.WATER, UnityEngine.Random.Range(15, 60), 0);
                //AddParticle(PARTICLE_TYPE.FIRE, UnityEngine.Random.Range(15, 60), 0);
                //AddParticle(PARTICLE_TYPE.FIRE, UnityEngine.Random.Range(15, 60), 0);
                break;
            case "4":
                //AddParticle(PARTICLE_TYPE.WATER, UnityEngine.Random.Range(10, 20), HEIGHT - 20);
               // AddParticle(PARTICLE_TYPE.SAND, UnityEngine.Random.Range((int)(WIDTH / 2) + 1, WIDTH - 20), HEIGHT - 2);
                //AddParticle(PARTICLE_TYPE.FIRE, UnityEngine.Random.Range(15, 60), 0);
                break;
        }
    }
}