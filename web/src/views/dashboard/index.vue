<template>
  <div class="dashboard">
    <div class="stats-grid">
      <div class="stat-card" v-for="stat in stats" :key="stat.label">
        <div class="stat-icon" :style="{ background: stat.bg }">
          <svg viewBox="0 0 20 20" fill="currentColor" v-html="stat.icon"></svg>
        </div>
        <div class="stat-info">
          <span class="stat-value">{{ stat.value }}</span>
          <span class="stat-label">{{ stat.label }}</span>
        </div>
      </div>
    </div>
    <div class="cards-row">
      <div class="card chart-card">
        <h3>近7日登录趋势</h3>
        <div class="chart">
          <div class="bar-chart">
            <div v-for="(d, i) in statsData?.dailyStats || []" :key="i" class="bar-col">
              <div class="bar-group">
                <div class="bar bar-login" :style="{ height: getBarHeight(d.loginCount, maxLogin) }" :title="`登录: ${d.loginCount}`"></div>
                <div class="bar bar-new" :style="{ height: getBarHeight(d.newUserCount, maxLogin) }" :title="`新增: ${d.newUserCount}`"></div>
              </div>
              <span class="bar-label">{{ d.date.slice(5) }}</span>
            </div>
          </div>
          <div class="chart-legend">
            <span><i class="dot" style="background:var(--primary)"></i> 登录</span>
            <span><i class="dot" style="background:var(--secondary)"></i> 新增</span>
          </div>
        </div>
      </div>
      <div class="card quick-card">
        <h3>快速操作</h3>
        <div class="quick-actions">
          <button class="quick-btn" @click="$router.push('/user')">
            <svg viewBox="0 0 20 20" fill="currentColor"><path d="M8 9a3 3 0 100-6 3 3 0 000 6zM8 11a6 6 0 016 6H2a6 6 0 016-6zM16 7a1 1 0 10-2 0v1h-1a1 1 0 100 2h1v1a1 1 0 102 0v-1h1a1 1 0 100-2h-1V7z"/></svg>
            <span>新增用户</span>
          </button>
          <button class="quick-btn" @click="$router.push('/user')">
            <svg viewBox="0 0 20 20" fill="currentColor"><path d="M5 3a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2V5a2 2 0 00-2-2H5zM5 11a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2v-2a2 2 0 00-2-2H5zM11 5a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V5zM11 13a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"/></svg>
            <span>用户列表</span>
          </button>
          <button class="quick-btn" @click="$router.push('/user')">
            <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/></svg>
            <span>登录日志</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { userApi, type UserStatsDto } from '@/api/user'

const statsData = ref<UserStatsDto | null>(null)

const stats = computed(() => [
  { label: '用户总数', value: statsData.value?.totalUsers || 0, bg: 'linear-gradient(135deg, #6366f1, #818cf8)', icon: '<path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z"/>' },
  { label: '活跃用户', value: statsData.value?.activeUsers || 0, bg: 'linear-gradient(135deg, #10b981, #34d399)', icon: '<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>' },
  { label: '今日在线', value: statsData.value?.onlineToday || 0, bg: 'linear-gradient(135deg, #3b82f6, #60a5fa)', icon: '<path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/>' },
  { label: '今日新增', value: statsData.value?.newToday || 0, bg: 'linear-gradient(135deg, #f59e0b, #fbbf24)', icon: '<path d="M8 11a3 3 0 100-6 3 3 0 000 6zM8 13a5 5 0 015 5H3a5 5 0 015-5zM16 7a1 1 0 10-2 0v1h-1a1 1 0 100 2h1v1a1 1 0 102 0v-1h1a1 1 0 100-2h-1V7z"/>' },
])

const maxLogin = computed(() => {
  if (!statsData.value?.dailyStats?.length) return 1
  return Math.max(...statsData.value.dailyStats.map(d => Math.max(d.loginCount, d.newUserCount)), 1)
})

function getBarHeight(val: number, max: number) {
  return max > 0 ? (val / max * 100) + '%' : '0%'
}

onMounted(async () => {
  try {
    statsData.value = await userApi.getStats()
  } catch { /* ignore */ }
})
</script>

<style scoped>
.stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; margin-bottom: 24px; }
.stat-card { background: var(--bg-card); border-radius: var(--radius); padding: 20px; box-shadow: var(--shadow); display: flex; align-items: center; gap: 16px; transition: var(--transition); }
.stat-card:hover { transform: translateY(-2px); box-shadow: var(--shadow-lg); }
.stat-icon { width: 48px; height: 48px; border-radius: 12px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
.stat-icon svg { width: 24px; height: 24px; color: #fff; }
.stat-info { display: flex; flex-direction: column; }
.stat-value { font-size: 24px; font-weight: 700; }
.stat-label { font-size: 13px; color: var(--text-muted); }
.cards-row { display: grid; grid-template-columns: 2fr 1fr; gap: 20px; }
.card { background: var(--bg-card); border-radius: var(--radius); padding: 24px; box-shadow: var(--shadow); }
.card h3 { font-size: 15px; font-weight: 600; margin-bottom: 20px; }
.chart { display: flex; flex-direction: column; gap: 12px; }
.bar-chart { display: flex; align-items: flex-end; gap: 12px; height: 180px; padding: 0 4px; }
.bar-col { flex: 1; display: flex; flex-direction: column; align-items: center; gap: 6px; height: 100%; }
.bar-group { flex: 1; width: 100%; display: flex; align-items: flex-end; justify-content: center; gap: 3px; }
.bar { width: 14px; border-radius: 4px 4px 0 0; transition: var(--transition); min-height: 2px; }
.bar-login { background: var(--primary); }
.bar-new { background: var(--secondary); }
.bar-label { font-size: 11px; color: var(--text-muted); }
.chart-legend { display: flex; gap: 16px; justify-content: center; font-size: 12px; color: var(--text-secondary); }
.dot { display: inline-block; width: 8px; height: 8px; border-radius: 50%; margin-right: 4px; }
.quick-actions { display: flex; flex-direction: column; gap: 10px; }
.quick-btn { display: flex; align-items: center; gap: 10px; padding: 14px 16px; border: 1px solid var(--border); border-radius: var(--radius-sm); background: var(--bg); cursor: pointer; transition: var(--transition); font-size: 14px; color: var(--text); }
.quick-btn:hover { border-color: var(--primary); background: rgba(99,102,241,.05); color: var(--primary); }
.quick-btn svg { width: 20px; height: 20px; }
</style>