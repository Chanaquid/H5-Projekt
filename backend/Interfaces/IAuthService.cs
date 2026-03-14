using backend.DTOs;

namespace backend.Interfaces
{
    public interface IAuthService
    {
        Task<AuthDTO.AuthResponseDTO> RegisterAsync(AuthDTO.RegisterDTO dto);
        Task<AuthDTO.AuthResponseDTO> LoginAsync(AuthDTO.LoginDTO dto);
        Task<AuthDTO.AuthResponseDTO> RefreshTokenAsync(AuthDTO.RefreshTokenDTO dto);
        Task LogoutAsync(string userId);
        Task<bool> ConfirmEmailAsync(string userId, string token);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(AuthDTO.ResetPasswordDTO dto);
        Task ResendConfirmationEmailAsync(string email);

    }
}
