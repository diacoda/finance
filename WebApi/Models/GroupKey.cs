namespace Finance.Tracking.Models;
/// <summary>
/// Simple composite key helper used for grouping results of two keys.
/// </summary>
public class GroupKey<T1, T2> : IEquatable<GroupKey<T1, T2>>
    where T1 : notnull
    where T2 : notnull
{
    public T1 Item1 { get; set; } = default!;
    public T2 Item2 { get; set; } = default!;

    public bool Equals(GroupKey<T1, T2>? other) =>
        other != null && EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                        && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);

    public override bool Equals(object? obj) => Equals(obj as GroupKey<T1, T2>);
    public override int GetHashCode() => HashCode.Combine(Item1, Item2);
}
