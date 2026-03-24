<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { Calendar as CalendarIcon, ChevronLeft, ChevronRight, X } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
  modelValue: string | null | undefined;
  label?: string;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  error?: string;
}>();

const emit = defineEmits(['update:modelValue']);
const { t, locale } = useI18n();

const isOpen = ref(false);
const dropdownRef = ref<HTMLElement | null>(null);

// Calendar state
const viewDate = ref(new Date());
if (props.modelValue) {
  const d = new Date(props.modelValue);
  if (!isNaN(d.getTime())) {
    viewDate.value = new Date(d);
  }
}

const selectedDate = computed(() => {
  if (!props.modelValue) return null;
  const d = new Date(props.modelValue);
  return isNaN(d.getTime()) ? null : d;
});

const formattedValue = computed(() => {
  if (!selectedDate.value) return '';
  return selectedDate.value.toLocaleDateString(locale.value === 'sl' ? 'sl-SI' : 'en-US');
});

const daysInMonth = computed(() => {
  const year = viewDate.value.getFullYear();
  const month = viewDate.value.getMonth();
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  
  const days = [];
  
  // Padding for start of month (get day of week, 0=Sun, 1=Mon... Adjust to Mon=0)
  let startDay = firstDay.getDay();
  startDay = startDay === 0 ? 6 : startDay - 1; // Adjust to Monday start
  
  // Previous month days
  const prevMonthLastDay = new Date(year, month, 0).getDate();
  for (let i = startDay - 1; i >= 0; i--) {
    days.push({
      day: prevMonthLastDay - i,
      month: month - 1,
      year: year,
      currentMonth: false
    });
  }
  
  // Current month days
  for (let i = 1; i <= lastDay.getDate(); i++) {
    days.push({
      day: i,
      month: month,
      year: year,
      currentMonth: true
    });
  }
  
  // Next month days
  const remaining = 42 - days.length;
  for (let i = 1; i <= remaining; i++) {
    days.push({
      day: i,
      month: month + 1,
      year: year,
      currentMonth: false
    });
  }
  
  return days;
});

const monthName = computed(() => {
  return viewDate.value.toLocaleString(locale.value === 'sl' ? 'sl-SI' : 'en-US', { month: 'long', year: 'numeric' });
});

const weekDays = computed(() => {
  if (locale.value === 'sl') {
    return ['Po', 'To', 'Sr', 'Če', 'Pe', 'So', 'Ne'];
  }
  return ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];
});

const toggleDropdown = () => {
  if (!props.disabled) {
    isOpen.value = !isOpen.value;
    if (isOpen.value && selectedDate.value) {
      viewDate.value = new Date(selectedDate.value);
    }
  }
};

const selectDate = (day: { day: number, month: number, year: number }) => {
  const date = new Date(day.year, day.month, day.day);
  // Format as YYYY-MM-DD for consistency with native date inputs
  const formatted = date.toISOString().split('T')[0];
  emit('update:modelValue', formatted);
  isOpen.value = false;
};

const clearDate = (e: Event) => {
  e.stopPropagation();
  emit('update:modelValue', null);
};

const changeMonth = (delta: number) => {
  const d = new Date(viewDate.value);
  d.setMonth(d.getMonth() + delta);
  viewDate.value = d;
};

const isSelected = (day: { day: number, month: number, year: number }) => {
  if (!selectedDate.value) return false;
  return selectedDate.value.getDate() === day.day && 
         selectedDate.value.getMonth() === day.month && 
         selectedDate.value.getFullYear() === day.year;
};

const isToday = (day: { day: number, month: number, year: number }) => {
  const today = new Date();
  return today.getDate() === day.day && 
         today.getMonth() === day.month && 
         today.getFullYear() === day.year;
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

watch(() => props.modelValue, (newVal) => {
  if (newVal) {
    const d = new Date(newVal);
    if (!isNaN(d.getTime())) {
      viewDate.value = new Date(d);
    }
  }
});
</script>

<template>
  <div class="w-full" ref="dropdownRef">
    <label v-if="label" class="label-caps">
      {{ label }} <span v-if="required" class="text-rose-500">*</span>
    </label>
    
    <div class="relative">
      <!-- Trigger button -->
      <button
        type="button"
        @click="toggleDropdown"
        :disabled="disabled"
        class="w-full px-4 py-2.5 text-left border rounded-2xl outline-none transition-all duration-200 flex items-center justify-between gap-2 bg-white dark:bg-slate-800"
        :class="[
          error 
            ? 'border-rose-400 focus:ring-2 focus:ring-rose-500/20 focus:border-rose-500' 
            : isOpen 
              ? 'border-indigo-400 dark:border-indigo-500 ring-2 ring-indigo-500/20' 
              : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-400 dark:focus:border-indigo-500',
          disabled ? 'bg-slate-50 dark:bg-slate-900 text-slate-400 dark:text-slate-600 cursor-not-allowed' : 'cursor-pointer'
        ]"
      >
        <div class="flex items-center gap-2 truncate overflow-hidden">
          <CalendarIcon :size="16" class="text-slate-400 shrink-0" />
          <span 
            class="truncate text-sm"
            :class="selectedDate ? 'text-slate-700 dark:text-slate-200 font-medium' : 'text-slate-400 dark:text-slate-500'"
          >
            {{ formattedValue || placeholder || $t('common.select_date') }}
          </span>
        </div>
        
        <div class="flex items-center gap-1">
          <button 
            v-if="selectedDate && !disabled && !required" 
            @click="clearDate"
            type="button"
            class="p-1 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-full text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 transition-colors"
          >
            <X :size="14" />
          </button>
        </div>
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
          class="absolute z-[60] mt-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-2xl shadow-xl overflow-hidden p-4 min-w-[300px]"
        >
          <!-- Calendar Header -->
          <div class="flex items-center justify-between mb-4">
            <button @click="changeMonth(-1)" type="button" class="p-1.5 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-xl transition-colors text-slate-500 dark:text-slate-400">
              <ChevronLeft :size="18" />
            </button>
            <span class="text-sm font-black uppercase tracking-widest text-slate-700 dark:text-slate-200">{{ monthName }}</span>
            <button @click="changeMonth(1)" type="button" class="p-1.5 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-xl transition-colors text-slate-500 dark:text-slate-400">
              <ChevronRight :size="18" />
            </button>
          </div>
          
          <!-- Calendar Grid -->
          <div class="grid grid-cols-7 gap-1">
            <!-- Day names -->
            <div v-for="day in weekDays" :key="day" class="calendar-day-header">
              {{ day }}
            </div>
            
            <!-- Days -->
            <button
              v-for="(day, index) in daysInMonth"
              :key="index"
              @click="selectDate(day)"
              type="button"
              class="aspect-square flex items-center justify-center text-xs font-bold rounded-xl transition-all"
              :class="[
                day.currentMonth 
                  ? isSelected(day)
                    ? 'bg-indigo-500 text-white shadow-md shadow-indigo-500/20'
                    : isToday(day)
                      ? 'bg-indigo-50 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400'
                      : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700'
                  : 'text-slate-300 dark:text-slate-600 hover:bg-slate-50 dark:hover:bg-slate-700/50'
              ]"
            >
              {{ day.day }}
            </button>
          </div>
          
          <!-- Quick actions -->
          <div class="mt-4 pt-4 border-t border-slate-100 dark:border-slate-700 flex justify-between gap-2">
             <button 
                @click="selectDate({ day: new Date().getDate(), month: new Date().getMonth(), year: new Date().getFullYear() })"
                type="button"
                class="text-xxs font-black uppercase tracking-widest text-indigo-600 dark:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 px-3 py-1.5 rounded-lg transition-all"
              >
                {{ $t('common.today') }}
             </button>
             <button 
                @click="isOpen = false"
                type="button"
                class="text-xxs font-black uppercase tracking-widest text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 px-3 py-1.5 rounded-lg transition-all"
              >
                {{ $t('common.close') }}
             </button>
          </div>
        </div>
      </Transition>
    </div>
    
    <p v-if="error" class="mt-1 text-xs text-rose-500 ml-1">{{ error }}</p>
  </div>
</template>

<style scoped>
.aspect-square {
  aspect-ratio: 1 / 1;
}
</style>
