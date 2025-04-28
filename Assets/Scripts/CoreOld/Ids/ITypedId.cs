namespace DVG.Core.Ids
{
    public interface ITypedId<TType>
    {
        TType Value { get; }
    }
}