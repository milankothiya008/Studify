using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartLearningPlatform.Models.Users;
using SmartLearningPlatform.Services;
using SmartLearningPlatform.ViewModels;

namespace SmartLearningPlatform.Controllers;

/// <summary>
/// Sign-in and sign-out, on top of ASP.NET Core Identity.
/// </summary>
/// <remarks>
/// This replaces <c>POST /Home/SetActingUser</c>, which used to name the account
/// the audit columns should credit. That account is now simply whoever signed
/// in — <c>ClaimsCurrentUserAccessor</c> reads it from the Identity cookie — so
/// the audit trail records a fact rather than a choice.
/// </remarks>
[AllowAnonymous]
public class AccountController : Controller
{
    /// <summary>Deliberately vague: a wrong email and a wrong password look alike.</summary>
    private const string InvalidCredentials = "That email and password do not match an account.";

    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ICurrentUserAccessor currentUser,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _currentUser = currentUser;
        _logger = logger;
    }

    // GET: /Account/Login
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToLocal(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel form)
    {
        if (!ModelState.IsValid) return View(form);

        // Identity looks users up by normalised email; the global soft-delete
        // filter means a retired account is simply not found.
        var user = await _userManager.FindByEmailAsync(form.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, InvalidCredentials);
            return View(form);
        }

        // Signing in writes to the user row — the failed-attempt counter either
        // way, the lockout stamp on the last failure. No cookie exists yet, so
        // tell the audit interceptor whose row it is.
        _currentUser.UseUserForRequest(user.Id);

        var result = await _signInManager.PasswordSignInAsync(
            user, form.Password, form.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} signed in.", user.Id);
            return RedirectToLocal(form.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            // account_locked_until — the column the port already had, now written
            // by Identity's lockout bookkeeping.
            ModelState.AddModelError(
                string.Empty,
                user.AccountLockedUntil is { } until
                    ? $"Too many failed attempts. This account is locked until {until:g} UTC."
                    : "Too many failed attempts. This account is locked.");

            _logger.LogWarning("Sign-in blocked: user {UserId} is locked out.", user.Id);
            return View(form);
        }

        if (result.IsNotAllowed)
        {
            // The UserStatus gate, enforced by ApplicationSignInManager.
            ModelState.AddModelError(
                string.Empty,
                $"This account cannot sign in while its status is {user.Status}.");

            _logger.LogWarning(
                "Sign-in refused: user {UserId} has status {Status}.", user.Id, user.Status);
            return View(form);
        }

        ModelState.AddModelError(string.Empty, InvalidCredentials);
        return View(form);
    }

    // POST: /Account/Logout
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Signed out.";
        return RedirectToAction(nameof(Login));
    }

    // GET: /Account/AccessDenied
    public IActionResult AccessDenied(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl!)
            : RedirectToAction(nameof(HomeController.Index), "Home");
}
