<template>
  <div class="user-page">
    <div class="page-card">
      <div class="page-toolbar">
        <div class="search-box">
          <svg class="search-icon" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd"/></svg>
          <input v-model="query.username" placeholder="搜索用户名..." @keyup.enter="search" />
        </div>
        <div class="filter-group">
          <select v-model="query.isEnabled" @change="search">
            <option :value="undefined">全部状态</option>
            <option :value="true">启用</option>
            <option :value="false">禁用</option>
          </select>
        </div>
        <button class="btn btn-primary" @click="openCreate">
          <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd"/></svg>
          新增用户
        </button>
        <button class="btn btn-danger-outline" :disabled="selectedIds.length === 0" @click="batchDelete">
          <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>
          批量删除
        </button>
      </div>

      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th style="width:40px"><input type="checkbox" :checked="isAllSelected" @change="toggleAll" /></th>
              <th>用户名</th>
              <th>昵称</th>
              <th>邮箱</th>
              <th>手机号</th>
              <th>状态</th>
              <th>角色</th>
              <th>最后登录</th>
              <th>创建时间</th>
              <th style="width:180px">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="loading">
              <td colspan="10" class="loading-cell">
                <span class="spinner"></span> 加载中...
              </td>
            </tr>
            <tr v-else-if="list.length === 0">
              <td colspan="10" class="empty-cell">暂无数据</td>
            </tr>
            <tr v-for="user in list" :key="user.id" :class="{ 'row-disabled': !user.isEnabled }">
              <td><input type="checkbox" :checked="selectedIds.includes(user.id)" @change="toggleSelect(user.id)" /></td>
              <td>
                <div class="user-cell">
                  <div class="user-avatar-sm">{{ user.username.charAt(0).toUpperCase() }}</div>
                  <span class="user-name">{{ user.username }}</span>
                </div>
              </td>
              <td>{{ user.nickname || '-' }}</td>
              <td>{{ user.email || '-' }}</td>
              <td>{{ user.phone || '-' }}</td>
              <td>
                <span class="badge" :class="user.isEnabled ? 'badge-success' : 'badge-danger'">
                  <span class="badge-dot"></span>
                  {{ user.isEnabled ? '启用' : '禁用' }}
                </span>
              </td>
              <td>
                <div class="role-tags">
                  <span v-for="role in user.roles" :key="role" class="role-tag">{{ role }}</span>
                </div>
              </td>
              <td>{{ user.lastLoginTime ? formatTime(user.lastLoginTime) : '从未登录' }}</td>
              <td>{{ formatTime(user.createdTime) }}</td>
              <td>
                <div class="action-btns">
                  <button class="btn-sm btn-primary-ghost" @click="openEdit(user)">编辑</button>
                  <button class="btn-sm btn-warning-ghost" @click="openResetPwd(user)">重置密码</button>
                  <button class="btn-sm btn-danger-ghost" @click="deleteUser(user.id)">删除</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination" v-if="total > 0">
        <span class="pagination-info">共 {{ total }} 条</span>
        <div class="pagination-btns">
          <button :disabled="query.pageIndex <= 1" @click="query.pageIndex--; search()">上一页</button>
          <button v-for="p in pages" :key="p" :class="{ active: p === query.pageIndex }" @click="query.pageIndex = p; search()">{{ p }}</button>
          <button :disabled="query.pageIndex * query.pageSize >= total" @click="query.pageIndex++; search()">下一页</button>
        </div>
        <select v-model="query.pageSize" @change="search()">
          <option :value="10">10条/页</option>
          <option :value="20">20条/页</option>
          <option :value="50">50条/页</option>
        </select>
      </div>
    </div>

    <div class="modal-overlay" v-if="showModal" @click.self="closeModal">
      <div class="modal">
        <div class="modal-header">
          <h3>{{ editingUser ? '编辑用户' : '新增用户' }}</h3>
          <button class="modal-close" @click="closeModal">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>用户名 <span class="required">*</span></label>
            <input v-model="form.username" :disabled="!!editingUser" placeholder="字母开头，3-50位" />
          </div>
          <div class="form-group" v-if="!editingUser">
            <label>密码 <span class="required">*</span></label>
            <input v-model="form.password" type="password" placeholder="至少8位，含大小写字母+数字+特殊字符" />
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>昵称</label>
              <input v-model="form.nickname" placeholder="请输入昵称" />
            </div>
            <div class="form-group">
              <label>状态</label>
              <select v-model="form.isEnabled">
                <option :value="true">启用</option>
                <option :value="false">禁用</option>
              </select>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>邮箱</label>
              <input v-model="form.email" type="email" placeholder="请输入邮箱" />
            </div>
            <div class="form-group">
              <label>手机号</label>
              <input v-model="form.phone" placeholder="请输入手机号" />
            </div>
          </div>
          <div v-if="modalError" class="error-msg">{{ modalError }}</div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="closeModal">取消</button>
          <button class="btn btn-primary" @click="saveUser" :disabled="saving">
            <span v-if="saving" class="spinner-sm"></span>
            {{ saving ? '保存中...' : '保存' }}
          </button>
        </div>
      </div>
    </div>

    <div class="modal-overlay" v-if="showPwdModal" @click.self="showPwdModal = false">
      <div class="modal modal-sm">
        <div class="modal-header">
          <h3>重置密码 - {{ pwdTarget?.username }}</h3>
          <button class="modal-close" @click="showPwdModal = false">&times;</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>新密码 <span class="required">*</span></label>
            <input v-model="resetPwdForm.newPassword" type="password" placeholder="至少8位，含大小写字母+数字+特殊字符" />
          </div>
          <div v-if="pwdError" class="error-msg">{{ pwdError }}</div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showPwdModal = false">取消</button>
          <button class="btn btn-primary" @click="doResetPwd" :disabled="saving">确认重置</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { userApi, type UserDto, type UserQueryRequest } from '@/api/user'

const list = ref<UserDto[]>([])
const total = ref(0)
const loading = ref(false)
const selectedIds = ref<number[]>([])
const query = reactive<UserQueryRequest>({ pageIndex: 1, pageSize: 10 })

const showModal = ref(false)
const showPwdModal = ref(false)
const editingUser = ref<UserDto | null>(null)
const saving = ref(false)
const modalError = ref('')
const pwdError = ref('')
const pwdTarget = ref<UserDto | null>(null)

const form = reactive({ username: '', password: '', nickname: '', email: '', phone: '', isEnabled: true, roleIds: [] as number[] })
const resetPwdForm = reactive({ userId: 0, newPassword: '' })

const isAllSelected = computed(() => list.value.length > 0 && list.value.every(u => selectedIds.value.includes(u.id)))
const pages = computed(() => {
  const totalPages = Math.ceil(total.value / query.pageSize)
  const current = query.pageIndex
  const pages: number[] = []
  for (let i = Math.max(1, current - 2); i <= Math.min(totalPages, current + 2); i++) pages.push(i)
  return pages
})

function formatTime(t: string) {
  return new Date(t).toLocaleString('zh-CN')
}

async function search() {
  loading.value = true
  try {
    const res = await userApi.getPage({ ...query })
    list.value = res.items
    total.value = res.total
  } catch { /* ignore */ }
  finally { loading.value = false }
}

function toggleAll() {
  if (isAllSelected.value) { selectedIds.value = [] }
  else { selectedIds.value = list.value.map(u => u.id) }
}

function toggleSelect(id: number) {
  const idx = selectedIds.value.indexOf(id)
  if (idx >= 0) selectedIds.value.splice(idx, 1)
  else selectedIds.value.push(id)
}

function openCreate() {
  editingUser.value = null
  form.username = ''; form.password = ''; form.nickname = ''; form.email = ''; form.phone = ''; form.isEnabled = true; form.roleIds = []
  modalError.value = ''; showModal.value = true
}

function openEdit(user: UserDto) {
  editingUser.value = user
  form.username = user.username; form.nickname = user.nickname || ''; form.email = user.email || ''; form.phone = user.phone || ''; form.isEnabled = user.isEnabled; form.roleIds = user.roleIds
  modalError.value = ''; showModal.value = true
}

function closeModal() { showModal.value = false }

async function saveUser() {
  if (!form.username) { modalError.value = '请输入用户名'; return }
  if (!editingUser.value && !form.password) { modalError.value = '请输入密码'; return }
  if (!editingUser.value && form.password.length < 8) { modalError.value = '密码至少8位'; return }
  saving.value = true; modalError.value = ''
  try {
    if (editingUser.value) {
      await userApi.update(editingUser.value.id, { id: editingUser.value.id, nickname: form.nickname, email: form.email, phone: form.phone, isEnabled: form.isEnabled })
    } else {
      await userApi.create({ ...form, roleIds: [] })
    }
    closeModal(); search()
  } catch (e: any) { modalError.value = e.message }
  finally { saving.value = false }
}

async function deleteUser(id: number) {
  if (!confirm('确认删除该用户？')) return
  try { await userApi.delete(id); search() }
  catch (e: any) { alert(e.message) }
}

async function batchDelete() {
  if (selectedIds.value.length === 0) return
  if (!confirm(`确认删除选中的 ${selectedIds.value.length} 个用户？`)) return
  try { await userApi.batchDelete(selectedIds.value); selectedIds.value = []; search() }
  catch (e: any) { alert(e.message) }
}

function openResetPwd(user: UserDto) {
  pwdTarget.value = user
  resetPwdForm.userId = user.id
  resetPwdForm.newPassword = ''
  pwdError.value = ''
  showPwdModal.value = true
}

async function doResetPwd() {
  if (resetPwdForm.newPassword.length < 8) { pwdError.value = '密码至少8位'; return }
  saving.value = true; pwdError.value = ''
  try {
    await userApi.resetPassword({ userId: resetPwdForm.userId, newPassword: resetPwdForm.newPassword })
    showPwdModal.value = false
  } catch (e: any) { pwdError.value = e.message }
  finally { saving.value = false }
}

onMounted(search)
</script>

<style scoped>
.user-page { height: 100%; }
.page-card { background: var(--bg-card); border-radius: var(--radius); box-shadow: var(--shadow); overflow: hidden; }
.page-toolbar { display: flex; align-items: center; gap: 12px; padding: 16px 20px; border-bottom: 1px solid var(--border); flex-wrap: wrap; }
.search-box { position: relative; flex: 1; max-width: 280px; }
.search-icon { position: absolute; left: 12px; top: 50%; transform: translateY(-50%); width: 16px; height: 16px; color: var(--text-muted); }
.search-box input { width: 100%; padding: 8px 12px 8px 36px; border: 1px solid var(--border); border-radius: var(--radius-sm); font-size: 13px; outline: none; transition: var(--transition); }
.search-box input:focus { border-color: var(--primary); box-shadow: 0 0 0 2px rgba(99,102,241,.1); }
.filter-group select { padding: 8px 12px; border: 1px solid var(--border); border-radius: var(--radius-sm); font-size: 13px; outline: none; background: var(--bg); cursor: pointer; }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px; border: none; border-radius: var(--radius-sm); font-size: 13px; font-weight: 600; cursor: pointer; transition: var(--transition); }
.btn-primary { background: var(--primary); color: #fff; }
.btn-primary:hover { background: var(--primary-dark); }
.btn-secondary { background: var(--bg); color: var(--text); border: 1px solid var(--border); }
.btn-secondary:hover { background: var(--border); }
.btn-danger-outline { background: #fff; color: var(--danger); border: 1px solid #fecaca; }
.btn-danger-outline:hover:not(:disabled) { background: #fef2f2; }
.btn:disabled { opacity: .5; cursor: not-allowed; }
.btn svg { width: 16px; height: 16px; }
.table-wrap { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; }
th { padding: 12px 16px; text-align: left; font-size: 12px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; background: var(--bg); border-bottom: 1px solid var(--border); white-space: nowrap; }
td { padding: 14px 16px; border-bottom: 1px solid var(--border); font-size: 13px; }
tr:hover td { background: rgba(99,102,241,.02); }
.row-disabled td { opacity: .5; }
.loading-cell, .empty-cell { text-align: center; padding: 40px; color: var(--text-muted); }
.user-cell { display: flex; align-items: center; gap: 10px; }
.user-avatar-sm { width: 32px; height: 32px; border-radius: 50%; background: linear-gradient(135deg, var(--primary), var(--secondary)); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 13px; flex-shrink: 0; }
.user-name { font-weight: 600; }
.badge { display: inline-flex; align-items: center; gap: 5px; padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }
.badge-dot { width: 6px; height: 6px; border-radius: 50%; }
.badge-success { background: #ecfdf5; color: #065f46; }
.badge-success .badge-dot { background: #10b981; }
.badge-danger { background: #fef2f2; color: #991b1b; }
.badge-danger .badge-dot { background: #ef4444; }
.role-tags { display: flex; flex-wrap: wrap; gap: 4px; }
.role-tag { padding: 2px 8px; background: rgba(99,102,241,.1); color: var(--primary); border-radius: 4px; font-size: 11px; font-weight: 600; }
.action-btns { display: flex; gap: 6px; }
.btn-sm { padding: 5px 10px; border: none; border-radius: 4px; font-size: 12px; cursor: pointer; transition: var(--transition); }
.btn-primary-ghost { color: var(--primary); background: rgba(99,102,241,.08); }
.btn-primary-ghost:hover { background: rgba(99,102,241,.15); }
.btn-warning-ghost { color: #d97706; background: rgba(245,158,11,.08); }
.btn-warning-ghost:hover { background: rgba(245,158,11,.15); }
.btn-danger-ghost { color: var(--danger); background: rgba(239,68,68,.08); }
.btn-danger-ghost:hover { background: rgba(239,68,68,.15); }
.pagination { display: flex; align-items: center; justify-content: space-between; padding: 14px 20px; border-top: 1px solid var(--border); }
.pagination-info { font-size: 13px; color: var(--text-muted); }
.pagination-btns { display: flex; gap: 4px; }
.pagination-btns button { padding: 6px 12px; border: 1px solid var(--border); border-radius: 6px; background: var(--bg); cursor: pointer; font-size: 13px; transition: var(--transition); }
.pagination-btns button:hover:not(:disabled) { border-color: var(--primary); color: var(--primary); }
.pagination-btns button.active { background: var(--primary); color: #fff; border-color: var(--primary); }
.pagination-btns button:disabled { opacity: .4; cursor: not-allowed; }
.pagination select { padding: 6px 10px; border: 1px solid var(--border); border-radius: 6px; font-size: 13px; outline: none; cursor: pointer; }
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.4); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 100; }
.modal { background: var(--bg-card); border-radius: var(--radius); width: 100%; max-width: 520px; box-shadow: var(--shadow-lg); animation: slideUp .3s ease; }
.modal-sm { max-width: 400px; }
@keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
.modal-header { display: flex; align-items: center; justify-content: space-between; padding: 20px 24px; border-bottom: 1px solid var(--border); }
.modal-header h3 { font-size: 16px; font-weight: 700; }
.modal-close { width: 28px; height: 28px; border: none; background: var(--bg); border-radius: 50%; cursor: pointer; font-size: 18px; color: var(--text-muted); display: flex; align-items: center; justify-content: center; transition: var(--transition); }
.modal-close:hover { background: var(--border); color: var(--text); }
.modal-body { padding: 24px; }
.modal-footer { display: flex; justify-content: flex-end; gap: 10px; padding: 16px 24px; border-top: 1px solid var(--border); }
.form-group { margin-bottom: 16px; }
.form-group label { display: block; font-size: 13px; font-weight: 600; color: var(--text-secondary); margin-bottom: 6px; }
.required { color: var(--danger); }
.form-group input, .form-group select { width: 100%; padding: 10px 12px; border: 1px solid var(--border); border-radius: var(--radius-sm); font-size: 13px; outline: none; transition: var(--transition); }
.form-group input:focus, .form-group select:focus { border-color: var(--primary); box-shadow: 0 0 0 2px rgba(99,102,241,.1); }
.form-group input:disabled { background: var(--bg); color: var(--text-muted); }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.error-msg { padding: 10px 14px; background: #fef2f2; border: 1px solid #fecaca; border-radius: var(--radius-sm); color: var(--danger); font-size: 13px; }
.spinner { display: inline-block; width: 16px; height: 16px; border: 2px solid var(--border); border-top-color: var(--primary); border-radius: 50%; animation: spin .6s linear infinite; vertical-align: middle; }
.spinner-sm { width: 14px; height: 14px; border-width: 2px; border-color: rgba(255,255,255,.3); border-top-color: #fff; }
@keyframes spin { to { transform: rotate(360deg); } }
</style>