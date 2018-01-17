using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JSONAuth.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace TokenProvider.Controllers
{
    [Route("api/[controller]/[Action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager,
                                    IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }
        [Authorize]
        [HttpGet]
        public IEnumerable<string> GetTest()
        {
            return new string[] { "test1", "test2" };
        }

        [HttpPost]
        public async Task<object> Login([FromBody] LoginDto model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

            if (result.Succeeded)
            {
                var appUser = _userManager.Users.SingleOrDefault(r => r.Email == model.Email);
                return GenerateJwtToken(model.Email, appUser);
                //return await GenerateToken(model.Email);
            }

            throw new ApplicationException("INVALID_LOGIN_ATTEMPT");
        }

        [HttpPost]
        public async Task<object> Register([FromBody] RegisterDto model)
        {
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return GenerateJwtToken(model.Email, user);
            }


            throw new ApplicationException(result.Errors.Select(x => x.Description).ToList().FirstOrDefault());
        }

        private object GenerateJwtToken(string email, IdentityUser user)
        {

            try
            {
                var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name,user.Email)
            };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JwtToken").GetSection("JwtKey").Value));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration.GetSection("JwtToken").GetSection("JwtExpireDays").Value));

                var token = new JwtSecurityToken(
                    _configuration.GetSection("JwtToken").GetSection("JwtIssuer").Value,
                    _configuration.GetSection("JwtToken").GetSection("JwtIssuer").Value,
                    claims,
                    expires: expires,
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;

        }



        //public class LoginDto
        //{
        //    [Required]
        //    public string Email { get; set; }

        //    [Required]
        //    public string Password { get; set; }

        //}

        public class RegisterDto
        {
            [Required]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "PASSWORD_MIN_LENGTH", MinimumLength = 6)]
            public string Password { get; set; }
        }
    }
}