using CharityHealth.Domain.Enums;

namespace CharityHealth.Web.Models;

public sealed class PendingFile
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public Stream Stream { get; set; } = Stream.Null;
    public DocumentType DocumentType { get; set; } = DocumentType.Other;
}
