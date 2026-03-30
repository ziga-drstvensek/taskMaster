<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from './store/auth';
import { useBacklogStore } from './store/backlog';
import { useUIStore } from './store/ui';
import BacklogList from './components/BacklogList.vue';
import BacklogForm from './components/BacklogForm.vue';
import SettingsManager from './components/SettingsManager.vue';
import LoginView from './views/LoginView.vue';
import BaseModal from './components/common/BaseModal.vue';
import { LogOut, Plus, Settings, LayoutGrid, Filter, User, UserPlus, Trello, ChevronDown, Check, Sun, Moon, Search, X, Keyboard, Command, Bell, Camera } from 'lucide-vue-next';

const authStore = useAuthStore();
const backlogStore = useBacklogStore();
const uiStore = useUIStore();
const { locale, t } = useI18n();

const showAddModal = ref(false);
const showSettingsManager = ref(false);
const showShortcutsModal = ref(false);
const showNotificationsDropdown = ref(false);
const initialSettingsTab = ref<'sprints' | 'users' | 'boards' | 'notifications'>('notifications');
const showBoardDropdown = ref(false);
const showSprintDropdown = ref(false);
const searchInput = ref<HTMLInputElement | null>(null);

const shortcuts = computed(() => [
  { key: 'N', label: t('common.new_task_shortcut') },
  { key: 'K', label: t('common.kanban_shortcut') },
  { key: 'T', label: t('common.table_shortcut') },
  { key: 'M', label: t('common.my_tasks_shortcut') },
  { key: 'U', label: t('common.unassigned_shortcut') },
  { key: 'S', label: t('common.settings_shortcut') },
  { key: '/', label: t('common.search_shortcut') },
  { key: '?', label: t('common.help_shortcut') },
  { key: 'Esc', label: t('common.esc_shortcut') },
]);

const handleGlobalKeydown = (e: KeyboardEvent) => {
  if (isAnyModalOpen.value) {
    if (e.key === 'Escape' && !uiStore.isModalOpen) {
       showAddModal.value = false;
       showSettingsManager.value = false;
       showShortcutsModal.value = false;
    }
    return;
  }

  if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement || e.target instanceof HTMLSelectElement) {
    return;
  }

  if (e.key.toLowerCase() === 'n') {
    e.preventDefault();
    showAddModal.value = true;
  } else if (e.key.toLowerCase() === 'k') {
    if (backlogStore.selectedBoardId !== -1) {
      e.preventDefault();
      uiStore.setViewMode('kanban');
    }
  } else if (e.key.toLowerCase() === 't') {
    e.preventDefault();
    uiStore.setViewMode('table');
  } else if (e.key.toLowerCase() === 'm') {
    e.preventDefault();
    backlogStore.setSelectedDashboardId('me');
  } else if (e.key.toLowerCase() === 'u') {
    e.preventDefault();
    backlogStore.setSelectedDashboardId('unassigned');
  } else if (e.key.toLowerCase() === 's') {
    e.preventDefault();
    showSettingsManager.value = true;
  } else if (e.key === '/') {
    e.preventDefault();
    searchInput.value?.focus();
  } else if (e.key === '?') {
    e.preventDefault();
    showShortcutsModal.value = true;
  }
};

const vClickOutside = {
  mounted(el: any, binding: any) {
    el.clickOutsideEvent = (event: any) => {
      if (!(el === event.target || el.contains(event.target))) {
        binding.value(event);
      }
    };
    document.addEventListener("click", el.clickOutsideEvent);
  },
  unmounted(el: any) {
    document.removeEventListener("click", el.clickOutsideEvent);
  },
};

const toastMessage = ref('');
const toastType = ref<'info' | 'error' | 'success'>('info');
const showToast = ref(false);

const triggerToast = (msg: string, type: 'info' | 'error' | 'success' = 'info') => {
  toastMessage.value = msg;
  toastType.value = type;
  showToast.value = true;
  setTimeout(() => {
    showToast.value = false;
  }, 3000);
};

(window as any).triggerToast = triggerToast;

const isAnyModalOpen = computed(() => {
  return showAddModal.value || 
         showSettingsManager.value || 
         showShortcutsModal.value ||
         uiStore.isModalOpen;
});

const dashboards = computed<{id: string, name: string, icon: any}[]>(() => [
  { id: 'all', name: t('dashboard.all'), icon: LayoutGrid },
  { id: 'unassigned', name: t('dashboard.unassigned'), icon: UserPlus }
]);

const personalDashboards = computed<{id: string, name: string, icon: any}[]>(() => [
  { id: 'me', name: t('dashboard.me'), icon: User }
]);

onMounted(async () => {
  window.addEventListener('keydown', handleGlobalKeydown);
  const savedDarkMode = localStorage.getItem('dark-mode');
  if (savedDarkMode !== null) {
    uiStore.isDarkMode = savedDarkMode === 'true';
  }
  uiStore.applyTheme();
  uiStore.applyFontSize();
  uiStore.applyFontFamily();
  if (authStore.isAuthenticated) {
    await backlogStore.fetchBoards();
    await backlogStore.fetchItems();
    await backlogStore.initSignalR();
    await backlogStore.fetchSprints();
  }
  
  if (localStorage.getItem('session_expired') === 'true') {
    triggerToast(t('common.session_expired'));
    localStorage.removeItem('session_expired');
  }
});

onUnmounted(() => {
  window.removeEventListener('keydown', handleGlobalKeydown);
});

watch(() => authStore.isAuthenticated, async (newVal) => {
  if (newVal) {
    await backlogStore.fetchBoards();
    await backlogStore.fetchItems();
    await backlogStore.initSignalR();
    await backlogStore.fetchSprints();
  }
});

watch(() => backlogStore.selectedBoardId, () => {
  backlogStore.setSelectedSprintId(null);
  backlogStore.fetchItems();
  backlogStore.fetchSprints();
});

const handleLogout = () => {
  authStore.logout();
};

const clearSearch = () => {
  backlogStore.setSearchQuery('');
};

const fileInput = ref<HTMLInputElement | null>(null);
const isUploading = ref(false);

const handleProfilePictureUpload = async (event: Event) => {
  const input = event.target as HTMLInputElement;
  if (!input.files || input.files.length === 0) return;

  const file = input.files[0];
  if (file.size > 2 * 1024 * 1024) {
    triggerToast(t('common.error.file_too_large'), 'error');
    return;
  }

  isUploading.value = true;
  const reader = new FileReader();
  reader.onload = async (e) => {
    try {
      const base64Image = e.target?.result as string;
      await authStore.updateProfilePicture(base64Image);
      triggerToast(t('common.success.saved'), 'success');
    } catch (err) {
      triggerToast(t('common.error.save'), 'error');
    } finally {
      isUploading.value = false;
      if (fileInput.value) fileInput.value.value = '';
    }
  };
  reader.readAsDataURL(file);
};

const toggleLanguage = () => {
  locale.value = locale.value === 'en' ? 'sl' : 'en';
  localStorage.setItem('user-locale', locale.value);
};

onMounted(() => {
  const savedLocale = localStorage.getItem('user-locale');
  if (savedLocale) {
    locale.value = savedLocale;
  }
});
</script>

<style>
.no-scrollbar::-webkit-scrollbar {
  display: none;
}
.no-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
    
<template>
  <div v-if="!authStore.isAuthenticated">
    <LoginView />
  </div>
  
  <div v-else class="min-h-screen flex flex-col bg-[#f8fafc] dark:bg-slate-950 transition-colors duration-300 dark:text-slate-200">
    <!-- Header -->
    <header class="h-16.25 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 sticky top-0 z-50 flex items-center transition-colors">
      <div class="max-w-400 mx-auto w-full px-4 lg:px-6 py-3 flex justify-between items-center">
        <div class="flex items-center gap-3">
        <div class="relative">
          <img src="./assets/LogoZD.png" :alt="$t('common.backlog')" class="w-10 h-10 object-contain" />
          <div class="absolute -top-1 -right-1 w-4 h-4 bg-white rounded-full flex items-center justify-center shadow-sm" v-if="backlogStore.loading">
            <div class="w-3 h-3 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
          </div>
        </div>
        <h1 class="text-xl font-bold bg-clip-text text-transparent bg-linear-to-r from-indigo-600 to-violet-600">
          {{ $t('common.backlog') }}
        </h1>
        <div class="ml-4 hidden sm:flex items-center bg-slate-100 dark:bg-slate-800 rounded-xl px-3 py-1.5 border border-slate-200 dark:border-slate-700 focus-within:ring-2 focus-within:ring-indigo-500/20 focus-within:border-indigo-500 transition-all w-64 group/search">
          <Search :size="16" class="text-slate-400 mr-2 group-focus-within/search:text-indigo-500 transition-colors" />
          <input 
            type="text" 
            ref="searchInput"
            v-model="backlogStore.searchQuery"
            :placeholder="$t('common.search_placeholder')"
            class="bg-transparent border-none outline-none text-sm w-full text-slate-700 dark:text-slate-200 placeholder:text-slate-400 dark:placeholder:text-slate-500"
          />
          <div v-if="!backlogStore.searchQuery" class="flex items-center gap-1 px-1.5 py-0.5 rounded border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-xxs font-bold text-slate-400 select-none">
            /
          </div>
          <button v-if="backlogStore.searchQuery" @click="clearSearch" class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200">
            <X :size="14" />
          </button>
        </div>
        <button 
          @click="showAddModal = true"
          class="ml-2 sm:ml-4 bg-indigo-600 text-white p-2 rounded-xl shadow-lg shadow-indigo-200 dark:shadow-none hover:bg-indigo-700 transition-all active:scale-95"
          :title="$t('common.new_task')"
        >
          <Plus :size="20" />
        </button>
      </div>

      <div class="flex items-center gap-4">
        <div class="flex items-center">
          <button 
            @click="initialSettingsTab = 'notifications'; showSettingsManager = true"
            class="p-2 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-xl transition-all flex items-center gap-2"
            :class="{ 'text-indigo-600 bg-indigo-50 dark:bg-indigo-900/20 dark:text-indigo-400 shadow-inner': showSettingsManager }"
            :title="$t('common.settings')"
          >
            <Settings :size="18" />
            <span class="hidden sm:inline text-xxs font-black uppercase tracking-widest">{{ $t('common.settings') }}</span>
          </button>
        </div>

        <div class="flex items-center gap-2 sm:gap-3 bg-slate-50 dark:bg-slate-800/50 p-1.5 rounded-2xl border border-slate-100 dark:border-slate-700">
          <button 
            @click="showShortcutsModal = true"
            class="hidden sm:block p-1.5 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-white dark:hover:bg-slate-700 rounded-xl transition-all shadow-sm active:scale-90"
            :title="$t('common.shortcuts')"
          >
            <Keyboard :size="16" />
          </button>
          <div class="hidden sm:block h-6 w-px bg-slate-200 dark:bg-slate-700"></div>
          <button 
            @click="uiStore.toggleDarkMode"
            class="p-1.5 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-white dark:hover:bg-slate-700 rounded-xl transition-all shadow-sm active:scale-90"
            :title="uiStore.isDarkMode ? $t('common.light_mode') : $t('common.dark_mode')"
          >
            <Sun v-if="uiStore.isDarkMode" :size="16" class="text-amber-500" />
            <Moon v-else :size="16" />
          </button>
          
          <div class="h-6 w-px bg-slate-200 dark:bg-slate-700"></div>

          <!-- Notifications -->
          <div class="relative flex items-center" v-click-outside="() => showNotificationsDropdown = false">
            <button 
              @click="showNotificationsDropdown = !showNotificationsDropdown"
              class="p-2 rounded-xl text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors relative"
              :class="{ 'bg-slate-100 dark:bg-slate-800 text-indigo-600': showNotificationsDropdown }"
            >
              <Bell :size="20" />
              <span v-if="authStore.unreadNotificationsCount > 0" class="absolute top-1.5 right-1.5 w-4 h-4 bg-red-500 text-white text-xxs font-bold rounded-full flex items-center justify-center border-2 border-white dark:border-slate-900">
                {{ authStore.unreadNotificationsCount }}
              </span>
            </button>
            
            <div v-if="showNotificationsDropdown" class="absolute right-0 top-full mt-2 w-80 bg-white dark:bg-slate-900 rounded-2xl shadow-xl border border-slate-200 dark:border-slate-800 z-100 overflow-hidden">
              <div class="px-4 py-3 border-b border-slate-100 dark:border-slate-800 flex justify-between items-center bg-slate-50/50 dark:bg-slate-800/50">
                <h3 class="font-bold text-sm">{{ $t('notifications.title') }}</h3>
                <button 
                  v-if="authStore.unreadNotificationsCount > 0"
                  @click="authStore.markAllNotificationsAsRead()"
                  class="text-xs text-indigo-600 hover:text-indigo-700 font-medium"
                >
                  {{ $t('notifications.mark_all_read') }}
                </button>
              </div>
              
              <div class="max-h-96 overflow-y-auto">
                <div v-if="authStore.notifications.length === 0" class="px-4 py-8 text-center text-slate-400 dark:text-slate-500 text-sm">
                  {{ $t('notifications.no_notifications') }}
                </div>
                <div 
                  v-for="notification in authStore.notifications" 
                  :key="notification.id"
                  @click="() => { authStore.markNotificationAsRead(notification.id); showNotificationsDropdown = false; }"
                  class="px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors cursor-pointer border-b border-slate-50 dark:border-slate-800 last:border-0"
                  :class="{ 'bg-indigo-50/30 dark:bg-indigo-900/10': !notification.isRead }"
                >
                  <div class="flex justify-between items-start gap-2 mb-1">
                    <span class="font-semibold text-sm" :class="{ 'text-indigo-600 dark:text-indigo-400': !notification.isRead }">
                      {{ notification.title }}
                    </span>
                    <span class="text-xxs text-slate-400 whitespace-nowrap">
                      {{ new Date(notification.createdAt).toLocaleDateString() }}
                    </span>
                  </div>
                  <p class="text-xs text-slate-600 dark:text-slate-400 line-clamp-2">{{ notification.message }}</p>
                </div>
              </div>
            </div>
          </div>

          <div class="h-6 w-px bg-slate-200 dark:bg-slate-700"></div>
          <button 
            @click="toggleLanguage"
            class="px-2 py-1 text-xxs font-black uppercase tracking-widest bg-white dark:bg-slate-700 border border-slate-200 dark:border-slate-600 text-slate-600 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-600 transition-colors"
          >
            {{ locale === 'en' ? 'SL' : 'EN' }}
          </button>
          <div class="h-6 w-px bg-slate-200 dark:bg-slate-700"></div>
          <div class="relative group/avatar">
            <div 
              class="w-8 h-8 rounded-xl bg-white dark:bg-slate-700 border border-slate-100 dark:border-slate-600 flex items-center justify-center text-indigo-600 shadow-sm overflow-hidden shrink-0 cursor-pointer"
              :title="authStore.user?.username"
              @click="fileInput?.click()"
            >
              <img v-if="authStore.user?.profilePicture" :src="authStore.user.profilePicture" class="w-full h-full object-cover" alt="User Profile Picture" />
              <User v-else :size="16" />
              
              <div v-if="isUploading" class="absolute inset-0 bg-black/50 flex items-center justify-center">
                <div class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              </div>
              
              <div class="absolute inset-0 bg-black/40 opacity-0 group-hover/avatar:opacity-100 flex items-center justify-center transition-opacity">
                <Camera :size="14" class="text-white" />
              </div>
            </div>
            <input 
              type="file" 
              ref="fileInput" 
              class="hidden" 
              accept="image/*"
              @change="handleProfilePictureUpload"
            />
          </div>
          <div class="hidden md:flex flex-col">
            <span class="text-xs font-black text-slate-800 dark:text-slate-200 leading-none">{{ authStore.user?.username }}</span>
            <span class="text-xxs text-slate-400 dark:text-slate-500 uppercase tracking-widest font-black leading-none mt-1">{{ authStore.user?.role }}</span>
          </div>
        </div>
        <div class="h-8 w-px bg-slate-200 dark:bg-slate-700 mx-2"></div>
        <button 
          @click="handleLogout" 
          class="p-2 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/30 rounded-lg transition-colors"
          :title="$t('common.logout')"
        >
          <LogOut :size="20" />
        </button>
      </div>
    </div>
    </header>
    <!-- Main Content -->
    <main
      class="flex-1 overflow-hidden flex flex-col transition-all duration-300"
    >
      <div 
        v-if="isAnyModalOpen" 
        class="fixed inset-0 z-40 bg-slate-900/10 backdrop-blur-[1px] pointer-events-auto"
        @click.stop
      ></div>
      <div class="border-b border-slate-200 dark:border-slate-800 bg-white/50 dark:bg-slate-900/50 backdrop-blur-sm sticky top-0 z-40 transition-colors">
        <div class="max-w-400 mx-auto px-4 lg:px-6 py-2 flex flex-col lg:flex-row justify-between items-start lg:items-center gap-4">
          <div class="flex flex-wrap items-center gap-4">
            <div class="flex items-center gap-2">
              <h2 class="text-xl font-bold text-slate-800 dark:text-slate-100 whitespace-nowrap">{{ $t('common.dashboard') }}</h2>
              <div v-if="backlogStore.boards.length > 0" class="flex items-center gap-2 relative group/board">
                <span class="text-slate-400 dark:text-slate-600">/</span>
                <div class="relative " v-click-outside="() => showBoardDropdown = false">
                  <button 
                    class="flex items-center gap-2 px-2.5 py-1 rounded-lg bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 hover:border-indigo-300 dark:hover:border-indigo-500 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 transition-all text-indigo-600 dark:text-indigo-400 font-bold shadow-sm text-sm"
                    @click.stop="showBoardDropdown = !showBoardDropdown"
                  >
                    <Trello :size="14" />
                    <span>{{ backlogStore.selectedBoardId === -1 ? $t('common.all_boards') : (backlogStore.boards.find(b => b.id === backlogStore.selectedBoardId)?.name || $t('common.select_board')) }}</span>
                    <ChevronDown :size="12" class="transition-transform duration-200" :class="{ 'rotate-180': showBoardDropdown }" />
                  </button>
                  
                  <Transition
                    enter-active-class="transition duration-150 ease-out"
                    enter-from-class="opacity-0 scale-95 -translate-y-1"
                    enter-to-class="opacity-100 scale-100 translate-y-0"
                    leave-active-class="transition duration-100 ease-in"
                    leave-from-class="opacity-100 scale-100 translate-y-0"
                    leave-to-class="opacity-0 scale-95 -translate-y-1"
                  >
                  <div 
                    v-if="showBoardDropdown" 
                    class="absolute top-full left-0 mt-2 w-56 bg-white dark:bg-slate-800 rounded-xl shadow-lg border border-slate-200 dark:border-slate-700 overflow-hidden z-100"
                  >
                    <div class="p-2 space-y-1">
                      <button 
                        v-if="authStore.isAdmin"
                        @click="backlogStore.setSelectedBoardId(-1); showBoardDropdown = false"
                        class="w-full flex items-center justify-between px-4 py-3 rounded-xl text-sm font-bold transition-all relative overflow-hidden group/item"
                        :class="backlogStore.selectedBoardId === -1
                          ? 'bg-indigo-600 text-white shadow-lg shadow-indigo-200 dark:shadow-none ring-2 ring-indigo-600 ring-offset-2' 
                          : 'text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700 hover:text-indigo-600 dark:hover:text-indigo-400'"
                      >
                        <div class="flex items-center gap-3 min-w-0">
                          <div 
                            class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 transition-colors"
                            :class="backlogStore.selectedBoardId === -1 ? 'bg-white/20' : 'bg-slate-100 dark:bg-slate-700 group-hover/item:bg-indigo-100 dark:group-hover/item:bg-indigo-900/50'"
                          >
                            <LayoutGrid :size="14" />
                          </div>
                          <span class="truncate">{{ $t('common.all_boards') }}</span>
                        </div>
                        <Check v-if="backlogStore.selectedBoardId === -1" :size="16" class="shrink-0" />
                      </button>

                      <div v-if="authStore.isAdmin" class="h-px bg-slate-100 dark:bg-slate-700 my-1"></div>


                      <button 
                        v-for="b in backlogStore.boards" 
                        :key="b.id"
                        @click="backlogStore.setSelectedBoardId(b.id); showBoardDropdown = false"
                        class="w-full flex items-center justify-between px-4 py-3 rounded-xl text-sm font-bold transition-all relative overflow-hidden group/item"
                        :class="backlogStore.selectedBoardId === b.id 
                          ? 'bg-indigo-600 text-white shadow-lg shadow-indigo-200 dark:shadow-none ring-2 ring-indigo-600 ring-offset-2' 
                          : 'text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700 hover:text-indigo-600 dark:hover:text-indigo-400'"
                      >
                        <div class="flex items-center gap-3 min-w-0">
                          <div 
                            class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 transition-colors"
                            :class="backlogStore.selectedBoardId === b.id ? 'bg-white/20' : 'bg-slate-100 dark:bg-slate-700 group-hover/item:bg-indigo-100 dark:group-hover/item:bg-indigo-900/50'"
                          >
                            <Trello :size="14" />
                          </div>
                          <span class="truncate">{{ b.name }}</span>
                        </div>
                        <Check v-if="backlogStore.selectedBoardId === b.id" :size="16" class="shrink-0" />
                      </button>
                    </div>
                  </div>
                  </Transition>
                </div>
              </div>
            </div>

            <div class="h-6 w-px bg-slate-200 dark:bg-slate-800 mx-2 hidden sm:block"></div>

            <div class="flex items-center gap-3 overflow-x-auto pb-1 sm:pb-0 no-scrollbar">
              <div class="flex items-center gap-1.5 flex-nowrap">
                <button 
                  v-for="db in dashboards" 
                  :key="db.id"
                  @click="backlogStore.setSelectedDashboardId(db.id)"
                  class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-[11px] font-bold transition-all border whitespace-nowrap"
                  :class="backlogStore.selectedDashboardId === db.id 
                    ? 'bg-indigo-600 text-white border-indigo-600 shadow-sm' 
                    : 'bg-white dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-200 dark:border-slate-700 hover:border-indigo-300 dark:hover:border-indigo-500 hover:text-indigo-600 dark:hover:text-indigo-400'"
                >
                  <component :is="db.icon" :size="13" />
                  {{ db.name }}
                </button>
              </div>

              <div class="h-4 w-px bg-slate-200 dark:bg-slate-800 mx-1"></div>

              <div class="flex items-center gap-1.5 flex-nowrap">
                <button 
                  v-for="db in personalDashboards" 
                  :key="db.id"
                  @click="backlogStore.setSelectedDashboardId(db.id)"
                  class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-[11px] font-bold transition-all border whitespace-nowrap"
                  :class="backlogStore.selectedDashboardId === db.id 
                    ? 'bg-indigo-600 text-white border-indigo-600 shadow-sm' 
                    : 'bg-white dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-200 dark:border-slate-700 hover:border-indigo-300 dark:hover:border-indigo-500 hover:text-indigo-600 dark:hover:text-indigo-400'"
                >
                  <component :is="db.icon" :size="13" />
                  {{ db.name }}
                </button>
              </div>

              <template v-if="backlogStore.selectedBoardId !== -1">
                <div class="h-4 w-px bg-slate-200 dark:bg-slate-800 mx-1"></div>

                <div class="inline-flex bg-slate-100 dark:bg-slate-800 p-0.5 rounded-lg border border-slate-200 dark:border-slate-700 flex-nowrap">
                  <button 
                    @click="uiStore.setViewMode('kanban')"
                    class="flex items-center gap-1.5 px-2 py-1 rounded-md text-xxs font-bold transition-all whitespace-nowrap"
                    :class="uiStore.viewMode === 'kanban' ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm' : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
                    :title="$t('common.kanban_view')"
                  >
                    <Columns :size="12" />
                    <span class="hidden sm:inline">{{ $t('common.kanban_view') }}</span>
                  </button>
                  <button 
                    @click="uiStore.setViewMode('table')"
                    class="flex items-center gap-1.5 px-2 py-1 rounded-md text-xxs font-bold transition-all whitespace-nowrap"
                    :class="uiStore.viewMode === 'table' ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm' : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
                    :title="$t('common.table_view')"
                  >
                    <List :size="12" />
                    <span class="hidden sm:inline">{{ $t('common.table_view') }}</span>
                  </button>
                </div>
              </template>
            </div>
          </div>

          <div class="flex items-center gap-3 w-full lg:w-auto">
            <div class="relative flex-1 lg:flex-initial min-w-40" v-click-outside="() => showSprintDropdown = false">
              <button
                type="button"
                @click="showSprintDropdown = !showSprintDropdown"
                class="w-full pl-9 pr-9 py-2 text-left text-sm bg-white dark:bg-slate-800 border rounded-xl transition-all flex items-center justify-between gap-2 shadow-sm"
                :class="showSprintDropdown ? 'border-indigo-400 dark:border-indigo-500 ring-2 ring-indigo-500/20' : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600'"
              >
                <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <Filter :size="14" class="text-slate-400 dark:text-slate-500" />
                </div>
                <span class="text-slate-700 dark:text-slate-200 font-medium truncate">
                  {{ backlogStore.selectedSprintId === null 
                    ? $t('common.allSprints') 
                    : (backlogStore.sprints.find((s: any) => s.id === backlogStore.selectedSprintId)?.name || $t('common.allSprints')) 
                      + (backlogStore.sprints.find((s: any) => s.id === backlogStore.selectedSprintId)?.isActive ? ` (${$t('common.active')})` : '')
                  }}
                </span>
                <ChevronDown 
                  :size="14" 
                  class="absolute right-2.5 text-slate-400 transition-transform duration-200"
                  :class="{ 'rotate-180': showSprintDropdown }"
                />
              </button>
              <Transition
                enter-active-class="transition duration-150 ease-out"
                enter-from-class="opacity-0 scale-95 -translate-y-1"
                enter-to-class="opacity-100 scale-100 translate-y-0"
                leave-active-class="transition duration-100 ease-in"
                leave-from-class="opacity-100 scale-100 translate-y-0"
                leave-to-class="opacity-0 scale-95 -translate-y-1"
              >
                <div 
                  v-if="showSprintDropdown"
                  class="absolute z-50 w-full mt-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg shadow-lg overflow-hidden"
                >
                  <ul class="py-1 max-h-60 overflow-auto custom-scrollbar">
                    <li
                      @click="backlogStore.selectedSprintId = null; showSprintDropdown = false"
                      class="px-4 py-2.5 cursor-pointer flex items-center justify-between gap-2 transition-colors duration-100"
                      :class="'bg-indigo-50 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300'"
                    >
                      <span class="text-sm font-medium">{{ $t('common.allSprints') }}</span>
                      <Check v-if="true" :size="16" class="text-indigo-600 dark:text-indigo-400" />
                    </li>
                    <li
                      v-for="s in backlogStore.sprints"
                      :key="s.id"
                      @click="backlogStore.selectedSprintId = s.id; showSprintDropdown = false"
                      class="px-4 py-2.5 cursor-pointer flex items-center justify-between gap-2 transition-colors duration-100"
                      :class="backlogStore.selectedSprintId === s.id ? 'bg-indigo-50 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700'"
                    >
                      <span class="text-sm font-medium">
                        {{ s.name }} <span v-if="s.isActive" class="text-xs text-emerald-600 dark:text-emerald-400">({{ $t('common.active') }})</span>
                      </span>
                      <Check v-if="backlogStore.selectedSprintId === s.id" :size="16" class="text-indigo-600 dark:text-indigo-400" />
                    </li>
                  </ul>
                </div>
              </Transition>
            </div>

            <button 
              @click="showAddModal = true"
              class="btn-primary group py-2! px-4!"
            >
              <Plus :size="16" class="mr-1.5 group-hover:rotate-90 transition-transform duration-300" />
              <span class="text-sm">{{ $t('common.new_task') }}</span>
            </button>
          </div>
        </div>
      </div>

      <div class="flex-1 overflow-auto custom-scrollbar">
        <div class="h-full max-w-400 mx-auto p-4 lg:p-6">
          <BacklogList :disabled="isAnyModalOpen" />
        </div>
      </div>
    </main>

    <!-- Modals -->
    <BacklogForm v-if="showAddModal" @close="showAddModal = false" />
    
    <BaseModal 
      :show="showSettingsManager" 
      @close="showSettingsManager = false"
      :title="$t('common.settings')"
      maxWidth="1100px"
      persistent
    >
      <SettingsManager :initialTab="initialSettingsTab" />
    </BaseModal>

    <BaseModal
      :show="showShortcutsModal"
      @close="showShortcutsModal = false"
      :title="$t('common.shortcuts')"
      maxWidth="400px"
    >
      <div class="space-y-4">
        <div v-for="shortcut in shortcuts" :key="shortcut.key" class="flex items-center justify-between p-3 rounded-2xl border border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/30">
          <span class="text-sm font-medium text-slate-600 dark:text-slate-400">{{ shortcut.label }}</span>
          <div class="flex items-center gap-1.5">
            <div class="px-2.5 py-1 rounded-lg bg-white dark:bg-slate-700 border-b-2 border-slate-200 dark:border-slate-900 text-xs font-black text-slate-700 dark:text-slate-200 shadow-sm ring-1 ring-slate-200/50 dark:ring-slate-700/50 min-w-8 flex items-center justify-center">
              <Command v-if="shortcut.key === 'Esc'" :size="10" class="mr-1 opacity-50" />
              {{ shortcut.key }}
            </div>
          </div>
        </div>
      </div>
      <div class="mt-6 text-center text-xs text-slate-400 dark:text-slate-500 font-medium">
        {{ $t('common.shortcut_help') }}
      </div>
    </BaseModal>

    <!-- Toast -->
    <div 
      v-if="showToast" 
      class="fixed bottom-10 left-1/2 transform -translate-x-1/2 z-100 text-white px-6 py-3 rounded-2xl shadow-2xl flex items-center gap-3 animate-in fade-in slide-in-from-bottom-4 duration-300 border"
      :class="{
        'bg-slate-800 border-slate-700': toastType === 'info',
        'bg-rose-600 border-rose-500': toastType === 'error',
        'bg-emerald-600 border-emerald-500': toastType === 'success'
      }"
    >
      <div class="w-2 h-2 rounded-full bg-white animate-pulse"></div>
      <span class="text-sm font-medium">{{ toastMessage }}</span>
    </div>
  </div>
</template>

<style>
  @keyframes pulse {
    0%, 100% {
      opacity: 1;
    }
    50% {
      opacity: .7;
    }
  }
</style>
