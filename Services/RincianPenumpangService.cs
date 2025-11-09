using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class RincianPenumpangService
    {
        private readonly AppDbContext _context;

        public RincianPenumpangService()
        {
            _context = DatabaseService.GetContext();
        }

        public async Task<List<RincianPenumpang>> GetByTiketIdAsync(int tiketId)
        {
            return await _context.RincianPenumpangs
                .Include(rp => rp.penumpang)
                .Where(rp => rp.tiket_id == tiketId)
                .ToListAsync();
        }
    }
}
