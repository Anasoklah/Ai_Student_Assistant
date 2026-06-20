

namespace Authentication.Dtos.ResetPassword;

public record ChangePasswordDto
{
    public string oldPassword {get; init;} = default!;
    public string newPassword {get;init;} = default!;
}
