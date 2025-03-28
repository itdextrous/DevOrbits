using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyVoltage.Data;
using MyVoltage.Models;

namespace MyVoltage.Services
{
    public class WebServicesProvider
    {
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly UserManager<ApplicationUser> _userManager;
        private IHttpContextAccessor _context;
        private IMemoryCache _cache;

        public WebServicesProvider(UserManager<ApplicationUser> userManager, DbContextOptions<MyVoltageDbContext> options, IHttpContextAccessor context, IMemoryCache cache, IConfiguration config)
        {
            _userManager = userManager;
            _options = options;
            _cache = cache;

            using (var db = new MyVoltageDbContext(_options))
            {
            }
        }

    }
}
