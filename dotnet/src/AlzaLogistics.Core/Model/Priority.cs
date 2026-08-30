namespace AlzaLogistics.Core.Model;

/// <summary>
/// Defines the priority of a package for delivery planning.
/// </summary>
public enum Priority
{
    /// <summary>
    /// Normal package, scheduled based on profit density and capacity.
    /// </summary>
    Standard = 0,
    
    /// <summary>
    /// Elevated priority, considered identically to Standard but could be used in custom strategies.
    /// </summary>
    Elevated = 1,
    
    /// <summary>
    /// Mandatory package, must be delivered on this day. Bypasses profit density optimization.
    /// </summary>
    Mandatory = 2
}
