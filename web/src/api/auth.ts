import http from './index'

export interface LoginRequest {
  username: string
  password: string
}

export interface UserInfo {
  id: number
  username: string
  nickname?: string
  email?: string
  phone?: string
  avatar?: string
  roles: string[]
  permissions: string[]
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: UserInfo
}

export const authApi = {
  login(data: LoginRequest) {
    return http.post<any, LoginResponse>('/auth/login', data)
  },
  refreshToken(data: { accessToken: string; refreshToken: string }) {
    return http.post<any, LoginResponse>('/auth/refresh', data)
  },
  logout(refreshToken: string) {
    return http.post('/auth/logout', { refreshToken })
  },
  changePassword(data: { oldPassword: string; newPassword: string }) {
    return http.post('/auth/change-password', data)
  }
}