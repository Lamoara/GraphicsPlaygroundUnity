using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

using static FunctionLib;

public class graph_controller : MonoBehaviour
{
    [SerializeField]
    GameObject dot;

    [SerializeField, Range(10, 100)]
    int resolution;
    [SerializeField]
    FuncType funcType = FuncType.Wave;

    Transform[] dots;

    void Awake()
    {
        CreateGraph();
    }

    void Update()
    {
        UpdateGraph();
    }

    void CreateGraph()
    {
        float step = resolution / 2f;
        Vector3 scale = Vector3.one / step;

        dots = new Transform[resolution * resolution];
        for (int i = 0; i < dots.Length; i++)
        {
            Transform obj = dots[i] = Instantiate(dot, transform, false).transform;

            obj.localScale = scale;
        }
    }

    void UpdateGraph()
    {
        float time = Time.time;
        Function f = GetFunction(funcType);

		float step = 2f / resolution;
        float v = 0.5f * step - 1f;
		for (int i = 0, x = 0, z = 0; i < dots.Length; i++, x++) {
			if (x == resolution) {
				x = 0;
				z += 1;
                v = (z + 0.5f) * step - 1f;
			}
			float u = (x + 0.5f) * step - 1f;
			dots[i].localPosition = f(u, v, time);
        }
    }

    Function GetFunction(FuncType funcType)
    {
        switch (funcType){
            case FuncType.Wave: return Wave;
            case FuncType.MultiWave: return MultiWave;
            case FuncType.Ripple: return Ripple;
            case FuncType.Sphere: return Sphere;
            case FuncType.Torus: return Torus;
        }

        return null;
    }


}
