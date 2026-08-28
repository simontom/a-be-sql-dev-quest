using System.Text;

namespace AlzaLogistics.Core.Model;

/// <summary>
/// Output of the planning algorithm with per-trip assignments and summary statistics.
/// </summary>
public sealed class PlanningResult
{
    public required Trip[] Trips { get; init; }
    public required Package[] UnassignedPackages { get; init; }
    public required int TotalPackages { get; init; }
    public required TimeSpan ElapsedTime { get; init; }
    public required string AlgorithmName { get; init; }

    public int AssignedPackageCount => TotalPackages - UnassignedPackages.Length;
    public decimal TotalProfit => Trips.Sum(t => t.TotalProfit);
    private double AverageVolumeUtilization => Trips.Length > 0 ? Trips.Average(t => t.VolumeUtilization) : 0;
    private double AverageWeightUtilization => Trips.Length > 0 ? Trips.Average(t => t.WeightUtilization) : 0;

    public string GetSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════");
        sb.AppendLine("  PLANNING RESULT SUMMARY");
        sb.AppendLine("═══════════════════════════════════════════════════════");
        sb.AppendLine($"  Algorithm:          {AlgorithmName}");
        sb.AppendLine($"  Total packages:     {TotalPackages:N0}");
        sb.AppendLine($"  Assigned:           {AssignedPackageCount:N0} ({(double)AssignedPackageCount / TotalPackages:P1})");
        sb.AppendLine($"  Unassigned:         {UnassignedPackages.Length:N0}");
        sb.AppendLine($"  Total profit:       {TotalProfit:N2} CZK");
        sb.AppendLine($"  Trips used:         {Trips.Count(t => t.PackageCount > 0)}/{Trips.Length}");
        sb.AppendLine($"  Avg volume util:    {AverageVolumeUtilization:P1}");
        sb.AppendLine($"  Avg weight util:    {AverageWeightUtilization:P1}");
        sb.AppendLine($"  Elapsed time:       {ElapsedTime.TotalMilliseconds:F2} ms");
        sb.AppendLine("═══════════════════════════════════════════════════════");
        return sb.ToString();
    }
}
