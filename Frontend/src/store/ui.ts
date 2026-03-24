import { defineStore } from 'pinia';

export const useUIStore = defineStore('ui', {
    state: () => ({
        activeModals: 0,
        isDarkMode: localStorage.getItem('dark-mode') === 'true',
        viewMode: (localStorage.getItem('view-mode') as 'table' | 'kanban') || 'kanban'
    }),
    getters: {
        isModalOpen: (state) => state.activeModals > 0
    },
    actions: {
        registerModal() {
            this.activeModals++;
        },
        unregisterModal() {
            this.activeModals = Math.max(0, this.activeModals - 1);
        },
        setViewMode(mode: 'table' | 'kanban') {
            this.viewMode = mode;
            localStorage.setItem('view-mode', mode);
        },
        toggleDarkMode() {
            this.isDarkMode = !this.isDarkMode;
            localStorage.setItem('dark-mode', this.isDarkMode.toString());
            this.applyTheme();
        },
        applyTheme() {
            const htmlElement = document.documentElement;
            if (this.isDarkMode) {
                htmlElement.classList.add('dark');
                htmlElement.style.colorScheme = 'dark';
            } else {
                htmlElement.classList.remove('dark');
                htmlElement.style.colorScheme = 'light';
            }
        }
    }
});
