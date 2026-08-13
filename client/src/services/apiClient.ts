export interface ApiRequestOptions {
  readonly signal?: AbortSignal
}

class ApiError extends Error {
  readonly status: number | undefined

  constructor(message: string, status?: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim() ?? ''
const apiBaseUrl = configuredBaseUrl.replace(/\/+$/, '')

function createApiUrl(path: string): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${apiBaseUrl}${normalizedPath}`
}

function getHttpErrorMessage(status: number): string {
  if (status >= 500) {
    return 'The API is temporarily unavailable. Please try again.'
  }

  if (status === 404) {
    return 'The requested API resource is unavailable.'
  }

  return 'The API request could not be completed. Please try again.'
}

async function requestJson<T>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<T> {
  let response: Response

  try {
    response = await fetch(createApiUrl(path), {
      headers: {
        Accept: 'application/json',
      },
      credentials: 'include',
      signal: options.signal,
    })
  } catch (error: unknown) {
    if (options.signal?.aborted) {
      throw error
    }

    throw new ApiError('Unable to connect to the API. Please try again.')
  }

  if (!response.ok) {
    throw new ApiError(getHttpErrorMessage(response.status), response.status)
  }

  try {
    const responseBody: unknown = await response.json()
    return responseBody as T
  } catch (error: unknown) {
    if (options.signal?.aborted) {
      throw error
    }

    throw new ApiError('The API returned an invalid response. Please try again.')
  }
}

export function getSafeApiErrorMessage(
  error: unknown,
  fallbackMessage = 'System information is unavailable. Please try again.',
): string {
  if (error instanceof ApiError) {
    return error.message
  }

  return fallbackMessage
}

export const apiClient = {
  get: <T>(path: string, options?: ApiRequestOptions) =>
    requestJson<T>(path, options),
}
