<template>
  <div class="login-page">
    <div class="login-bg">
      <div class="bg-shape shape-1"></div>
      <div class="bg-shape shape-2"></div>
      <div class="bg-shape shape-3"></div>
    </div>
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <div class="logo-icon">
            <svg viewBox="0 0 40 40" fill="none"><rect width="40" height="40" rx="10" fill="url(#lg)"/><defs><linearGradient id="lg" x1="0" y1="0" x2="40" y2="40"><stop stop-color="#6366f1"/><stop offset="1" stop-color="#ec4899"/></linearGradient></defs></svg>
          </div>
          <h1>EfCore.Enterprise</h1>
          <p>企业级高性能开发框架</p>
        </div>
        <form @submit.prevent="handleLogin" class="login-form">
          <div class="form-group">
            <label>用户名</label>
            <div class="input-wrapper">
              <svg class="input-icon" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/></svg>
              <input v-model="form.username" type="text" placeholder="请输入用户名" autocomplete="username" />
            </div>
          </div>
          <div class="form-group">
            <label>密码</label>
            <div class="input-wrapper">
              <svg class="input-icon" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd"/></svg>
              <input v-model="form.password" :type="showPwd ? 'text' : 'password'" placeholder="请输入密码" autocomplete="current-password" />
              <button type="button" class="toggle-pwd" @click="showPwd = !showPwd">
                <svg v-if="!showPwd" viewBox="0 0 20 20" fill="currentColor"><path d="M10 12a2 2 0 100-4 2 2 0 000 4z"/><path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"/></svg>
                <svg v-else viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074l-1.78-1.781zm4.261 4.26l1.514 1.515a2.003 2.003 0 012.45 2.45l1.514 1.514a4 4 0 00-5.478-5.478z" clip-rule="evenodd"/><path d="M12.454 16.697L9.75 13.992a4 4 0 01-3.742-3.741L2.335 6.578A9.98 9.98 0 00.458 10c1.274 4.057 5.065 7 9.542 7 .847 0 1.669-.105 2.454-.303z"/></svg>
              </button>
            </div>
          </div>
          <div v-if="error" class="error-msg">
            <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>
            {{ error }}
          </div>
          <button type="submit" class="btn-login" :disabled="loading">
            <span v-if="loading" class="spinner"></span>
            {{ loading ? '登录中...' : '登 录' }}
          </button>
          <div class="login-tip">
            <span>默认账号: admin / Admin@123456</span>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()
const form = reactive({ username: '', password: '' })
const loading = ref(false)
const error = ref('')
const showPwd = ref(false)

async function handleLogin() {
  if (!form.username || !form.password) {
    error.value = '请输入用户名和密码'
    return
  }
  loading.value = true
  error.value = ''
  try {
    await auth.login(form.username, form.password)
    router.push('/')
  } catch (e: any) {
    error.value = e.message || '登录失败'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page { height: 100%; display: flex; align-items: center; justify-content: center; position: relative; overflow: hidden; }
.login-bg { position: absolute; inset: 0; z-index: 0; }
.bg-shape { position: absolute; border-radius: 50%; filter: blur(80px); opacity: .3; }
.shape-1 { width: 500px; height: 500px; background: var(--primary); top: -150px; right: -100px; animation: float1 8s ease-in-out infinite; }
.shape-2 { width: 400px; height: 400px; background: var(--secondary); bottom: -100px; left: -100px; animation: float2 10s ease-in-out infinite; }
.shape-3 { width: 300px; height: 300px; background: var(--primary-light); top: 50%; left: 50%; transform: translate(-50%,-50%); animation: float3 12s ease-in-out infinite; }
@keyframes float1 { 0%,100% { transform: translate(0,0) scale(1); } 50% { transform: translate(-30px,30px) scale(1.05); } }
@keyframes float2 { 0%,100% { transform: translate(0,0) scale(1); } 50% { transform: translate(30px,-30px) scale(1.08); } }
@keyframes float3 { 0%,100% { transform: translate(-50%,-50%) scale(1); } 50% { transform: translate(-45%,-55%) scale(1.1); } }
.login-container { position: relative; z-index: 1; width: 100%; max-width: 420px; padding: 20px; }
.login-card { background: rgba(255,255,255,.9); backdrop-filter: blur(20px); border-radius: 20px; padding: 40px; box-shadow: 0 25px 50px -12px rgba(0,0,0,.25); border: 1px solid rgba(255,255,255,.5); }
.login-header { text-align: center; margin-bottom: 32px; }
.logo-icon { width: 48px; height: 48px; margin: 0 auto 16px; }
.logo-icon svg { width: 100%; height: 100%; }
.login-header h1 { font-size: 22px; font-weight: 700; background: linear-gradient(135deg, var(--primary), var(--secondary)); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
.login-header p { font-size: 13px; color: var(--text-muted); margin-top: 4px; }
.form-group { margin-bottom: 20px; }
.form-group label { display: block; font-size: 13px; font-weight: 600; color: var(--text-secondary); margin-bottom: 6px; }
.input-wrapper { position: relative; }
.input-icon { position: absolute; left: 14px; top: 50%; transform: translateY(-50%); width: 18px; height: 18px; color: var(--text-muted); }
.input-wrapper input { width: 100%; padding: 12px 14px 12px 42px; border: 2px solid var(--border); border-radius: var(--radius-sm); font-size: 14px; transition: var(--transition); background: var(--bg); outline: none; }
.input-wrapper input:focus { border-color: var(--primary); box-shadow: 0 0 0 3px rgba(99,102,241,.15); }
.toggle-pwd { position: absolute; right: 12px; top: 50%; transform: translateY(-50%); width: 20px; height: 20px; border: none; background: none; cursor: pointer; color: var(--text-muted); padding: 0; }
.toggle-pwd:hover { color: var(--text-secondary); }
.error-msg { display: flex; align-items: center; gap: 6px; padding: 10px 14px; background: #fef2f2; border: 1px solid #fecaca; border-radius: var(--radius-sm); color: var(--danger); font-size: 13px; margin-bottom: 16px; }
.error-msg svg { width: 18px; height: 18px; flex-shrink: 0; }
.btn-login { width: 100%; padding: 13px; background: linear-gradient(135deg, var(--primary), var(--primary-dark)); color: #fff; border: none; border-radius: var(--radius-sm); font-size: 15px; font-weight: 600; cursor: pointer; transition: var(--transition); display: flex; align-items: center; justify-content: center; gap: 8px; }
.btn-login:hover:not(:disabled) { transform: translateY(-1px); box-shadow: 0 4px 12px rgba(99,102,241,.4); }
.btn-login:disabled { opacity: .7; cursor: not-allowed; }
.spinner { width: 18px; height: 18px; border: 2px solid rgba(255,255,255,.3); border-top-color: #fff; border-radius: 50%; animation: spin .6s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
.login-tip { text-align: center; margin-top: 20px; font-size: 12px; color: var(--text-muted); padding: 8px; background: var(--bg); border-radius: var(--radius-sm); }
</style>