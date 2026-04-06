using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SV22T1020105.Shop
{
    /// <summary>
    /// Thông tin khách hàng được lưu trong phiên đăng nhập (cookie)
    /// </summary>
    public class CustomerUserData
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";

        private List<Claim> Claims => new List<Claim>
        {
            new Claim(nameof(UserId), UserId),
            new Claim(nameof(UserName), UserName),
            new Claim(nameof(DisplayName), DisplayName),
            new Claim(nameof(Email), Email)
        };

        public ClaimsPrincipal CreatePrincipal()
        {
            var identity = new ClaimsIdentity(Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
    }

    /// <summary>
    /// Extension đọc thông tin khách hàng từ ClaimsPrincipal
    /// </summary>
    public static class CustomerUserExtensions
    {
        public static CustomerUserData? GetCustomerData(this ClaimsPrincipal principal)
        {
            try
            {
                if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
                    return null;

                return new CustomerUserData
                {
                    UserId = principal.FindFirstValue(nameof(CustomerUserData.UserId)) ?? "",
                    UserName = principal.FindFirstValue(nameof(CustomerUserData.UserName)) ?? "",
                    DisplayName = principal.FindFirstValue(nameof(CustomerUserData.DisplayName)) ?? "",
                    Email = principal.FindFirstValue(nameof(CustomerUserData.Email)) ?? ""
                };
            }
            catch { return null; }
        }
    }
}
