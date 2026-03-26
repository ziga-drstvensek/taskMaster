import { defineStore } from 'pinia';
import api from '../services/api';
import type { User, Notification } from '../types';

export const useAuthStore = defineStore('auth', {
    state: () => ({
        user: JSON.parse(localStorage.getItem('user') || 'null') as User | null,
        notifications: [] as Notification[],
        loading: false,
        error: null as string | null
    }),
    getters: {
        isAuthenticated: (state) => !!state.user,
        isAdmin: (state) => state.user?.role === 'Admin',
        isManager: (state) => state.user?.role === 'Admin' || state.user?.role === 'Manager',
        unreadNotificationsCount: (state) => state.notifications.filter(n => !n.isRead).length
    },
    actions: {
        async login(credentials: any) {
            this.loading = true;
            this.error = null;
            try {
                const response = await api.post('/auth/login', credentials);
                this.user = response.data;
                localStorage.setItem('user', JSON.stringify(this.user));
                await this.fetchNotifications();
            } catch (err: any) {
                this.error = err.response?.data?.message || 'Login failed';
                throw err;
            } finally {
                this.loading = false;
            }
        },
        async updateProfilePicture(base64Image: string) {
            try {
                await api.put('/auth/profile', { profilePicture: base64Image });
                if (this.user) {
                    this.user.profilePicture = base64Image;
                    localStorage.setItem('user', JSON.stringify(this.user));
                }
            } catch (err: any) {
                console.error('Failed to update profile picture', err);
                throw err;
            }
        },
        async fetchNotifications() {
            if (!this.user) return;
            try {
                const response = await api.get('/notifications');
                this.notifications = response.data;
            } catch (err) {
                console.error('Failed to fetch notifications', err);
            }
        },
        async markNotificationAsRead(id: number) {
            try {
                await api.put(`/notifications/${id}/read`);
                const notification = this.notifications.find(n => n.id === id);
                if (notification) notification.isRead = true;
            } catch (err) {
                console.error('Failed to mark notification as read', err);
            }
        },
        async markAllNotificationsAsRead() {
            try {
                await api.put('/notifications/read-all');
                this.notifications.forEach(n => n.isRead = true);
            } catch (err) {
                console.error('Failed to mark all notifications as read', err);
            }
        },
        addNotification(notification: Notification) {
            this.notifications.unshift(notification);
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
