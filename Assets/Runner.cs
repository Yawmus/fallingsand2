using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Runner : MonoBehaviour
{
    public ComputeShader shader;

    // Start is called before the first frame update
    void Start()
    {
        uint[] test = { 1, 2, 3, 0, 2, 1, 2, 3, 0, 7 };
        List<uint[]> tests = new List<uint[]>();
        tests.Add(test);

        for(int k=0; k<tests.Count; k++)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            List<uint> vals = new List<uint>();
            uint[] inputs = tests[k];

            for (int i=0; i < Mathf.Pow(inputs.Length, 2); i++)
            {
                vals.Add((uint)i);
            }

            // Extra dimensions for padding
            ComputeBuffer inputBuffer = new ComputeBuffer(inputs.Length, 4);
            ComputeBuffer valBuffer = new ComputeBuffer(vals.Count, 4);
            int[] outputs = new int[vals.Count];

            int kernel = shader.FindKernel("CSMain");
            inputBuffer.SetData(inputs);
            valBuffer.SetData(vals);
            shader.SetBuffer(kernel, "inputs", inputBuffer);
            shader.SetBuffer(kernel, "vals", valBuffer);

            stopWatch.Start();
            shader.Dispatch(kernel, vals.Count > 1024 ? 1024 : vals.Count, 1, 1);
            valBuffer.GetData(outputs);

            for (int i = 0; i < vals.Count; i++)
            {
                //Debug.Log(outputs[i]); ;
            }

            int max = outputs.Max();
            stopWatch.Stop();
            Debug.Log(max);
            Debug.Log(  "Time: " + stopWatch.ElapsedMilliseconds + "ms");

            inputBuffer.Dispose();
            valBuffer.Dispose();
        }
    }
    public static void DumpToConsole(object obj)
    {
        var output = JsonUtility.ToJson(obj, true);
        Debug.Log(output);
    }

}
