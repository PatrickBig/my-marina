import axios from 'axios'

/**
 * Base Axios instance. All API calls go through here so auth headers,
 * base URL, and error handling are applied in one place.
 *
 * TypeScript types are auto-generated from the OpenAPI spec — run:
 *   npm run generate-api
 * to regenerate src/api/schema.d.ts after backend changes.
 */
export const apiClient = axios.create({
  baseURL: '/api',
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
