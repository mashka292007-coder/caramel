using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.UI.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CaramelCloud.UI.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _uow;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailService emailService, IUnitOfWork uow)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _uow = uow;
        }

        [HttpGet]
        public IActionResult Register() =>
            User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var customerRole = _uow.Role.GetByName("Customer");

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.Phone,
                RoleId = customerRole?.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Customer");

            _uow.CustomerProfile.Add(new CustomerProfile
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            });
            await _uow.SaveAsync();

            TempData["Success"] = "Реєстрація успішна! Тепер увійдіть.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Невірний email або пароль");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Акаунт деактивовано");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Акаунт тимчасово заблоковано. Спробуйте пізніше.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Невірний email або пароль");
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var hasPassword = await _userManager.HasPasswordAsync(user);
                if (!hasPassword)
                {
                    TempData["Info"] = "Цей акаунт створено через Google або Facebook. Увійдіть через відповідну платформу або встановіть пароль у профілі.";
                    return RedirectToAction(nameof(Login));
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action(nameof(ResetPassword), "Auth",
                    new { email = model.Email, token }, Request.Scheme)!;

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(
                        model.Email,
                        string.IsNullOrEmpty(user.FirstName) ? "Користувач" : user.FirstName,
                        resetLink);
                }
                catch (Exception ex)
                {
                    TempData["Info"] = $"Помилка відправки листа: {ex.Message}";
                    return RedirectToAction(nameof(Login));
                }
            }

            TempData["Info"] = "Якщо email існує в системі — ви отримаєте листа з інструкціями.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token) =>
            View(new ResetPasswordViewModel { Email = email, Token = token });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Success"] = "Пароль успішно змінено!";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            TempData["Success"] = "Пароль успішно змінено! Увійдіть з новим паролем.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var logins = await _userManager.GetLoginsAsync(user);
            var isGoogleUser = logins.Any(l => l.LoginProvider == "Google");
            var profile = _uow.CustomerProfile.Get(cp => cp.UserId == user.Id);
            var hasPassword = await _userManager.HasPasswordAsync(user);

            return View(new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber,
                BirthDate = profile?.BirthDate,
                Email = user.Email,
                IsGoogleUser = isGoogleUser,
                HasPassword = hasPassword
            });
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.Phone;

            if (!string.IsNullOrEmpty(model.Email) &&
                model.Email != user.Email &&
                !model.Email.EndsWith("@caramelcloud.local"))
            {
                var existingWithEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingWithEmail != null && existingWithEmail.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "Цей email вже використовується іншим акаунтом");
                    return View(model);
                }

                user.Email = model.Email;
                user.UserName = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();
                user.NormalizedUserName = model.Email.ToUpper();
            }

            await _userManager.UpdateAsync(user);

            var profile = _uow.CustomerProfile.Get(
                cp => cp.UserId == user.Id, tracked: true);

            if (profile != null)
            {
                _uow.CustomerProfile.Update(profile);
                _uow.Save();
            }

            TempData["Success"] = "Профіль успішно оновлено!";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult LoginWithGoogle(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null) return RedirectToAction(nameof(Login));

            return await HandleExternalLoginAsync(info, returnUrl);
        }

        [HttpGet]
        public IActionResult LoginWithFacebook(string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(FacebookCallback), "Auth", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Facebook", redirectUrl);
            return Challenge(properties, "Facebook");
        }

        [HttpGet]
        public async Task<IActionResult> FacebookCallback(string? returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null) return RedirectToAction(nameof(Login));

            return await HandleExternalLoginAsync(info, returnUrl);
        }

        private async Task<IActionResult> HandleExternalLoginAsync(
            ExternalLoginInfo info, string? returnUrl)
        {
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false);

            if (result.Succeeded)
                return LocalRedirect(returnUrl ?? "/");

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                email = $"fb_{info.ProviderKey}@caramelcloud.local";

            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;

            var existingUser = await _userManager.FindByEmailAsync(email);
            bool isNewUser = existingUser == null;

            if (existingUser == null)
            {
                var customerRole = _uow.Role.GetByName("Customer");
                existingUser = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    UserName = email,
                    RoleId = customerRole?.Id,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _userManager.CreateAsync(existingUser);
                await _userManager.AddToRoleAsync(existingUser, "Customer");

                _uow.CustomerProfile.Add(new CustomerProfile
                {
                    UserId = existingUser.Id,
                    CreatedAt = DateTime.UtcNow
                });
                await _uow.SaveAsync();
            }

            var logins = await _userManager.GetLoginsAsync(existingUser);
            var alreadyLinked = logins.Any(l =>
                l.LoginProvider == info.LoginProvider &&
                l.ProviderKey == info.ProviderKey);

            if (!alreadyLinked)
                await _userManager.AddLoginAsync(existingUser, info);

            await _signInManager.SignInAsync(existingUser, isPersistent: false);

            if (isNewUser)
            {
                TempData["Info"] = $"Ласкаво просимо, {existingUser.FirstName}! Рекомендуємо встановити пароль для альтернативного входу.";
                return RedirectToAction(nameof(SetPassword));
            }

            return LocalRedirect(returnUrl ?? "/");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetPassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var hasPassword = await _userManager.HasPasswordAsync(user);
            ViewData["HasPassword"] = hasPassword;
            return View();
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {
                TempData["Info"] = "Пароль вже встановлено. Щоб змінити — скористайтесь розділом зміни пароля.";
                ViewData["HasPassword"] = true;
                return View(model);
            }

            var result = await _userManager.AddPasswordAsync(user, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);

                ViewData["HasPassword"] = hasPassword;
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Пароль успішно збережено! Тепер ви можете входити і через email, і через соцмережі.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}