<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from './store/auth';
import { useBacklogStore } from './store/backlog';
import { useUIStore } from './store/ui';
import BacklogList from './components/BacklogList.vue';
import BacklogForm from './components/BacklogForm.vue';
import SprintManager from './components/SprintManager.vue';
import UserManager from './components/UserManager.vue';
import BoardManager from './components/BoardManager.vue';
import LoginView from './views/LoginView.vue';
import BaseModal from './components/common/BaseModal.vue';
import api from './services/api';
import type { Sprint } from './types';
import { LogOut, Plus, Settings, LayoutGrid, Users, Filter, User, UserPlus, Trello, ChevronDown, Check, Sun, Moon } from 'lucide-vue-next';

const authStore = useAuthStore();
const backlogStore = useBacklogStore();
const uiStore = useUIStore();
const { locale, t } = useI18n();

const showAddModal = ref(false);
const showSprintManager = ref(false);
const showUserManager = ref(false);
const showBoardManager = ref(false);
const showBoardDropdown = ref(false);
const showSprintDropdown = ref(false);

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
         showSprintManager.value || 
         showUserManager.value || 
         showBoardManager.value || 
         uiStore.isModalOpen;
});

const dashboards = computed<{id: string, name: string, icon: any}[]>(() => [
  { id: 'all', name: t('dashboard.all'), icon: LayoutGrid },
  { id: 'me', name: t('dashboard.me'), icon: User },
  { id: 'unassigned', name: t('dashboard.unassigned'), icon: UserPlus }
]);

onMounted(async () => {
  const savedDarkMode = localStorage.getItem('dark-mode');
  if (savedDarkMode !== null) {
    uiStore.isDarkMode = savedDarkMode === 'true';
  }
  uiStore.applyTheme();
  if (authStore.isAuthenticated) {
    await backlogStore.fetchBoards();
    await backlogStore.fetchItems();
    backlogStore.initSignalR();
    backlogStore.fetchSprints();
  }
  
  if (localStorage.getItem('session_expired') === 'true') {
    triggerToast(t('common.session_expired'));
    localStorage.removeItem('session_expired');
  }
});

watch(() => authStore.isAuthenticated, async (newVal) => {
  if (newVal) {
    await backlogStore.fetchBoards();
    await backlogStore.fetchItems();
    backlogStore.initSignalR();
    backlogStore.fetchSprints();
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

<template>
  <div v-if="!authStore.isAuthenticated">
    <LoginView />
  </div>
  
  <div v-else class="min-h-screen flex flex-col bg-[#f8fafc] dark:bg-slate-950 transition-colors duration-300 dark:text-slate-200">
    <!-- Header -->
    <header class="bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 sticky top-0 z-10 px-6 py-3 flex justify-between items-center transition-colors">
      <div class="flex items-center gap-3">
        <div class="relative">
          <img src="./assets/logo2.png" :alt="$t('common.backlog')" class="w-10 h-10 object-contain" />
          <div class="absolute -top-1 -right-1 w-4 h-4 bg-white rounded-full flex items-center justify-center shadow-sm" v-if="backlogStore.loading">
            <div class="w-3 h-3 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
          </div>
        </div>
        <h1 class="text-xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-indigo-600 to-violet-600">
          {{ $t('common.backlog') }}
        </h1>
        <button 
          @click="showAddModal = true"
          class="ml-4 bg-indigo-600 text-white p-2 rounded-xl shadow-lg shadow-indigo-200 dark:shadow-none hover:bg-indigo-700 transition-all active:scale-95"
          :title="$t('common.new_task')"
        >
          <Plus :size="20" />
        </button>
      </div>
      
      <div class="flex items-center gap-4">
        <div v-if="authStore.isManager" class="hidden lg:flex items-center">
          <button 
            @click="showBoardManager = !showBoardManager; showSprintManager = false; showUserManager = false"
            class="p-2 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-xl transition-all flex items-center gap-2"
            :class="{ 'text-indigo-600 bg-indigo-50 dark:bg-indigo-900/20 dark:text-indigo-400 shadow-inner': showBoardManager }"
            :title="$t('common.boards')"
          >
            <Trello :size="18" />
            <span class="text-[10px] font-black uppercase tracking-widest">{{ $t('common.boards') }}</span>
          </button>
          
          <div class="h-6 w-px bg-slate-100 dark:bg-slate-700 mx-2"></div>

          <button 
            @click="showSprintManager = !showSprintManager; showUserManager = false; showBoardManager = false"
            class="p-2 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-xl transition-all flex items-center gap-2"
            :class="{ 'text-indigo-600 bg-indigo-50 dark:bg-indigo-900/20 dark:text-indigo-400 shadow-inner': showSprintManager }"
            :title="$t('common.sprints')"
          >
            <Settings :size="18" />
            <span class="text-[10px] font-black uppercase tracking-widest">{{ $t('common.sprints') }}</span>
          </button>
          
          <div class="h-6 w-px bg-slate-100 dark:bg-slate-700 mx-2"></div>

          <button 
            v-if="authStore.isAdmin"
            @click="showUserManager = !showUserManager; showSprintManager = false; showBoardManager = false"
            class="p-2 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-xl transition-all flex items-center gap-2"
            :class="{ 'text-indigo-600 bg-indigo-50 dark:bg-indigo-900/20 dark:text-indigo-400 shadow-inner': showUserManager }"
            :title="$t('common.users')"
          >
            <Users :size="18" />
            <span class="text-[10px] font-black uppercase tracking-widest">{{ $t('common.users') }}</span>
          </button>

          <div v-if="authStore.isAdmin" class="h-6 w-px bg-slate-100 dark:bg-slate-700 mx-4"></div>
        </div>

        <div class="hidden sm:flex items-center gap-3 bg-slate-50 dark:bg-slate-800/50 p-1.5 rounded-2xl border border-slate-100 dark:border-slate-700">
          <button 
            @click="uiStore.toggleDarkMode"
            class="p-1.5 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-white dark:hover:bg-slate-700 rounded-xl transition-all shadow-sm active:scale-90"
            :title="uiStore.isDarkMode ? $t('common.light_mode') : $t('common.dark_mode')"
          >
            <Sun v-if="uiStore.isDarkMode" :size="16" class="text-amber-500" />
            <Moon v-else :size="16" />
          </button>
          <div class="h-6 w-px bg-slate-200 dark:bg-slate-700"></div>
          <button 
            @click="toggleLanguage"
            class="px-2 py-1 text-[10px] font-black uppercase tracking-widest bg-white dark:bg-slate-700 border border-slate-200 dark:border-slate-600 text-slate-600 dark:text-slate-300 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-600 transition-colors"
          >
            {{ locale === 'en' ? 'SL' : 'EN' }}
          </button>
          <div class="h-6 w-px bg-slate-200 dark:bg-slate-700"></div>
          <div class="w-8 h-8 rounded-xl bg-white dark:bg-slate-700 border border-slate-100 dark:border-slate-600 flex items-center justify-center text-indigo-600 shadow-sm">
             <User :size="16" />
          </div>
          <div class="flex flex-col">
            <span class="text-xs font-black text-slate-800 dark:text-slate-200 leading-none">{{ authStore.user?.username }}</span>
            <span class="text-[8px] text-slate-400 dark:text-slate-500 uppercase tracking-widest font-black">{{ authStore.user?.role }}</span>
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
      <div class="px-6 py-6 border-b border-slate-200 dark:border-slate-800 bg-white/50 dark:bg-slate-900/50 backdrop-blur-sm relative z-50 transition-colors">
        <div class="max-w-[1600px] mx-auto flex flex-col sm:flex-row justify-between items-start sm:items-center gap-6">
          <div>
            <div class="flex items-center gap-3">
              <h2 class="text-2xl font-bold text-slate-800 dark:text-slate-100">{{ $t('common.dashboard') }}</h2>
              <div v-if="backlogStore.boards.length > 0" class="flex items-center gap-2 relative group/board">
                <span class="text-slate-400 dark:text-slate-600">/</span>
                <div class="relative" v-click-outside="() => showBoardDropdown = false">
                  <button 
                    class="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 hover:border-indigo-300 dark:hover:border-indigo-500 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 transition-all text-indigo-600 dark:text-indigo-400 font-bold shadow-sm"
                    @click.stop="showBoardDropdown = !showBoardDropdown"
                  >
                    <Trello :size="16" />
                    <span>{{ backlogStore.selectedBoardId === -1 ? $t('common.all_boards') : (backlogStore.boards.find(b => b.id === backlogStore.selectedBoardId)?.name || $t('common.select_board')) }}</span>
                    <ChevronDown :size="14" class="transition-transform duration-200" :class="{ 'rotate-180': showBoardDropdown }" />
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
                    class="absolute top-full left-0 mt-2 w-56 bg-white dark:bg-slate-800 rounded-xl shadow-lg border border-slate-200 dark:border-slate-700 overflow-hidden z-[100]"
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
                            class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 transition-colors"
                            :class="backlogStore.selectedBoardId === -1 ? 'bg-white/20' : 'bg-slate-100 dark:bg-slate-700 group-hover/item:bg-indigo-100 dark:group-hover/item:bg-indigo-900/50'"
                          >
                            <LayoutGrid :size="14" />
                          </div>
                          <span class="truncate">{{ $t('common.all_boards') }}</span>
                        </div>
                        <Check v-if="backlogStore.selectedBoardId === -1" :size="16" class="flex-shrink-0" />
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
                            class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 transition-colors"
                            :class="backlogStore.selectedBoardId === b.id ? 'bg-white/20' : 'bg-slate-100 dark:bg-slate-700 group-hover/item:bg-indigo-100 dark:group-hover/item:bg-indigo-900/50'"
                          >
                            <Trello :size="14" />
                          </div>
                          <span class="truncate">{{ b.name }}</span>
                        </div>
                        <Check v-if="backlogStore.selectedBoardId === b.id" :size="16" class="flex-shrink-0" />
                      </button>
                    </div>
                  </div>
                  </Transition>
                </div>
              </div>
            </div>
            <div class="flex items-center gap-2 mt-1">
              <button 
                v-for="db in dashboards" 
                :key="db.id"
                @click="backlogStore.setSelectedDashboardId(db.id)"
                class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold transition-all border"
                :class="backlogStore.selectedDashboardId === db.id 
                  ? 'bg-indigo-600 text-white border-indigo-600 shadow-sm' 
                  : 'bg-white dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-200 dark:border-slate-700 hover:border-indigo-300 dark:hover:border-indigo-500 hover:text-indigo-600 dark:hover:text-indigo-400'"
              >
                <component :is="db.icon" :size="14" />
                {{ db.name }}
              </button>
            </div>
          </div>
          <div class="flex flex-col sm:flex-row items-start sm:items-center gap-6">
            <div class="relative min-w-[200px]" v-click-outside="() => showSprintDropdown = false">
              <button
                type="button"
                @click="showSprintDropdown = !showSprintDropdown"
                class="w-full pl-10 pr-10 py-2.5 text-left text-sm bg-white dark:bg-slate-800 border rounded-xl transition-all flex items-center justify-between gap-2 shadow-sm"
                :class="showSprintDropdown ? 'border-indigo-400 dark:border-indigo-500 ring-2 ring-indigo-500/20' : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600'"
              >
                <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <Filter :size="14" class="text-slate-400 dark:text-slate-500" />
                </div>
                <span class="text-slate-700 dark:text-slate-200 font-medium truncate">
                  {{ backlogStore.selectedSprintId === null 
                    ? $t('common.allSprints') 
                    : (backlogStore.sprints.find(s => s.id === backlogStore.selectedSprintId)?.name || $t('common.allSprints')) 
                      + (backlogStore.sprints.find(s => s.id === backlogStore.selectedSprintId)?.isActive ? ` (${$t('common.active')})` : '')
                  }}
                </span>
                <ChevronDown 
                  :size="16" 
                  class="absolute right-3 text-slate-400 transition-transform duration-200"
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
                      :class="backlogStore.selectedSprintId === null ? 'bg-indigo-50 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300' : 'text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700'"
                    >
                      <span class="text-sm font-medium">{{ $t('common.allSprints') }}</span>
                      <Check v-if="backlogStore.selectedSprintId === null" :size="16" class="text-indigo-600 dark:text-indigo-400" />
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
              class="btn-primary group"
            >
              <Plus :size="18" class="mr-2 group-hover:rotate-90 transition-transform duration-300" />
              {{ $t('common.new_task') }}
            </button>
          </div>
        </div>
      </div>

      <div class="flex-1 overflow-auto p-8 custom-scrollbar">
        <div class="h-full max-w-[1600px] mx-auto">
          <BacklogList :disabled="isAnyModalOpen" />
        </div>
      </div>
    </main>

    <!-- Modals -->
    <BacklogForm v-if="showAddModal" @close="showAddModal = false" />
    
    <BaseModal 
      :show="showSprintManager" 
      @close="showSprintManager = false"
      :title="$t('sprints.manage')"
      maxWidth="800px"
      persistent
    >
      <SprintManager />
    </BaseModal>


    <BaseModal 
      :show="showUserManager" 
      @close="showUserManager = false"
      :title="$t('users_mng.manage')"
      maxWidth="800px"
      persistent
    >
      <UserManager />
    </BaseModal>

    <BaseModal 
      :show="showBoardManager" 
      @close="showBoardManager = false"
      :title="$t('common.manage_boards')"
      maxWidth="800px"
      persistent
    >
      <BoardManager />
    </BaseModal>

    <!-- Toast -->
    <div 
      v-if="showToast" 
      class="fixed bottom-10 left-1/2 transform -translate-x-1/2 z-[100] text-white px-6 py-3 rounded-2xl shadow-2xl flex items-center gap-3 animate-in fade-in slide-in-from-bottom-4 duration-300 border"
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
