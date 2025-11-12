using System;

namespace ManagementEmployee
{
    public static class AppSession
    {
        public static int? CurrentUserId { get; private set; }
        public static string? CurrentUserEmail { get; private set; }
        public static string? CurrentUserName { get; private set; }

        public static bool IsAuthenticated => CurrentUserId.HasValue && CurrentUserId.Value > 0;

        public static void SignIn(int userId, string? email, string? name)
        {
            CurrentUserId = userId;
            CurrentUserEmail = email;
            CurrentUserName = name;
            SignedIn?.Invoke(null, EventArgs.Empty);
        }

        public static void SignOut()
        {
            CurrentUserId = null;
            CurrentUserEmail = null;
            CurrentUserName = null;
            SignedOut?.Invoke(null, EventArgs.Empty);
        }

        public static event EventHandler? SignedIn;
        public static event EventHandler? SignedOut;
    }
}
