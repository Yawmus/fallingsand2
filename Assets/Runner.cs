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
        int kernel = shader.FindKernel("CSMain");
        uint[] test = { 1, 2, 3, 5, 2, 1, 2, 3, 0, 7, 5, 8, 1, 10, 1, 2, 3, 5 };
        List<uint[]> tests = new List<uint[]>();
        tests.Add(test);

        for(int k=0; k<tests.Count; k++)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            uint[] inputs = tests[k];

            int valLen = (int) Mathf.Pow(2, inputs.Length);
            uint[] vals = new uint[valLen];
            int numThreads = (int)Mathf.Floor(valLen / 16);

            shader.SetInt("THREAD_GROUP", numThreads);
            stopWatch.Start();

            for (int i=0; i < valLen; i++)
            {
                vals[i] = (uint)i;
            }

            // Extra dimensions for padding
            ComputeBuffer inputBuffer = new ComputeBuffer(inputs.Length, 4);
            ComputeBuffer valBuffer = new ComputeBuffer(valLen, 4);
            int[] outputs = new int[valLen];

            inputBuffer.SetData(inputs);
            valBuffer.SetData(vals);
            shader.SetBuffer(kernel, "inputs", inputBuffer);
            shader.SetBuffer(kernel, "vals", valBuffer);

            shader.Dispatch(kernel, numThreads, 1, 1);
            valBuffer.GetData(outputs);

            for (int i = 0; i < valLen; i++)
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
