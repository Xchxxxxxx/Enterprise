import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/login/index.vue'),
      meta: { guest: true }
    },
    {
      path: '/',
      component: () => import('@/layout/index.vue'),
      redirect: '/dashboard',
      children: [
        {
          path: 'dashboard',
          name: 'Dashboard',
          component: () => import('@/views/dashboard/index.vue'),
          meta: { title: '工作台' }
        },
        {
          path: 'user',
          name: 'User',
          component: () => import('@/views/user/index.vue'),
          meta: { title: '用户管理' }
        }
      ]
    }
  ]
})

router.beforeEach((to, _from, next) => {
  const auth = useAuthStore()
  if (to.meta.guest) {
    if (auth.isAuthenticated) return next('/')
    return next()
  }
  if (!auth.isAuthenticated) return next('/login')
  next()
})

export default router