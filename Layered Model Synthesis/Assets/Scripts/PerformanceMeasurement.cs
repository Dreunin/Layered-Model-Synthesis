using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

public class PerformanceMeasurement
{
    public string name;
    
    private List<float> measurements = new();
    private Stopwatch stopwatch = new();
    private CustomSampler sampler;
    
    public PerformanceMeasurement(string name)
    {
        this.name = name;
        sampler = CustomSampler.Create(name);
    }
    
    public void Start()
    {
        sampler.Begin();
        stopwatch.Restart();
    }
    
    public void Stop()
    {
        sampler.End();
        stopwatch.Stop();
        measurements.Add(stopwatch.ElapsedMilliseconds);
    }

    public void Report()
    {
        Debug.Log($@"Performance Analysis {name}:
    Count:  {Count}
    Total:  {Total()} ms
    Mean:   {Mean()} ms
    Min:    {Min()} ms
    Max:    {Max()} ms
    Median: {Median()} ms
    StdDev: {StdDev()} ms");
    }

    private float Total() => measurements.Sum();
    private float Min() => measurements.Min();
    private float Max() => measurements.Max();
    private float Mean() => measurements.Average();
    private int Count => measurements.Count;

    private float? Median()
    {
        if (Count == 0) return null;

        if (Count % 2 == 0)
            return measurements.OrderBy(x => x).Skip((Count / 2) - 1).Take(2).Average();
        else
            return measurements.OrderBy(x => x).ElementAt(Count / 2);
    }

    private float? StdDev()
    {
        if (Count == 0) return null;
        
        float mean = Mean();
        return Mathf.Sqrt(measurements.Sum(x => (x - mean) * (x - mean)) / Count);
    }
}