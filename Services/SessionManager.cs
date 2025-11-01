namespace TiketLaut.Services
{
    public static class SessionManager
    {
        public static Pengguna? CurrentUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}