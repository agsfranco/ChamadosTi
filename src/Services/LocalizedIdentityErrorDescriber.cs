using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace ChamadosTi.Services
{
    public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
    {
        private readonly IStringLocalizer<IdentityUser> _localizer;

        public LocalizedIdentityErrorDescriber(IStringLocalizer<IdentityUser> localizer)
        {
            _localizer = localizer;
        }

        public override IdentityError PasswordTooShort(int length) => new() {
            Code = nameof(PasswordTooShort),
            Description = string.Format(_localizer["PasswordTooShort"], length) 
        };

        public override IdentityError PasswordMismatch() => new() {
            Code = nameof(PasswordMismatch),
            Description = _localizer["PasswordMismatch"]
        };
    }
}
