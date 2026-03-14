namespace backend.DTOs
{
    public class AuthDTO
    {
        //-------REQUESTS---------
        public class RegisterDTO
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string? AvatarUrl { get; set; }
            public string Password { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public DateTime DateOfBirth { get; set; }
            public string? Gender { get; set; }
        }

        public class LoginDTO
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RefreshTokenDTO
        {
            public string RefreshToken { get; set; } = string.Empty;
        }

        public class ForgotPasswordDTO
        {
            public string Email { get; set; } = string.Empty;
        }

        public class ResetPasswordDTO
        {
            public string Email { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        //-------------RESPONSES-------------
        public class AuthResponseDTO
        {
            public string Token { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public int Score { get; set; }
            public decimal UnpaidFinesTotal { get; set; }
            public DateTime ExpiresAt { get; set; }
        }



    }
}
