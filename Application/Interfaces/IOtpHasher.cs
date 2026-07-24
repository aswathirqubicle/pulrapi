namespace Core.Application.Interfaces
{
    /// <summary>
    /// Hashes and verifies short-lived one-time codes (password reset / email
    /// verification OTPs) so they are never stored in plaintext. Verification uses
    /// a constant-time comparison to avoid timing side channels.
    /// </summary>
    public interface IOtpHasher
    {
        /// <summary>Returns a deterministic hash of the supplied code, safe to persist.</summary>
        string Hash(string code);

        /// <summary>Returns true if <paramref name="code"/> matches the stored <paramref name="hash"/>.</summary>
        bool Verify(string code, string hash);
    }
}
