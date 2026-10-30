using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AbibiFood.Api.Services;
using AbibiFood.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
namespace AbibiFood.Api.Controllers;

public sealed class LoginInput
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}
public sealed class AccountController(SignInManager<AppUser> signIn, UserManager<AppUser> users, IConfiguration config, Messages messages) : Controller
{
    [HttpGet, AllowAnonymous] public IActionResult Login() => View(new LoginInput());
    [HttpPost, AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var user = await users.FindByEmailAsync(input.Email);
        if (user is not null && user.IsActive && (await users.IsInRoleAsync(user, "Manager") || await users.IsInRoleAsync(user, "Employee")))
        {
            var result = await signIn.PasswordSignInAsync(user, input.Password, false, lockoutOnFailure: true);
            if (result.Succeeded) return RedirectToAction("Index", "Admin");
        }
        ModelState.AddModelError("", messages["LoginFailed"]); return View(input);
    }
    [HttpPost, Authorize] public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    [HttpGet, AllowAnonymous] public IActionResult Denied() => StatusCode(403, "دسترسی مجاز نیست.");
    // این ورود فقط زیرساخت کارکنان/مدیر است؛ سناریوی OTP مشتری هنوز تعریف و اجرا نشده است.
    [HttpPost("api/auth/token"), AllowAnonymous, IgnoreAntiforgeryToken, EnableRateLimiting("login")]
    public async Task<IActionResult> Token([FromBody] LoginInput input)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var user = await users.FindByEmailAsync(input.Email);
        if (user is null || !user.IsActive || !(await signIn.CheckPasswordSignInAsync(user, input.Password, true)).Succeeded) return Unauthorized();
        var roles = await users.GetRolesAsync(user);
        if (!roles.Any(x => x is "Manager" or "Employee")) return Forbid();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
        var expires = DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:Minutes"));
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims, expires: expires,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), expiresAt = expires });
    }
}
