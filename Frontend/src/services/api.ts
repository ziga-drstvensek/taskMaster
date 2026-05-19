import axios from 'axios';
import { useAuthStore } from '../store/auth';

const isElectron = !!(window && (window as any).electron);
const baseURL = isElectron 
    ? (window as any).electron.env.BACKEND_URL + '/api'
    : (import.meta.env.VITE_API_BASE_URL || '/api');

const api = axios.create({
    baseURL: baseURL
});

api.interceptors.request.use(config => {
    const userJson = localStorage.getItem('user');
    if (userJson) {
        const user = JSON.parse(userJson);
        if (user.token) {
            config.headers.Authorization = `Bearer ${user.token}`;
        }
    }
    return config;
});

api.interceptors.response.use(
    response => response,
    error => {
        if (error.response && error.response.status === 401) {
            const authStore = useAuthStore();
            if (authStore.isAuthenticated) {
                localStorage.setItem('session_expired', 'true');
                authStore.logout();
            }
        }
        return Promise.reject(error);
    }
);

export default api;
