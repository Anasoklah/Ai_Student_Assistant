

namespace Authentication.Dtos.ResetPassword;

public record ResetPasswordDto
{
    public Guid userid{get;init;} = default!;
    public string token{get;init;} = default!;
    public string newPassword{get;init;} = default!;
}
