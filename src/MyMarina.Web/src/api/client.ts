import axios from 'axios'
import { toast } from 'sonner'
import { useAuthStore } from '@/store/authStore'

/**
 * Base Axios instance. All API calls go through here so auth headers,
 * base URL, and error handling are applied in one place.
 *
 * TypeScript types are auto-generated from the OpenAPI spec — run:
 *   npm run generate-api
 * to regenerate src/api/schema.d.ts after backend changes.
 */
// In dev, Vite proxies /api → localhost:5222 (see vite.config.ts).
// In production, API_BASE_URL is injected at container startup via envsubst into config.js.
const baseURL = (window as any).__CONFIG__?.apiBaseUrl ?? '/api'

export const apiClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Attach JWT Bearer token from localStorage on every request
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Handle 401 errors (expired or invalid tokens)
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout()
      toast.error('Your session has expired. Please log in again.')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)
