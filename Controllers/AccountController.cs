using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCodeGenerator.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

public class AccountController : Controller
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService)
    {
        _userService = userService;
    }

    // GET: /Account/Index
    [HttpGet]
    public IActionResult Index()
    {
        // Redirect logged-in users to Home page
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        // Otherwise show login page
        return View();
    }

    // POST: /Account/Index - Login or Register
    [HttpPost]
    public async Task<IActionResult> Index(string email, string password, string actionType)
    {
        if (actionType == "register")
        {
            var existingUser = await _userService.GetByEmailAsync(email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email already exists.");
                return View();
            }

            var passwordHash = UserService.ComputeSha256Hash(password);
            var user = await _userService.CreateUserAsync(email, passwordHash, "Local");
            await SignInUser(user.Email, user.Provider);
            return RedirectToAction("Index", "Home");
        }
        else if (actionType == "login")
        {
            if (!await _userService.ValidateUserAsync(email, password))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            var user = await _userService.GetByEmailAsync(email);
            await SignInUser(user!.Email, user.Provider!);
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    // POST: /Account/GoogleLogin
    [HttpPost]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleResponse"),
            Items =
            {
                { "prompt", "select_account" }
            }
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    // GET: /Account/GoogleResponse
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;

        if (email == null)
            return RedirectToAction("Index");

        var user = await _userService.GetByEmailAsync(email);
        if (user == null)
        {
            user = await _userService.CreateUserAsync(email, null, "Google");
        }

        await SignInUser(user.Email, user.Provider!);
        return RedirectToAction("Index", "Home");
    }

    // POST: /Account/Logout
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Account"); // redirect to login page
    }

    // Helper method to sign in user via cookie authentication
    private async Task SignInUser(string email, string provider)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, email),
            new Claim("Provider", provider)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    // Other [Authorize] actions for setting password and updating profile (unchanged)...
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> SetPassword()
    {
        var email = User.Identity?.Name!;
        return View(new SetPasswordViewModel { Email = email });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (model.NewPassword != model.ConfirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match.");
            return View(model);
        }

        var success = await _userService.UpdatePasswordAsync(model.Email, model.NewPassword);
        if (!success)
        {
            ModelState.AddModelError("", "Failed to update password.");
            return View(model);
        }

        ViewBag.Message = "Password updated successfully.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> UpdateProfile()
    {
        var email = User.Identity?.Name!;
        var user = await _userService.GetByEmailAsync(email);
        if (user == null) return NotFound();

        return View(new UpdateProfileViewModel
        {
            FullName = user.FullName,
            Phone = user.Phone,
            Address = user.Address
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
    {
        var email = User.Identity?.Name!;
        var success = await _userService.UpdateProfileAsync(email, model.FullName, model.Phone, model.Address);
        if (!success)
        {
            ModelState.AddModelError("", "Failed to update profile.");
            return View(model);
        }

        ViewBag.Message = "Profile updated successfully.";
        return RedirectToAction("Index", "Home");
    }
}
