using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyVoltage.Data;
using MyVoltage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyVoltage.Services
{
    public class GoogleMapsService
    {
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly UserManager<ApplicationUser> _userManager;
        public GoogleMapsService(UserManager<ApplicationUser> userManager, DbContextOptions<MyVoltageDbContext> options)
        {
            _userManager = userManager;
            _options = options;
        }


    }
}
