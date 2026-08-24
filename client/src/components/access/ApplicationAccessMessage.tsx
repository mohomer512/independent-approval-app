import { Button, Card } from '@fluentui/react-components'
import {
  ArrowClockwise20Regular,
  DismissCircle24Regular,
  Shield24Regular,
} from '@fluentui/react-icons'

interface ApplicationAccessMessageProps {
  readonly title: string
  readonly description: string
  readonly variant?: 'denied' | 'error'
  readonly fullPage?: boolean
  readonly onRetry?: () => void
}

export function ApplicationAccessMessage({
  title,
  description,
  variant = 'denied',
  fullPage = false,
  onRetry,
}: ApplicationAccessMessageProps) {
  return (
    <div
      className={`access-message-shell${fullPage ? ' access-message-shell--full-page' : ''}`}
    >
      <Card className="access-message" role="alert">
        <div
          className={`access-message__icon access-message__icon--${variant}`}
          aria-hidden="true"
        >
          {variant === 'error' ? (
            <DismissCircle24Regular />
          ) : (
            <Shield24Regular />
          )}
        </div>
        <div className="access-message__copy">
          <p className="access-message__eyebrow">Independent Approval</p>
          {fullPage ? <h1>{title}</h1> : <h2>{title}</h2>}
          <p>{description}</p>
        </div>
        {onRetry ? (
          <Button
            type="button"
            appearance="primary"
            icon={<ArrowClockwise20Regular />}
            onClick={onRetry}
          >
            Retry
          </Button>
        ) : null}
      </Card>
    </div>
  )
}
