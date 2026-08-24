namespace IndependentApproval.Api.Application.Directory;

public sealed record DirectorySelectionReference(byte[] Sid, Guid? ObjectGuid);

public interface IDirectorySelectionTokenService
{
    string Create(DirectoryUser directoryUser);

    bool TryRead(
        string? selectionToken,
        out DirectorySelectionReference? reference);
}
