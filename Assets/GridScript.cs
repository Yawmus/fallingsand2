using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class GridScript : MonoBehaviour
{

    public int WIDTH = 20, HEIGHT = 20;
    public List<int> ids;

    public Dictionary<int, Particle> particles;
    public ComputeShader shader;
    Texture2D tex;
    ComputeBuffer buffer;
    public struct Particle
    {
        public Color color;
        public Vector3 position;
    }
    public struct ComputeData {
        public Vector3 position;
        public int id;
    }


    // Use this for initialization
    void Start()
    {
        ids = new List<int>();
        particles = new Dictionary<int, Particle>();
        for (int i = 0; i < WIDTH * HEIGHT; i++)
        {
            int x = i % WIDTH;
            int y = i / HEIGHT;

            //AddParticle(x, y);
        }


        tex = new Texture2D(WIDTH, HEIGHT);
        //pixels = tex.GetPixels ();
        tex.filterMode = FilterMode.Point;
        //		tex = Instantiate(GetComponent<Renderer> ().material.mainTexture) as Texture2D;
        GetComponent<Renderer>().material.mainTexture = tex;
    }

    void AddParticle(int x, int y)
    {
        int id = UnityEngine.Random.Range(1, 20000);
        Vector3 position = new Vector3(x, y, 0);
        Color color = new Color(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f), 1);
        particles[id] = new Particle { position = position, color = color };
        ids.Add(id);
    }


    // Update is called once per frame
    void Update()
    {

        AddParticle(UnityEngine.Random.Range(0, WIDTH), HEIGHT - 1);


        Color[] pixels = new Color[WIDTH * HEIGHT];
        for (int i = 0; i < WIDTH * HEIGHT; i++)
        {
            pixels[i] = new Color(0, 0, 0, 1);
        }
        for (int i = 0; i < ids.Count; i++)
        {
            int x = i % WIDTH;
            int y = i / HEIGHT;
            int id = ids[i];

            Vector3 t = particles[id].position;
            //Debug.Log("x:" + x + ", y:" + y);
            pixels[WIDTH * (int)t.y + (int)t.x] = particles[id].color;
        }

        //pixels[2] = new Color(1, 1, 1, 1);

        //Particle.Positions = g.OrderBy(i => i.y).ThenBy(i => i.x).ToList();
        //tex.SetPixel(0, 0, Color.cyan);
        if (ids.Count != 0)
        {
            RunShader();
        }
        tex.SetPixels(pixels);
        tex.Apply();
    }

    void RunShader()
    {
        List<ComputeData> input = new List<ComputeData>();
        for (int i = 0; i < ids.Count; i++)
        {
            int x = i % WIDTH;
            int y = i / HEIGHT;
            int id = ids[i];

            Particle p = particles[id];
            ComputeData temp = new ComputeData { position = p.position, id = id};
            input.Add(temp);
        }
        ComputeData[] output = new ComputeData[input.Count];
        buffer = new ComputeBuffer(input.Count, 16);
        //INITIALIZE DATA HERE

        buffer.SetData(input.ToArray());
        int kernel = shader.FindKernel("Move");
        shader.SetBuffer(kernel, "dataBuffer", buffer);
        shader.SetInt("width", WIDTH);
        shader.SetInt("height", HEIGHT);
        shader.Dispatch(kernel, input.Count, 1, 1);
        buffer.GetData(output);

        for (int i = 0; i < output.Length; i++)
        {
            ComputeData data = output[i];
            Particle p = particles[data.id];
            if(data.position.y < 0)
            {
                particles.Remove(data.id);
                ids.Remove(data.id);
            }
            else
            {
                particles[data.id] = new Particle { position = data.position, color = p.color };
            }
        }
        buffer.Dispose();
    }
}