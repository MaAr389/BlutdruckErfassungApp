using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BlutdruckErfassungApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlutdruckErfassungApp.Pages;

[AllowAnonymous]
public sealed class LoginModel(AuthService authService) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string ReturnUrl { get; private set; } = "/";
    public string ErrorMessage { get; private set; } = string.Empty;

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect("/");
        }

        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await authService.ValidateCredentialsAsync(Input.UserName, Input.Password);
        if (user is null)
        {
            ErrorMessage = "Anmeldung fehlgeschlagen. Bitte Benutzername/Passwort prüfen.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });

        return LocalRedirect(ReturnUrl);
    }

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "Benutzername ist erforderlich.")]
        [Display(Name = "Benutzername")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Passwort ist erforderlich.")]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = string.Empty;
    }
}
