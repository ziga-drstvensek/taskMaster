<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import api from '../services/api';
import { Plus, Trash2, UserPlus, Users, Mail, Shield, ShieldAlert, Key, Search, Camera } from 'lucide-vue-next';
import BaseInput from './common/BaseInput.vue';
import BaseSelect from './common/BaseSelect.vue';

const { t } = useI18n();
const users = ref<any[]>([]);
const loading = ref(false);
const showForm = ref(false);
const isEditing = ref(false);

const username = ref('');
const email = ref('');
const password = ref('');
const role = ref('User');
const tags = ref('');
const profilePicture = ref('');
const error = ref('');
const searchQuery = ref('');
const fileInput = ref<HTMLInputElement | null>(null);
const isUploading = ref(false);

const filteredUsers = computed(() => {
  if (!searchQuery.value) return users.value;
  const q = searchQuery.value.toLowerCase();
  return users.value.filter(u => 
    u.username.toLowerCase().includes(q) || 
    u.email.toLowerCase().includes(q) ||
    (u.tags && u.tags.toLowerCase().includes(q))
  );
});

const fetchUsers = async () => {
  loading.value = true;
  try {
    const response = await api.get('/auth/users-list');
    users.value = response.data;
  } catch (err) {
    console.error('Failed to fetch users');
  } finally {
    loading.value = false;
  }
};

const resetForm = () => {
  username.value = '';
  email.value = '';
  password.value = '';
  role.value = 'User';
  tags.value = '';
  profilePicture.value = '';
  error.value = '';
  showForm.value = false;
  isEditing.value = false;
};

const handleEdit = (user: any) => {
  username.value = user.username;
  email.value = user.email;
  password.value = '';
  role.value = user.role;
  tags.value = user.tags || '';
  profilePicture.value = user.profilePicture || '';
  isEditing.value = true;
  showForm.value = true;
};

const handleFileUpload = (event: Event) => {
  const input = event.target as HTMLInputElement;
  if (!input.files || input.files.length === 0) return;

  const file = input.files[0];
  if (file.size > 2 * 1024 * 1024) {
    triggerToast(t('common.error.file_too_large'), 'error');
    return;
  }

  isUploading.value = true;
  const reader = new FileReader();
  reader.onload = (e) => {
    profilePicture.value = e.target?.result as string;
    isUploading.value = false;
  };
  reader.readAsDataURL(file);
};

const triggerToast = (msg: string, type: 'info' | 'error' | 'success' = 'info') => {
  (window as any).triggerToast?.(msg, type);
};

const handleDelete = async (user: any) => {
  if (!confirm(t('users_mng.delete_confirm'))) return;
  
  try {
    await api.delete(`/auth/users/${user.username}`);
    await fetchUsers();
    triggerToast(t('common.success.deleted'), 'success');
  } catch (err: any) {
    triggerToast(err.response?.data?.message || t('common.error.delete'), 'error');
  }
};

const handleSubmit = async () => {
  error.value = '';
  try {
    if (isEditing.value) {
      await api.put(`/auth/users/${username.value}`, {
        username: username.value,
        email: email.value,
        password: password.value,
        role: role.value,
        tags: tags.value,
        profilePicture: profilePicture.value
      });
      triggerToast(t('common.success.saved'), 'success');
    } else {
      await api.post('/auth/register', {
        username: username.value,
        email: email.value,
        password: password.value
      });
      if (role.value !== 'User' || tags.value || profilePicture.value) {
          await api.put(`/auth/users/${username.value}`, {
            username: username.value,
            email: email.value,
            role: role.value,
            tags: tags.value,
            profilePicture: profilePicture.value
          });
      }
      triggerToast(t('common.success.created'), 'success');
    }
    await fetchUsers();
    resetForm();
  } catch (err: any) {
    error.value = typeof err.response?.data === 'string' 
      ? err.response.data 
      : err.response?.data?.[0]?.description || err.response?.data?.message || t('common.error.save');
    triggerToast(error.value, 'error');
  }
};

onMounted(fetchUsers);
</script>

<template>
  <div>
    <!-- Form -->
    <form @submit.prevent="handleSubmit" v-if="showForm" class="mb-8 p-6 bg-indigo-50/50 dark:bg-indigo-900/20 rounded-3xl border border-indigo-100 dark:border-indigo-900/40 space-y-6 animate-in fade-in slide-in-from-top-4 duration-300 shadow-sm">
      <div class="flex items-center gap-2 mb-2">
        <UserPlus class="text-indigo-600 dark:text-indigo-400" :size="18" />
        <h4 class="font-bold text-slate-800 dark:text-slate-100">{{ isEditing ? $t('users_mng.edit') : $t('users_mng.add') }}</h4>
      </div>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <BaseInput 
          v-model="username"
          :label="$t('common.username')"
          :disabled="isEditing"
          required
        />
        <BaseInput 
          v-model="email"
          type="email"
          :label="$t('users_mng.email')"
          required
        />
        <BaseInput 
          v-model="password"
          type="password"
          :label="$t('common.password')"
          :required="!isEditing"
          :placeholder="isEditing ? $t('users_mng.password_hint') : 'Min 6 chars, number, uppercase'"
        />
        <BaseSelect 
          v-model="role"
          :label="$t('users_mng.role')"
          :options="[
            { value: 'User', label: 'User' },
            { value: 'Manager', label: 'Manager' },
            { value: 'Admin', label: 'Admin' }
          ]"
          searchable
        />
        <div class="flex flex-col gap-2">
          <label class="block text-xs font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest px-1">
            {{ $t('users_mng.profile_picture') }}
          </label>
          <div class="flex items-center gap-4">
            <div 
              class="w-12 h-12 rounded-2xl bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 flex items-center justify-center text-indigo-600 overflow-hidden cursor-pointer hover:border-indigo-300 transition-all shadow-sm"
              @click="fileInput?.click()"
            >
              <img v-if="profilePicture" :src="profilePicture" class="w-full h-full object-cover" />
              <Camera v-else :size="20" class="text-slate-400" />
            </div>
            <div class="flex flex-col gap-1">
              <button 
                type="button" 
                @click="fileInput?.click()"
                class="text-xs font-bold text-indigo-600 hover:text-indigo-700 transition-colors"
              >
                {{ profilePicture ? $t('common.change') : $t('common.upload') }}
              </button>
              <button 
                v-if="profilePicture"
                type="button" 
                @click="profilePicture = ''"
                class="text-xs font-bold text-rose-500 hover:text-rose-600 transition-colors"
              >
                {{ $t('common.remove') }}
              </button>
            </div>
            <input 
              type="file" 
              ref="fileInput" 
              class="hidden" 
              accept="image/*"
              @change="handleFileUpload"
            />
          </div>
        </div>
        <div class="md:col-span-2">
          <BaseInput 
            v-model="tags"
            :label="$t('users_mng.tags')"
            :placeholder="$t('users_mng.tags_hint')"
          />
        </div>
      </div>

      <div v-if="error" class="text-rose-500 dark:text-rose-400 text-xs font-medium px-1">
        {{ error }}
      </div>

      <div class="flex justify-end gap-4 pt-4 border-t border-indigo-100/50 dark:border-indigo-900/30">
        <button type="button" @click="resetForm" class="px-4 py-2 text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200 text-sm font-bold uppercase tracking-wider transition-colors">{{ $t('common.cancel') }}</button>
        <button type="submit" class="bg-indigo-600 text-white px-8 py-2.5 rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-indigo-700 transition-all shadow-lg shadow-indigo-100 dark:shadow-none active:scale-95">
          {{ isEditing ? $t('common.save') : $t('users_mng.add') }}
        </button>
      </div>
    </form>

    <!-- Search and List Header -->
    <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 mb-6">
      <div class="flex items-center gap-4 w-full md:w-auto">
        <div class="flex items-center gap-2">
          <Users class="text-slate-400 dark:text-slate-500" :size="18" />
          <span class="text-xs font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest">{{ $t('common.users') }} ({{ users.length }})</span>
        </div>
        
        <div class="relative flex-1 md:w-64">
          <Search class="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" :size="14" />
          <input 
            v-model="searchQuery"
            type="text" 
            :placeholder="$t('common.search')"
            class="w-full pl-9 pr-4 py-1.5 bg-slate-100 dark:bg-slate-800 border-none rounded-xl text-xs focus:ring-2 focus:ring-indigo-500/20 outline-none dark:text-slate-200"
          >
        </div>
      </div>
      
      <button 
        v-if="!showForm"
        @click="showForm = true" 
        class="bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-indigo-600 dark:text-indigo-400 px-4 py-1.5 rounded-xl text-xs font-black uppercase tracking-widest hover:bg-indigo-50 dark:hover:bg-indigo-900/30 hover:border-indigo-200 dark:hover:border-indigo-900/40 transition-all shadow-sm active:scale-95 whitespace-nowrap"
      >
        <Plus :size="14" class="inline mr-1" /> {{ $t('users_mng.add') }}
      </button>
    </div>

    <!-- List -->
    <div v-if="loading" class="flex justify-center py-12">
      <div class="w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
    </div>
    
    <div v-else-if="filteredUsers.length === 0" class="text-center py-12 bg-slate-50 dark:bg-slate-900/40 rounded-3xl border-2 border-dashed border-slate-200 dark:border-slate-800">
      <Users class="mx-auto text-slate-300 dark:text-slate-700 mb-2" :size="48" />
      <p class="text-slate-400 dark:text-slate-500 font-bold uppercase tracking-widest text-xxs">{{ searchQuery ? $t('common.no_results') : $t('users_mng.no_users') }}</p>
    </div>

    <div v-else class="overflow-x-auto -mx-2">
      <table class="w-full text-left border-collapse">
        <thead>
          <tr class="text-slate-400 dark:text-slate-500 text-xxs font-black uppercase tracking-widest border-b border-slate-100 dark:border-slate-800">
            <th class="px-6 py-4">{{ $t('common.username') }}</th>
            <th class="px-6 py-4">{{ $t('users_mng.email') }}</th>
            <th class="px-6 py-4">{{ $t('users_mng.role') }}</th>
            <th class="px-6 py-4">{{ $t('users_mng.tags') }}</th>
            <th class="px-6 py-4 text-right">{{ $t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-50 dark:divide-slate-800/50">
          <tr v-for="u in filteredUsers" :key="u.username" class="group hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition-all">
            <td class="px-6 py-4">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-2xl bg-indigo-100 dark:bg-indigo-900/40 flex items-center justify-center text-indigo-700 dark:text-indigo-400 font-black text-xs uppercase shadow-sm overflow-hidden">
                  <img v-if="u.profilePicture" :src="u.profilePicture" class="w-full h-full object-cover" />
                  <span v-else>{{ u.username.substring(0, 2) }}</span>
                </div>
                <span class="font-bold text-slate-700 dark:text-slate-200 tracking-tight">{{ u.username }}</span>
              </div>
            </td>
            <td class="px-6 py-4 text-xs text-slate-500 dark:text-slate-400 font-medium">
              <div class="flex items-center gap-1.5">
                <Mail :size="12" class="text-slate-300 dark:text-slate-600" />
                {{ u.email }}
              </div>
            </td>
            <td class="px-6 py-4">
              <div class="flex items-center gap-1.5">
                <Shield v-if="u.role === 'Admin'" :size="14" class="text-amber-500 dark:text-amber-400" />
                <ShieldAlert v-else :size="14" class="text-slate-400 dark:text-slate-600" />
                <span class="badge px-2 py-0.5 shadow-sm" 
                      :class="u.role === 'Admin' ? 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400'">
                  {{ u.role }}
                </span>
              </div>
            </td>
            <td class="px-6 py-4">
              <div class="flex flex-wrap gap-1.5">
                <span v-for="tag in (u.tags?.split(',').map((s: string) => s.trim()).filter((s: string) => s) || [])" :key="tag" 
                      class="badge px-2 py-0.5 bg-indigo-50 dark:bg-indigo-900/30 text-indigo-600 dark:text-indigo-400 shadow-sm">
                  {{ tag }}
                </span>
              </div>
            </td>
            <td class="px-6 py-4 text-right">
              <div class="flex justify-end gap-1 opacity-0 group-hover:opacity-100 transition-all">
                 <button @click="handleEdit(u)" class="p-2 text-slate-400 dark:text-slate-500 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-xl transition-all" :title="$t('common.edit')">
                  <Key :size="16" />
                </button>
                 <button v-if="u.username !== 'admin'" @click="handleDelete(u)" class="p-2 text-slate-400 dark:text-slate-500 hover:text-rose-600 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-xl transition-all" :title="$t('common.delete')">
                  <Trash2 :size="16" />
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
