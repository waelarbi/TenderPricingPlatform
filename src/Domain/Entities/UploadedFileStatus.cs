namespace Domain.Entities
{
    public enum UploadedFileStatus : byte
    {
        Queued = 0,
        Parsed = 1,
        Failed = 2
    }
}