import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type UserInfo } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token') || '')
  const refreshToken = ref(localStorage.getItem('refreshToken') || '')
  const user = ref<UserInfo | null>(JSON.parse(localStorage.getItem('user') || 'null'))

  const isAuthenticated = computed(() => !!token.value)
  const userName = computed(() => user.value?.nickname || user.value?.username || '')
  const roles = computed(() => user.value?.roles || [])
  const permissions = computed(() => user.value?.permissions || [])

  function hasPermission(perm: string) {
    return permissions.value.includes(perm) || roles.value.includes('admin')
  }

  async function login(username: string, password: string) {
    const res = await authApi.login({ username, password })
    token.value = res.accessToken
    refreshToken.value = res.refreshToken
    user.value = res.user
    localStorage.setItem('token', res.accessToken)
    localStorage.setItem('refreshToken', res.refreshToken)
    localStorage.setItem('user', JSON.stringify(res.user))
  }

  async function refresh() {
    try {
      const res = await authApi.refreshToken({
        accessToken: token.value,
        refreshToken: refreshToken.value
      })
      token.value = res.accessToken
      refreshToken.value = res.refreshToken
      localStorage.setItem('token', res.accessToken)
      localStorage.setItem('refreshToken', res.refreshToken)
    } catch {
      logout()
    }
  }

  function logout() {
    if (refreshToken.value) {
      authApi.logout(refreshToken.value).catch(() => {})
    }
    token.value = ''
    refreshToken.value = ''
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('user')
  }

  return { token, refreshToken, user, isAuthenticated, userName, roles, permissions, hasPermission, login, refresh, logout }
})