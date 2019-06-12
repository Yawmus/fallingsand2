using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Runner : MonoBehaviour
{
    public ComputeShader shader;
    public List<uint> inputs;

    // Start is called before the first frame update
    void Start()
    {
        List<uint> vals = new List<uint>();
        for(int i=0; i < Mathf.Pow(inputs.Count, 2); i++)
        {
            vals.Add((uint)i);
        }

        // Extra dimensions for padding
        ComputeBuffer inputBuffer = new ComputeBuffer(inputs.Count, 4);
        ComputeBuffer valBuffer = new ComputeBuffer(vals.Count, 4);
        int[] outputs = new int[vals.Count];

        int kernel = shader.FindKernel("CSMain");

        inputBuffer.SetData(inputs);
        valBuffer.SetData(vals);
        shader.SetBuffer(kernel, "inputs", inputBuffer);
        shader.SetBuffer(kernel, "vals", valBuffer);
        shader.Dispatch(kernel, vals.Count, 1, 1);
        valBuffer.GetData(outputs);

        inputBuffer.Dispose();
        valBuffer.Dispose();

        Debug.Log(outputs.Max());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
