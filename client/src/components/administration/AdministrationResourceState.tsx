import { Button, Spinner } from '@fluentui/react-components'
import {
  ArrowClockwise20Regular,
  DismissCircle24Regular,
  Box24Regular,
} from '@fluentui/react-icons'
import type { ReactNode } from 'react'

interface AdministrationResourceStateProps {
  readonly loading: boolean
  readonly error: string | null
  readonly empty?: boolean
  readonly loadingLabel: string
  readonly emptyTitle?: string
  readonly emptyDescription?: string
  readonly onRetry: () => void
  readonly children: ReactNode
}

export function AdministrationResourceState({
  loading,
  error,
  empty = false,
  loadingLabel,
  emptyTitle = 'Nothing to show',
  emptyDescription = 'Items will appear here when they are available.',
  onRetry,
  children,
}: AdministrationResourceStateProps) {
  if (loading) {
    return (
      <div className="administration-resource-state" role="status">
        <Spinner size="small" label={loadingLabel} />
      </div>
    )
  }

  if (error) {
    return (
      <div
        className="administration-resource-state administration-resource-state--error"
        role="alert"
      >
        <DismissCircle24Regular aria-hidden="true" />
        <div>
          <h3>Information is unavailable</h3>
          <p>{error}</p>
        </div>
        <Button
          type="button"
          appearance="primary"
          icon={<ArrowClockwise20Regular />}
          onClick={onRetry}
        >
          Retry
        </Button>
      </div>
    )
  }

  if (empty) {
    return (
      <div
        className="administration-resource-state administration-resource-state--empty"
        role="status"
      >
        <Box24Regular aria-hidden="true" />
        <div>
          <h3>{emptyTitle}</h3>
          <p>{emptyDescription}</p>
        </div>
      </div>
    )
  }

  return children
}
