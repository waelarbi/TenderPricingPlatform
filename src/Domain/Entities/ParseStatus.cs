namespace Domain.Entities
{
    public enum ParseStatus : byte
    {
        Queued = 0,
        Parsed = 1,
        Failed = 2
    }
}