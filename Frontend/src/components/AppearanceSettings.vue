<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { useUIStore } from '../store/ui';
import { Type, Check, Type as FontIcon, Sun, Moon } from 'lucide-vue-next';

const { t } = useI18n();
const uiStore = useUIStore();

const fontSizes = [
  { id: 'small', name: t('common.small'), class: 'text-xs' },
  { id: 'medium', name: t('common.medium'), class: 'text-base' },
  { id: 'large', name: t('common.large'), class: 'text-lg' }
] as const;

const fontFamilies = [
  { id: 'sans', name: t('common.fonts.sans'), class: 'font-sans' },
  { id: 'serif', name: t('common.fonts.serif'), class: 'font-serif' },
  { id: 'mono', name: t('common.fonts.mono'), class: 'font-mono' },
  { id: 'display', name: t('common.fonts.display'), class: 'font-display' },
  { id: 'comic', name: t('common.fonts.comic'), class: 'font-comic' },
  { id: 'retro', name: t('common.fonts.retro'), class: 'font-retro' }
] as const;

const setFontSize = (size: 'small' | 'medium' | 'large') => {
  uiStore.setFontSize(size);
};

const setFontFamily = (font: string) => {
  uiStore.setFontFamily(font);
};

const toggleDarkMode = () => {
  uiStore.toggleDarkMode();
};
</script>

<template>
  <div class="space-y-8">
    <!-- Theme Mode -->
    <div>
      <h3 class="text-lg font-bold text-slate-800 dark:text-slate-100 mb-4 flex items-center gap-2">
        <component :is="uiStore.isDarkMode ? 'Moon' : 'Sun'" :size="20" class="text-indigo-600" />
        {{ $t('common.appearance') }}
      </h3>
      
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <button
          @click="uiStore.isDarkMode = false; uiStore.applyTheme(); uiStore.saveTheme('false')"
          class="flex items-center justify-between p-4 rounded-2xl border-2 transition-all text-left"
          :class="!uiStore.isDarkMode
            ? 'border-indigo-600 bg-indigo-50 dark:bg-indigo-900/20'
            : 'border-slate-100 dark:border-slate-800 hover:border-indigo-200 dark:hover:border-indigo-800 bg-white dark:bg-slate-900'"
        >
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-amber-100 flex items-center justify-center text-amber-600">
              <Sun :size="20" />
            </div>
            <span class="font-medium text-slate-700 dark:text-slate-200">{{ $t('common.light_mode') }}</span>
          </div>
          <div v-if="!uiStore.isDarkMode" class="w-6 h-6 rounded-full bg-indigo-600 flex items-center justify-center text-white">
            <Check :size="14" />
          </div>
        </button>

        <button
          @click="uiStore.isDarkMode = true; uiStore.applyTheme(); uiStore.saveTheme('true')"
          class="flex items-center justify-between p-4 rounded-2xl border-2 transition-all text-left"
          :class="uiStore.isDarkMode
            ? 'border-indigo-600 bg-indigo-50 dark:bg-indigo-900/20'
            : 'border-slate-100 dark:border-slate-800 hover:border-indigo-200 dark:hover:border-indigo-800 bg-white dark:bg-slate-900'"
        >
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-slate-800 flex items-center justify-center text-slate-400">
              <Moon :size="20" />
            </div>
            <span class="font-medium text-slate-700 dark:text-slate-200">{{ $t('common.dark_mode') }}</span>
          </div>
          <div v-if="uiStore.isDarkMode" class="w-6 h-6 rounded-full bg-indigo-600 flex items-center justify-center text-white">
            <Check :size="14" />
          </div>
        </button>
      </div>
    </div>

    <!-- Font Size -->
    <div>
      <h3 class="text-lg font-bold text-slate-800 dark:text-slate-100 mb-4 flex items-center gap-2">
        <Type :size="20" class="text-indigo-600" />
        {{ $t('common.font_size') }}
      </h3>
      
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <button
          v-for="size in fontSizes"
          :key="size.id"
          @click="setFontSize(size.id)"
          class="flex items-center justify-between p-4 rounded-2xl border-2 transition-all text-left"
          :class="uiStore.fontSize === size.id
            ? 'border-indigo-600 bg-indigo-50 dark:bg-indigo-900/20'
            : 'border-slate-100 dark:border-slate-800 hover:border-indigo-200 dark:hover:border-indigo-800 bg-white dark:bg-slate-900'"
        >
          <div class="flex flex-col">
            <span class="text-xs font-black uppercase tracking-widest text-slate-400 dark:text-slate-500 mb-1">
              {{ size.name }}
            </span>
            <span :class="size.class" class="font-medium text-slate-700 dark:text-slate-200">
              Aa Bb Cc
            </span>
          </div>
          <div 
            v-if="uiStore.fontSize === size.id"
            class="w-6 h-6 rounded-full bg-indigo-600 flex items-center justify-center text-white"
          >
            <Check :size="14" />
          </div>
        </button>
      </div>
    </div>

    <!-- Font Family -->
    <div>
      <h3 class="text-lg font-bold text-slate-800 dark:text-slate-100 mb-4 flex items-center gap-2">
        <FontIcon :size="20" class="text-indigo-600" />
        {{ $t('common.font_family') }}
      </h3>
      
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <button
          v-for="font in fontFamilies"
          :key="font.id"
          @click="setFontFamily(font.id)"
          class="flex items-center justify-between p-4 rounded-2xl border-2 transition-all text-left"
          :class="uiStore.fontFamily === font.id
            ? 'border-indigo-600 bg-indigo-50 dark:bg-indigo-900/20'
            : 'border-slate-100 dark:border-slate-800 hover:border-indigo-200 dark:hover:border-indigo-800 bg-white dark:bg-slate-900'"
        >
          <div class="flex flex-col">
            <span class="text-xs font-black uppercase tracking-widest text-slate-400 dark:text-slate-500 mb-1">
              {{ font.id.toUpperCase() }}
            </span>
            <span :class="font.class" class="text-base font-medium text-slate-700 dark:text-slate-200">
              {{ font.name }}
            </span>
          </div>
          <div 
            v-if="uiStore.fontFamily === font.id"
            class="w-6 h-6 rounded-full bg-indigo-600 flex items-center justify-center text-white"
          >
            <Check :size="14" />
          </div>
        </button>
      </div>
    </div>

    <div class="p-4 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-100 dark:border-slate-700">
      <p class="text-sm text-slate-500 dark:text-slate-400 italic">
        {{ $t('common.appearance') }}: {{ $t(`common.${uiStore.fontSize}`) }}, {{ fontFamilies.find(f => f.id === uiStore.fontFamily)?.name }}
      </p>
    </div>
  </div>
</template>
