<template>
  <div class="layout">
    <aside class="sidebar">
      <div class="sidebar-brand">
        <div class="brand-icon">
          <svg viewBox="0 0 36 36" fill="none"><rect width="36" height="36" rx="9" fill="url(#sg)"/><defs><linearGradient id="sg" x1="0" y1="0" x2="36" y2="36"><stop stop-color="#818cf8"/><stop offset="1" stop-color="#6366f1"/></linearGradient></defs></svg>
        </div>
        <div class="brand-text">
          <span class="brand-name">EfCore</span>
          <span class="brand-sub">Enterprise</span>
        </div>
      </div>
      <nav class="sidebar-nav">
        <router-link to="/dashboard" class="nav-item" :class="{ active: $route.path === '/dashboard' }">
          <svg viewBox="0 0 20 20" fill="currentColor"><path d="M10.707 2.293a1 1 0 00-1.414 0l-7 7a1 1 0 001.414 1.414L4 10.414V17a1 1 0 001 1h2a1 1 0 001-1v-2a1 1 0 011-1h2a1 1 0 011 1v2a1 1 0 001 1h2a1 1 0 001-1v-6.586l.293.293a1 1 0 001.414-1.414l-7-7z"/></svg>
          <span>工作台</span>
        </router-link>
        <router-link to="/user" class="nav-item" :class="{ active: $route.path.startsWith('/user') }">
          <svg viewBox="0 0 20 20" fill="currentColor"><path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z"/></svg>
          <span>用户管理</span>
        </router-link>
      </nav>
      <div class="sidebar-footer">
        <div class="user-info">
          <div class="user-avatar">{{ auth.userName.charAt(0).toUpperCase() }}</div>
          <div class="user-detail">
            <span class="user-name">{{ auth.userName }}</span>
            <span class="user-role">{{ auth.roles.includes('admin') ? '管理员' : '用户' }}</span>
          </div>
        </div>
        <button class="btn-logout" @click="handleLogout" title="退出登录">
          <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3 3a1 1 0 00-1 1v12a1 1 0 102 0V4a1 1 0 00-1-1zm10.293 9.293a1 1 0 001.414 1.414l3-3a1 1 0 000-1.414l-3-3a1 1 0 10-1.414 1.414L14.586 9H7a1 1 0 100 2h7.586l-1.293 1.293z" clip-rule="evenodd"/></svg>
        </button>
      </div>
    </aside>
    <main class="main-content">
      <header class="topbar">
        <h2 class="page-title">{{ $route.meta.title || '' }}</h2>
        <div class="topbar-actions">
          <button class="btn-icon" @click="toggleTheme" title="切换主题">
            <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4 8a4 4 0 11-8 0 4 4 0 018 0zm-.464 4.95l.707.707a1 1 0 001.414-1.414l-.707-.707a1 1 0 00-1.414 1.414zm2.12-10.607a1 1 0 010 1.414l-.706.707a1 1 0 11-1.414-1.414l.707-.707a1 1 0 011.414 0zM17 11a1 1 0 100-2h-1a1 1 0 100 2h1zm-7 4a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1zM5.05 6.464A1 1 0 106.465 5.05l-.708-.707a1 1 0 00-1.414 1.414l.707.707zm1.414 8.486l-.707.707a1 1 0 01-1.414-1.414l.707-.707a1 1 0 011.414 1.414zM4 11a1 1 0 100-2H3a1 1 0 000 2h1z" clip-rule="evenodd"/></svg>
          </button>
        </div>
      </header>
      <div class="content-area">
        <router-view v-slot="{ Component }">
          <transition name="slide" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

function handleLogout() {
  auth.logout()
  router.push('/login')
}

function toggleTheme() {
  document.documentElement.classList.toggle('dark')
}
</script>

<style scoped>
.layout { display: flex; height: 100%; }
.sidebar { width: 240px; background: var(--bg-sidebar); display: flex; flex-direction: column; flex-shrink: 0; }
.sidebar-brand { display: flex; align-items: center; gap: 12px; padding: 20px; border-bottom: 1px solid rgba(255,255,255,.08); }
.brand-icon { width: 36px; height: 36px; flex-shrink: 0; }
.brand-icon svg { width: 100%; height: 100%; }
.brand-text { display: flex; flex-direction: column; }
.brand-name { font-size: 16px; font-weight: 700; color: #fff; }
.brand-sub { font-size: 11px; color: rgba(255,255,255,.5); }
.sidebar-nav { flex: 1; padding: 12px 8px; display: flex; flex-direction: column; gap: 2px; }
.nav-item { display: flex; align-items: center; gap: 10px; padding: 10px 14px; border-radius: 8px; color: rgba(255,255,255,.6); font-size: 14px; font-weight: 500; transition: var(--transition); text-decoration: none; }
.nav-item:hover { color: #fff; background: rgba(255,255,255,.08); }
.nav-item.active { color: #fff; background: rgba(99,102,241,.3); }
.nav-item svg { width: 20px; height: 20px; flex-shrink: 0; }
.sidebar-footer { padding: 16px; border-top: 1px solid rgba(255,255,255,.08); }
.user-info { display: flex; align-items: center; gap: 10px; }
.user-avatar { width: 36px; height: 36px; border-radius: 50%; background: linear-gradient(135deg, var(--primary), var(--secondary)); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 14px; flex-shrink: 0; }
.user-detail { display: flex; flex-direction: column; overflow: hidden; }
.user-name { color: #fff; font-size: 13px; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.user-role { color: rgba(255,255,255,.5); font-size: 11px; }
.btn-logout { margin-left: auto; width: 32px; height: 32px; border: none; background: rgba(255,255,255,.1); border-radius: 8px; color: rgba(255,255,255,.5); cursor: pointer; display: flex; align-items: center; justify-content: center; transition: var(--transition); }
.btn-logout:hover { background: rgba(239,68,68,.3); color: #fca5a5; }
.btn-logout svg { width: 16px; height: 16px; }
.main-content { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
.topbar { display: flex; align-items: center; justify-content: space-between; padding: 16px 28px; background: var(--bg-card); border-bottom: 1px solid var(--border); }
.page-title { font-size: 18px; font-weight: 700; }
.btn-icon { width: 36px; height: 36px; border: 1px solid var(--border); background: var(--bg); border-radius: 8px; cursor: pointer; display: flex; align-items: center; justify-content: center; color: var(--text-secondary); transition: var(--transition); }
.btn-icon:hover { background: var(--border); }
.btn-icon svg { width: 18px; height: 18px; }
.content-area { flex: 1; overflow: auto; padding: 24px 28px; }
</style>