import type { AntiforgeryToken } from '../models'
import { antiforgeryService } from './antiforgeryService'

export interface ApiRequestOptions {
  readonly signal?: AbortSignal
  readonly headers?: HeadersInit
}

export interface ApiRequestBodyOptions extends ApiRequestOptions {
  readonly body?: XMLHttpRequestBodyInit | null
  readonly json?: unknown
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

export interface ApiProblemDetails {
  readonly type?: string
  readonly title?: string
  readonly status?: number
  readonly detail?: string
  readonly instance?: string
  readonly code?: string
  readonly errors: Readonly<Record<string, readonly string[]>>
}

export class ApiError extends Error {
  readonly status: number | undefined
  readonly code: string | undefined
  readonly errors: Readonly<Record<string, readonly string[]>>
  readonly problemDetails: ApiProblemDetails | null

  constructor(
    message: string,
    status?: number,
    problemDetails: ApiProblemDetails | null = null,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = problemDetails?.code
    this.errors = problemDetails?.errors ?? {}
    this.problemDetails = problemDetails
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

  if (status === 413) {
    return 'The request is too large. Choose a smaller file and try again.'
  }

  if (status === 415) {
    return 'The selected file type could not be accepted. Choose a supported file and try again.'
  }

  if (status === 401) {
    return 'Your Windows session could not be authenticated. Refresh the page and try again.'
  }

  if (status === 403) {
    return 'You do not have permission to perform this action.'
  }

  if (status === 404) {
    return 'The requested API resource is unavailable.'
  }

  if (status === 400) {
    return 'The API rejected the request. Check the supplied values and try again.'
  }

  if (status === 409) {
    return 'This information changed or the requested action is no longer allowed. Reload and try again.'
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

function createRequestBody(
  options: ApiRequestBodyOptions,
  headers: Headers,
): XMLHttpRequestBodyInit | null | undefined {
  if (options.json === undefined) {
    return options.body
  }

  if (options.body !== undefined) {
    throw new ApiError('The API request body is configured more than once.')
  }

  if (!headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  return JSON.stringify(options.json)
}

async function sendRequest(
  path: string,
  method: ApiHttpMethod,
  options: ApiFetchOptions,
  requestToken?: string,
): Promise<Response> {
  try {
    const headers = createRequestHeaders(
      method,
      options.headers,
      requestToken,
    )

    return await fetch(createApiUrl(path), {
      method,
      headers,
      body: createRequestBody(options, headers),
      credentials: 'include',
      signal: options.signal,
      cache: options.cache,
    })
  } catch (error: unknown) {
    if (options.signal?.aborted) {
      throw error
    }

    if (error instanceof ApiError) {
      throw error
    }

    throw new ApiError('Unable to connect to the API. Please try again.')
  }
}

function readOptionalString(
  value: Readonly<Record<string, unknown>>,
  propertyName: string,
): string | undefined {
  const propertyValue = value[propertyName]
  return typeof propertyValue === 'string' && propertyValue.trim().length > 0
    ? propertyValue
    : undefined
}

function readProblemErrors(
  value: Readonly<Record<string, unknown>>,
): Readonly<Record<string, readonly string[]>> {
  const errorsValue = value.errors

  if (
    typeof errorsValue !== 'object' ||
    errorsValue === null ||
    Array.isArray(errorsValue)
  ) {
    return {}
  }

  const errors: Record<string, readonly string[]> = {}

  Object.entries(errorsValue).forEach(([fieldName, messages]) => {
    if (!Array.isArray(messages)) {
      return
    }

    const safeMessages = messages.filter(
      (message): message is string =>
        typeof message === 'string' && message.trim().length > 0,
    )

    if (safeMessages.length > 0) {
      errors[fieldName] = safeMessages
    }
  })

  return errors
}

async function readProblemDetails(
  response: Response,
): Promise<ApiProblemDetails | null> {
  try {
    const responseBody: unknown = await response.clone().json()

    if (
      typeof responseBody !== 'object' ||
      responseBody === null ||
      Array.isArray(responseBody)
    ) {
      return null
    }

    const problemRecord = responseBody as Readonly<Record<string, unknown>>
    const problemStatus = problemRecord.status

    return {
      type: readOptionalString(problemRecord, 'type'),
      title: readOptionalString(problemRecord, 'title'),
      status: typeof problemStatus === 'number' ? problemStatus : undefined,
      detail: readOptionalString(problemRecord, 'detail'),
      instance: readOptionalString(problemRecord, 'instance'),
      code: readOptionalString(problemRecord, 'code'),
      errors: readProblemErrors(problemRecord),
    }
  } catch {
    return null
  }
}

function getProblemMessage(
  status: number,
  problemDetails: ApiProblemDetails | null,
): string {
  if (status < 500) {
    const detail = problemDetails?.detail?.trim()
    const title = problemDetails?.title?.trim()

    if (detail) {
      return detail
    }

    if (title) {
      return title
    }
  }

  return getHttpErrorMessage(status)
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
    const problemDetails = await readProblemDetails(response)
    return problemDetails?.type === antiforgeryValidationProblemType
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
    const problemDetails = await readProblemDetails(response)
    throw new ApiError(
      getProblemMessage(response.status, problemDetails),
      response.status,
      problemDetails,
    )
  }

  if (response.status === 204 || response.status === 205) {
    return undefined as T
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

async function requestBlob(
  path: string,
  options: ApiRequestOptions = {},
): Promise<Blob> {
  const response = await sendRequest(path, 'GET', options)

  if (!response.ok) {
    const problemDetails = await readProblemDetails(response)
    throw new ApiError(
      getProblemMessage(response.status, problemDetails),
      response.status,
      problemDetails,
    )
  }

  try {
    return await response.blob()
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
  getBlob: (path: string, options?: ApiRequestOptions) =>
    requestBlob(path, options),
  post: <T>(path: string, options?: ApiRequestBodyOptions) =>
    requestJson<T>(path, 'POST', options),
  put: <T>(path: string, options?: ApiRequestBodyOptions) =>
    requestJson<T>(path, 'PUT', options),
  patch: <T>(path: string, options?: ApiRequestBodyOptions) =>
    requestJson<T>(path, 'PATCH', options),
  delete: <T>(path: string, options?: ApiRequestBodyOptions) =>
    requestJson<T>(path, 'DELETE', options),
}
