export type AntiforgeryTokenProvider = () => Promise<string>

export interface AntiforgeryService {
  readonly getRequestToken: (
    tokenProvider: AntiforgeryTokenProvider,
  ) => Promise<string>
  readonly invalidateRequestToken: (requestToken: string) => void
}

let cachedRequestToken: string | null = null
let pendingTokenRequest: Promise<string> | null = null

export const antiforgeryService: AntiforgeryService = {
  getRequestToken: (tokenProvider) => {
    if (cachedRequestToken !== null) {
      return Promise.resolve(cachedRequestToken)
    }

    if (pendingTokenRequest !== null) {
      return pendingTokenRequest
    }

    const tokenRequest = Promise.resolve()
      .then(tokenProvider)
      .then((requestToken) => {
        if (requestToken.trim().length === 0) {
          throw new Error('The API returned an empty antiforgery token.')
        }

        cachedRequestToken = requestToken
        return requestToken
      })
      .finally(() => {
        if (pendingTokenRequest === tokenRequest) {
          pendingTokenRequest = null
        }
      })

    pendingTokenRequest = tokenRequest
    return tokenRequest
  },
  invalidateRequestToken: (requestToken) => {
    if (cachedRequestToken === requestToken) {
      cachedRequestToken = null
    }
  },
}
