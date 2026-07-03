import http from './index'

export interface UserDto {
  id: number
  username: string
  nickname?: string
  email?: string
  phone?: string
  avatar?: string
  isEnabled: boolean
  emailConfirmed: boolean
  phoneConfirmed: boolean
  twoFactorEnabled: boolean
  lastLoginTime?: string
  lastLoginIp?: string
  loginFailedCount: number
  lockoutEnd?: string
  createdTime: string
  roles: string[]
  roleIds: number[]
}

export interface CreateUserRequest {
  username: string
  password: string
  nickname?: string
  email?: string
  phone?: string
  roleIds: number[]
}

export interface UpdateUserRequest {
  id: number
  nickname?: string
  email?: string
  phone?: string
  isEnabled?: boolean
  roleIds?: number[]
}

export interface UserQueryRequest {
  username?: string
  nickname?: string
  email?: string
  phone?: string
  isEnabled?: boolean
  pageIndex: number
  pageSize: number
  sortField?: string
  sortDesc?: boolean
}

export interface PagedResult<T> {
  items: T[]
  total: number
  pageIndex: number
  pageSize: number
}

export interface LoginLogDto {
  id: number
  userId?: number
  username?: string
  ipAddress?: string
  userAgent?: string
  deviceInfo?: string
  location?: string
  isSuccess: boolean
  failReason?: string
  loginTime: string
}

export interface UserStatsDto {
  totalUsers: number
  activeUsers: number
  disabledUsers: number
  lockedUsers: number
  onlineToday: number
  newToday: number
  dailyStats: { date: string; loginCount: number; newUserCount: number }[]
}

export const userApi = {
  getPage(params: UserQueryRequest) {
    return http.get<any, PagedResult<UserDto>>('/user', { params })
  },
  getById(id: number) {
    return http.get<any, UserDto>(`/user/${id}`)
  },
  create(data: CreateUserRequest) {
    return http.post<any, UserDto>('/user', data)
  },
  update(id: number, data: UpdateUserRequest) {
    return http.put<any, UserDto>(`/user/${id}`, data)
  },
  delete(id: number) {
    return http.delete(`/user/${id}`)
  },
  resetPassword(data: { userId: number; newPassword: string }) {
    return http.post('/user/reset-password', data)
  },
  assignRoles(data: { userId: number; roleIds: number[] }) {
    return http.post('/user/assign-roles', data)
  },
  batchDelete(ids: number[]) {
    return http.post('/user/batch-delete', { ids })
  },
  batchEnable(ids: number[], isEnabled: boolean) {
    return http.post('/user/batch-enable', { ids, isEnabled })
  },
  unlock(id: number) {
    return http.post(`/user/${id}/unlock`)
  },
  getLoginLogs(params: any) {
    return http.get<any, PagedResult<LoginLogDto>>('/user/login-logs', { params })
  },
  getStats() {
    return http.get<any, UserStatsDto>('/user/stats')
  },
  checkUsername(username: string, excludeId?: number) {
    return http.get('/user/check-username', { params: { username, excludeId } })
  }
}