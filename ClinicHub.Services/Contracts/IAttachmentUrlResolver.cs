namespace ClinicHub.Services.Contracts
{
    public interface IAttachmentUrlResolver
    {
        string Resolve(string? fileName);
    }
}
