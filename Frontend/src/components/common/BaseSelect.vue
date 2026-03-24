<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { ChevronDown, Check } from 'lucide-vue-next';

const props = defineProps<{
  modelValue: string | number;
  label?: string;
  options: { value: string | number; label: string }[];
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  error?: string;
}>();

const emit = defineEmits(['update:modelValue']);

const isOpen = ref(false);
const dropdownRef = ref<HTMLElement | null>(null);

const selectedOption = computed(() => {
  return props.options.find(opt => String(opt.value) === String(props.modelValue));
});

const displayValue = computed(() => {
  return selectedOption.value?.label || props.placeholder || '';
});

const selectOption = (opt: { value: string | number; label: string }) => {
  emit('update:modelValue', opt.value);
  isOpen.value = false;
};

const toggleDropdown = () => {
  if (!props.disabled) {
    isOpen.value = !isOpen.value;
  }
};

const handleClickOutside = (event: MouseEvent) => {
  if (isOpen.value && dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    isOpen.value = false;
  }
};

onMounted(() => {
  document.addEventListener('mousedown', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('mousedown', handleClickOutside);
});
</script>

<template>
  <div class="w-full" ref="dropdownRef">
    <label v-if="label" class="block text-[10px] font-black uppercase text-slate-400 dark:text-slate-500 mb-1.5 ml-1 tracking-widest">
      {{ label }} <span v-if="required" class="text-rose-500">*</span>
    </label>
    
    <div class="relative">
      <!-- Trigger button -->
      <button
        type="button"
        @click="toggleDropdown"
        :disabled="disabled"
        class="w-full px-4 py-2.5 text-left border rounded-lg outline-none transition-all duration-200 flex items-center justify-between gap-2 bg-white dark:bg-slate-800"
        :class="[
          error 
            ? 'border-rose-400 focus:ring-2 focus:ring-rose-500/20 focus:border-rose-500' 
            : isOpen 
              ? 'border-indigo-400 dark:border-indigo-500 ring-2 ring-indigo-500/20' 
              : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-400 dark:focus:border-indigo-500',
          disabled ? 'bg-slate-50 dark:bg-slate-900 text-slate-400 dark:text-slate-600 cursor-not-allowed' : 'cursor-pointer'
        ]"
      >
        <span 
          class="truncate text-sm"
          :class="selectedOption ? 'text-slate-700 dark:text-slate-200 font-medium' : 'text-slate-400 dark:text-slate-500'"
        >
          {{ displayValue }}
        </span>
        <ChevronDown 
          :size="18" 
          class="flex-shrink-0 text-slate-400 transition-transform duration-200"
          :class="{ 'rotate-180': isOpen }"
        />
      </button>
      
      <!-- Dropdown menu -->
      <Transition
        enter-active-class="transition duration-150 ease-out"
        enter-from-class="opacity-0 scale-95 -translate-y-1"
        enter-to-class="opacity-100 scale-100 translate-y-0"
        leave-active-class="transition duration-100 ease-in"
        leave-from-class="opacity-100 scale-100 translate-y-0"
        leave-to-class="opacity-0 scale-95 -translate-y-1"
      >
        <div 
          v-if="isOpen"
          class="absolute z-50 w-full mt-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg shadow-lg overflow-hidden"
        >
          <ul class="py-1 max-h-60 overflow-auto custom-scrollbar">
            <!-- Empty state -->
            <li
              v-if="options.length === 0"
              class="px-4 py-3 text-center text-sm text-slate-400 dark:text-slate-500 italic"
            >
              {{ $t('common.no_items') }}
            </li>
            <li
              v-for="opt in options"
              :key="opt.value"
              @click="selectOption(opt)"
              class="px-4 py-2.5 cursor-pointer flex items-center justify-between gap-2 transition-colors duration-100"
              :class="[
                String(opt.value) === String(modelValue)
                  ? 'bg-indigo-50 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300'
                  : 'text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700'
              ]"
            >
              <span class="text-sm font-medium truncate">{{ opt.label }}</span>
              <Check 
                v-if="String(opt.value) === String(modelValue)" 
                :size="16" 
                class="shrink-0 text-indigo-600 dark:text-indigo-400"
              />
            </li>
          </ul>
        </div>
      </Transition>
    </div>
    
    <p v-if="error" class="mt-1 text-xs text-rose-500 ml-1">{{ error }}</p>
  </div>
</template>
