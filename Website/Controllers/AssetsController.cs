using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MyVoltage.Data;
using MyVoltage.Models;

namespace MyVoltage.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AssetsController : Controller
    {
        private readonly DbContextOptions<MyVoltageDbContext> _options;
        private readonly IHttpContextAccessor _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssetsController(
            UserManager<ApplicationUser> userManager,
            DbContextOptions<MyVoltageDbContext> options,
            IHttpContextAccessor contextet)
        {
            _userManager = userManager;
            _context = contextet;
            _options = options;
        }

        [Route("css")]
        public IActionResult Css()
        {
            string Url = _context.HttpContext.Request.Host.ToString();

            using (var db = new MyVoltageDbContext(_options))
            {
                var companySkin = db.CompanySkins.Where(tbl => tbl.Url == Url).SingleOrDefault();

                string primaryColor = "#00225b";
                string secondaryColor = "#00b3eb";

                var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "template.css");

                if (companySkin != null)
                {
                    primaryColor = companySkin.PrimaryColor;
                    secondaryColor = companySkin.SecondaryColor;
                    string css = System.IO.File.ReadAllText(file);
                    if (companySkin.CompanyID == 1018)
                    {
                        css = css.Replace("var(--logo-width)", "300px");
                    }

                    css = css.Replace("var(--brand-primary)", primaryColor);
                    css = css.Replace("var(--brand-secondary)", secondaryColor);

                    return Content(css, "text/css");
                }

                var customer = db.Customers.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
                if (customer != null)
                {
                    companySkin = db.CompanySkins.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();

                    primaryColor = "#00225b";
                    secondaryColor = "#00b3eb";

                    file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "template.css");

                    if (companySkin != null)
                    {
                        primaryColor = companySkin.PrimaryColor;
                        secondaryColor = companySkin.SecondaryColor;
                        string css = System.IO.File.ReadAllText(file);
                        if (companySkin.CompanyID == 1018)
                        {
                            css = css.Replace("var(--logo-width)", "300px");
                        }

                        css = css.Replace("var(--brand-primary)", primaryColor);
                        css = css.Replace("var(--brand-secondary)", secondaryColor);

                        return Content(css, "text/css");
                    }


                }

                return Content(String.Empty, "text/css");

            }
        }

        [Route("logo/{white?}")]
        public IActionResult Logo(string white)
        {
            string Url = _context.HttpContext.Request.Host.ToString();

            using (var db = new MyVoltageDbContext(_options))
            {
                string logo = white != null ? "myvoltage-logo-w.png" : "myvoltage-logo.png";

                var companySkin = db.CompanySkins.Where(tbl => tbl.Url == Url).SingleOrDefault();

                if (companySkin != null)
                {
                    logo = white != null ? companySkin.LogoWhite : companySkin.Logo;
                }

                var customer = db.Customers.Where(p => p.UserID == _userManager.GetUserId(User)).SingleOrDefault();
                if (customer != null)
                {
                    companySkin = db.CompanySkins.Where(tbl => tbl.CompanyID == customer.CompanyID).SingleOrDefault();
                }

                if (companySkin != null)
                {
                    logo = white != null ? companySkin.LogoWhite : companySkin.Logo;
                }

                var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", logo);

                EntityTagHeaderValue etag = new EntityTagHeaderValue("\"" + Guid.NewGuid().ToString() + "\"");

                return PhysicalFile(file, "image/png", logo, DateTime.Now, etag);
            }
        }
    }
}