import type { AntiforgeryToken } from '../models'
import { antiforgeryService } from './antiforgeryService'

export interface ApiRequestOptions {
  readonly signal?: AbortSignal
  readonly headers?: HeadersInit
}

export interface ApiRequestBodyOptions extends ApiRequestOptions {
  readonly body?: XMLHttpRequestBodyInit | null
}

type ApiHttpMethod =
  | 'GET'
  | 'HEAD'
  | 'OPTIONS'
  | 'POST'
  | 'PUT'
  | 'PATCH'
  | 'DELETE'

interface ApiFetchOptions extends ApiRequestBodyOptions {
  readonly cache?: RequestCache
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
const antiforgeryHeaderName = 'X-CSRF-TOKEN'
const antiforgeryTokenPath = '/api/security/antiforgery-token'
const antiforgeryValidationProblemType =
  'urn:independent-approval:problem:antiforgery-validation'
const safeHttpMethods: ReadonlySet<ApiHttpMethod> = new Set([
  'GET',
  'HEAD',
  'OPTIONS',
])

function createApiUrl(path: string): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return apiBaseUrl ? `${apiBaseUrl}${normalizedPath}` : normalizedPath
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

function isUnsafeMethod(method: ApiHttpMethod): boolean {
  return !safeHttpMethods.has(method)
}

function createRequestHeaders(
  method: ApiHttpMethod,
  configuredHeaders?: HeadersInit,
  requestToken?: string,
): Headers {
  const headers = new Headers(configuredHeaders)

  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json')
  }

  if (isUnsafeMethod(method) && requestToken !== undefined) {
    headers.set(antiforgeryHeaderName, requestToken)
  } else {
    headers.delete(antiforgeryHeaderName)
  }

  return headers
}

async function sendRequest(
  path: string,
  method: ApiHttpMethod,
  options: ApiFetchOptions,
  requestToken?: string,
): Promise<Response> {
  try {
    return await fetch(createApiUrl(path), {
      method,
      headers: createRequestHeaders(method, options.headers, requestToken),
      body: options.body,
      credentials: 'include',
      signal: options.signal,
      cache: options.cache,
    })
  } catch (error: unknown) {
    if (options.signal?.aborted) {
      throw error
    }

    throw new ApiError('Unable to connect to the API. Please try again.')
  }
}

function isAntiforgeryToken(value: unknown): value is AntiforgeryToken {
  return (
    typeof value === 'object' &&
    value !== null &&
    'requestToken' in value &&
    typeof value.requestToken === 'string' &&
    value.requestToken.trim().length > 0
  )
}

async function acquireAntiforgeryToken(): Promise<string> {
  const responseBody = await requestJson<unknown>(
    antiforgeryTokenPath,
    'GET',
    { cache: 'no-store' },
  )

  if (!isAntiforgeryToken(responseBody)) {
    throw new ApiError('The API returned an invalid response. Please try again.')
  }

  return responseBody.requestToken
}

async function isAntiforgeryValidationFailure(
  response: Response,
): Promise<boolean> {
  if (response.status !== 400) {
    return false
  }

  try {
    const responseBody: unknown = await response.clone().json()

    return (
      typeof responseBody === 'object' &&
      responseBody !== null &&
      'type' in responseBody &&
      responseBody.type === antiforgeryValidationProblemType
    )
  } catch {
    return false
  }
}

async function sendUnsafeRequest(
  path: string,
  method: ApiHttpMethod,
  options: ApiFetchOptions,
): Promise<Response> {
  let requestToken = await antiforgeryService.getRequestToken(
    acquireAntiforgeryToken,
  )
  let response = await sendRequest(path, method, options, requestToken)

  if (!(await isAntiforgeryValidationFailure(response))) {
    return response
  }

  antiforgeryService.invalidateRequestToken(requestToken)
  requestToken = await antiforgeryService.getRequestToken(
    acquireAntiforgeryToken,
  )
  response = await sendRequest(path, method, options, requestToken)

  if (await isAntiforgeryValidationFailure(response)) {
    antiforgeryService.invalidateRequestToken(requestToken)
  }

  return response
}

async function requestJson<T>(
  path: string,
  method: ApiHttpMethod,
  options: ApiFetchOptions = {},
): Promise<T> {
  const response = isUnsafeMethod(method)
    ? await sendUnsafeRequest(path, method, options)
    : await sendRequest(path, method, options)

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
    requestJson<T>(path, 'GET', options),
  post: <T>(path: string, options?: ApiRequestBodyOptions) =>
    requestJson<T>(path, 'POST', options),
  put: <T>(path: string, options?: ApiRequestBodyOptions) =>
    requestJson<T>(path, 'PUT', options),
  patch: <T>(path: string, options?: ApiRequestBodyOptions) =>
    requestJson<T>(path, 'PATCH', options),
  delete: <T>(path: string, options?: ApiRequestBodyOptions) =>
    requestJson<T>(path, 'DELETE', options),
}
