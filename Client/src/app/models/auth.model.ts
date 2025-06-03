export interface ILoginRequest {
    email: string
    password: string
}
export interface ILoginResponse {
    accessToken: string
    refreshToken: string
}
export interface IJwtPayload {
    UserId: string;
    Email: string;
    RoleId: string;
    Role: string;
    Name: string;
    exp: string;
  }
  