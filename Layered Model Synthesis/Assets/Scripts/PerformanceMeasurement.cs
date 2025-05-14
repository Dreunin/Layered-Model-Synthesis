using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

    public void MeasureFunction(Action action)
    {
        Start();
        action();
        Stop();
    }
    
    public T MeasureFunction<T>(Func<T> action)
    {
        Start();
        T val = action();
        Stop();
        return val;
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
    
    public static void ReportMultiple(params PerformanceMeasurement[] measurements)
    {
        var nameLength = Math.Max(measurements.Max(m => m.name.Length), 11);
        var countLength = Math.Max(measurements.Max(m => m.Count).ToString().Length, 5);

        var totals = measurements.Select(m => m.Total().ToString("g5")).ToArray();
        var means = measurements.Select(m => m.Mean().ToString("g5")).ToArray();
        var mins = measurements.Select(m => m.Min().ToString("g5")).ToArray();
        var maxs = measurements.Select(m => m.Max().ToString("g5")).ToArray();
        // ReSharper disable PossibleInvalidOperationException
        var medians = measurements.Select(m => m.Median().Value.ToString("g5")).ToArray();
        var stddevs = measurements.Select(m => m.StdDev().Value.ToString("g5")).ToArray();
        // ReSharper restore PossibleInvalidOperationException

        var totalLength = Math.Max(totals.Max(m => m.Length), 5);
        var meanLength = Math.Max(means.Max(m => m.Length), 4);
        var minLength = Math.Max(mins.Max(m => m.Length), 3);
        var maxLength = Math.Max(maxs.Max(m => m.Length), 3);
        var medianLength = Math.Max(medians.Max(m => m.Length), 6);
        var stddevLength = Math.Max(stddevs.Max(m => m.Length), 6);

        var sb = new StringBuilder();
        sb.AppendLine($"{"Measurement".PadRight(nameLength)} | {"Count".PadLeft(countLength)} | {"Total".PadLeft(totalLength + 3)} | {"Mean".PadLeft(meanLength + 3)} | {"Min".PadLeft(minLength + 3)} | {"Max".PadLeft(maxLength + 3)} | {"Median".PadLeft(medianLength + 3)} | {"StdDev".PadLeft(stddevLength + 3)} |");
        for (var i = 0; i < measurements.Length; i++)
        {
            var measurement = measurements[i];
            sb.AppendLine($"{measurement.name.PadRight(nameLength)} | {measurement.Count.ToString().PadLeft(countLength)} | {totals[i].PadLeft(totalLength)} ms | {means[i].PadLeft(meanLength)} ms | {mins[i].PadLeft(minLength)} ms | {maxs[i].PadLeft(maxLength)} ms | {medians[i].PadLeft(medianLength)} ms | {stddevs[i].PadLeft(stddevLength)} ms |");
        }
        
        Debug.Log(sb);
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