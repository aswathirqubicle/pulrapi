namespace Core.Application.Models.Users
{
    public class CheckEmailResponse
    {
        public bool Exists { get; set; }
        public bool IsVerified { get; set; }
        public bool HasCompletedOnboarding { get; set; }
        public bool TermsAccepted { get; set; }
    }
} 