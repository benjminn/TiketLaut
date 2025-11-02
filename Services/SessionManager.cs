using System.Collections.Generic;
using TiketLaut.Models;

namespace TiketLaut.Services
{
    public static class SessionManager
    {
        public static Pengguna? CurrentUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;

        // ADD: Properties untuk menyimpan state pencarian
        public static SearchCriteria? LastSearchCriteria { get; set; }
        public static List<Jadwal>? LastSearchResults { get; set; }

        public static void Logout()
        {
            CurrentUser = null;
            // Clear search data saat logout
            LastSearchCriteria = null;
            LastSearchResults = null;
        }

        // Method untuk menyimpan hasil pencarian
        public static void SaveSearchSession(SearchCriteria criteria, List<Jadwal> results)
        {
            LastSearchCriteria = criteria;
            LastSearchResults = results;
        }

        // Method untuk membersihkan search session
        public static void ClearSearchSession()
        {
            LastSearchCriteria = null;
            LastSearchResults = null;
        }
    }
}
