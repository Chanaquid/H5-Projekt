export namespace AuthDTO {
  //Requests
  export interface RegisterDTO {
    fullName: string;
    email: string;
    username: string;
    avatarUrl?: string;
    password: string;
    address: string;
    latitude?: number;
    longitude?: number;
    dateOfBirth: string; //ISO date string
    gender?: string;
  }
 
  export interface LoginDTO {
    email: string;
    password: string;
  }
 
  export interface RefreshTokenDTO {
    refreshToken: string;
  }
 
  export interface ForgotPasswordDTO {
    email: string;
  }
 
  export interface ResetPasswordDTO {
    email: string;
    token: string;
    newPassword: string;
  }
 
  //Responses
  export interface AuthResponseDTO {
    token: string;
    refreshToken: string;
    userId: string;
    fullName: string;
    username: string;
    email: string;
    role: string;
    score: number;
    unpaidFinesTotal: number;
    expiresAt: string; //ISO date string
  }
}