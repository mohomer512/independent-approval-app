import { Avatar, Button, Tooltip } from '@fluentui/react-components'
import { Alert24Regular, Navigation24Regular } from '@fluentui/react-icons'
import { useCurrentUser } from '../../hooks/useCurrentUser'

interface TopHeaderProps {
  readonly pageTitle: string
  readonly onOpenNavigation: () => void
}

export function TopHeader({
  pageTitle,
  onOpenNavigation,
}: TopHeaderProps) {
  const { data: currentUser, loading, retry } = useCurrentUser()
  const authenticatedUser =
    !loading && currentUser?.isAuthenticated === true ? currentUser : null
  const isUnavailable = !loading && authenticatedUser === null
  const primaryText = authenticatedUser
    ? authenticatedUser.displayName.trim() ||
      authenticatedUser.userName ||
      authenticatedUser.accountName
    : loading
      ? 'Identifying user'
      : 'User unavailable'
  const profileLabel = authenticatedUser
    ? `Signed in as ${primaryText}, ${authenticatedUser.accountName}`
    : primaryText
  const profileStateClass = loading
    ? ' user-profile--loading'
    : isUnavailable
      ? ' user-profile--unavailable'
      : ''

  return (
    <header className="top-header">
      <div className="top-header__title-group">
        <Button
          className="top-header__menu-button"
          appearance="subtle"
          icon={<Navigation24Regular />}
          aria-label="Open navigation menu"
          onClick={onOpenNavigation}
        />
        <div>
          <span className="top-header__eyebrow">Workspace</span>
          <h1 className="top-header__title">{pageTitle}</h1>
        </div>
      </div>

      <div className="top-header__actions">
        <Tooltip content="Notifications" relationship="description">
          <Button
            className="notification-button"
            appearance="subtle"
            icon={<Alert24Regular />}
            aria-label="Notifications, 3 unread"
          />
        </Tooltip>
        <span className="notification-button__indicator" aria-hidden="true" />

        <div
          className={`user-profile${profileStateClass}`}
          aria-label={profileLabel}
          aria-busy={loading}
        >
          <Avatar
            className="user-profile__avatar"
            name={primaryText}
            color="colorful"
            size={36}
          />
          <div
            className="user-profile__details"
            aria-live="polite"
            aria-atomic="true"
          >
            <span className="user-profile__name">{primaryText}</span>
            {authenticatedUser && (
              <span className="user-profile__meta">
                {authenticatedUser.accountName}
              </span>
            )}
          </div>
          {isUnavailable && (
            <Button
              className="user-profile__retry"
              appearance="subtle"
              size="small"
              onClick={retry}
            >
              Retry
            </Button>
          )}
        </div>
      </div>
    </header>
  )
}
