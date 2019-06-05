using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GridScript : MonoBehaviour {

	public int WIDTH = 10, HEIGHT = 10;
	public int[,] grid;

	public List<Particle> particles;
	public ComputeShader shader;
	Texture2D tex;
	ComputeBuffer buffer;

	// Use this for initialization
	void Start () {
		particles = new List<Particle> ();
		for (int i = 0; i < WIDTH * HEIGHT; i++)
		{
			int x = i % WIDTH;
			int y = i / HEIGHT;
			Debug.Log ("x:" + x + ", y:" + y);
			particles.Add (new Particle (x, y));
		}


		tex = new Texture2D (WIDTH, HEIGHT);
		//pixels = tex.GetPixels ();
		tex.filterMode = FilterMode.Point;
//		tex = Instantiate(GetComponent<Renderer> ().material.mainTexture) as Texture2D;
		GetComponent<Renderer> ().material.mainTexture = tex;
	}

	// Update is called once per frame
	void Update () {
		//pixels = tex.GetPixels ();
//		for (int i = 0; i < WIDTH * HEIGHT; i++)
//		{
//			int x = i % WIDTH;
//			int y = i / HEIGHT;
//			//particles[WIDTH * y + x ] = new Color(grid[x, y], 1, 1);
//			tex.SetPixel(i % WIDTH, y, new Color(grid[x, y], 1, 1));
//		}
		RunShader ();
		tex.SetPixels (Particle.Colors.ToArray());
		tex.Apply ();
	}

	void RunShader()
	{
		Vector3[] output = new Vector3[Particle.Positions.Count];
		buffer = new ComputeBuffer(Particle.Positions.Count, 12);
		//INITIALIZE DATA HERE

		buffer.SetData(Particle.Positions.ToArray());
		int kernel = shader.FindKernel("Move");
		shader.SetBuffer(kernel, "dataBuffer", buffer);
		shader.Dispatch(kernel, Particle.Positions.Count, 1, 1);
		buffer.GetData(output);


		Particle.SetPositions (output [i]);
		Particle.Positions = Particle.Positions.OrderBy (i => i.y).ThenBy (i => i.x).ToList ();
		buffer.Dispose ();
	}
}

public class Particle
{
	public static List<Vector3> Positions = new List<Vector3>();
	static List<Color> Colors = new List<Color>();
	public Color tint;
	public Particle(int x, int y)
	{
		Color color = new Color (Random.Range (0, 1f), Random.Range (0, 1f), Random.Range (0, 1f));
		Vector3 pos = new Vector3 (x, y, 0);
		Positions.Add (pos);
		Colors.Add (color);
	}
	public static void SetPositions(Vector3[] input)
	{
		for (int i=0; i<input.Length; i++)
		{
			Positions[i] = input[i];
		}
	}
}