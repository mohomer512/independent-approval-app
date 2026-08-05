import { Avatar, Button, Tooltip } from '@fluentui/react-components'
import { Alert24Regular, Navigation24Regular } from '@fluentui/react-icons'
import type { AppUser } from '../../models'

interface TopHeaderProps {
  readonly pageTitle: string
  readonly user: AppUser
  readonly onOpenNavigation: () => void
}

export function TopHeader({
  pageTitle,
  user,
  onOpenNavigation,
}: TopHeaderProps) {
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

        <div className="user-profile" aria-label={`Signed in as ${user.name}`}>
          <Avatar
            name={user.name}
            initials={user.initials}
            color="colorful"
            size={36}
          />
          <div className="user-profile__details">
            <span className="user-profile__name">{user.name}</span>
            <span className="user-profile__meta">
              {user.username} · {user.role}
            </span>
          </div>
        </div>
      </div>
    </header>
  )
}
