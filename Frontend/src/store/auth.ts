import { defineStore } from 'pinia';
import api from '../services/api';
import type { User } from '../types';

export const useAuthStore = defineStore('auth', {
    state: () => ({
        user: JSON.parse(localStorage.getItem('user') || 'null') as User | null,
        loading: false,
        error: null as string | null
    }),
    getters: {
        isAuthenticated: (state) => !!state.user,
        isAdmin: (state) => state.user?.role === 'Admin',
        isManager: (state) => state.user?.role === 'Admin' || state.user?.role === 'Manager'
    },
    actions: {
        async login(credentials: any) {
            this.loading = true;
            this.error = null;
            try {
                const response = await api.post('/auth/login', credentials);
                this.user = response.data;
                localStorage.setItem('user', JSON.stringify(this.user));
            } catch (err: any) {
                this.error = err.response?.data?.message || 'Login failed';
                throw err;
            } finally {
                this.loading = false;
            }
        },
        async register(userData: any) {
            this.loading = true;
            this.error = null;
            try {
                await api.post('/auth/register', userData);
            } catch (err: any) {
                this.error = err.response?.data?.message || 'Registration failed';
                throw err;
            } finally {
                this.loading = false;
            }
        },
        logout() {
            this.user = null;
            localStorage.removeItem('user');
        }
    }
});
