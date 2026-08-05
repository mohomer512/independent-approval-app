import { Button, Tooltip } from '@fluentui/react-components'
import {
  Dismiss24Regular,
  PanelLeftContract24Regular,
  PanelLeftExpand24Regular,
  ShieldLock20Regular,
} from '@fluentui/react-icons'
import { NavLink } from 'react-router'
import { navigationItems } from '../../app/navigation'

interface NavigationSidebarProps {
  readonly collapsed: boolean
  readonly mobile?: boolean
  readonly onCollapse: () => void
  readonly onNavigate?: () => void
  readonly onClose?: () => void
}

export function NavigationSidebar({
  collapsed,
  mobile = false,
  onCollapse,
  onNavigate,
  onClose,
}: NavigationSidebarProps) {
  const isCollapsed = collapsed && !mobile

  return (
    <aside
      className={`sidebar${isCollapsed ? ' sidebar--collapsed' : ''}${
        mobile ? ' sidebar--mobile' : ''
      }`}
      aria-label="Primary navigation"
    >
      <div className="sidebar__brand-row">
        <NavLink
          to="/"
          className="sidebar__brand"
          aria-label="Independent Approval home"
          onClick={onNavigate}
        >
          <span className="brand-mark" aria-hidden="true">
            IA
          </span>
          {!isCollapsed && (
            <span className="brand-copy">
              <span className="brand-copy__name">Independent</span>
              <span className="brand-copy__name brand-copy__name--accent">
                Approval
              </span>
            </span>
          )}
        </NavLink>

        {mobile ? (
          <Button
            className="sidebar__close"
            appearance="subtle"
            icon={<Dismiss24Regular />}
            aria-label="Close navigation menu"
            onClick={onClose}
          />
        ) : null}
      </div>

      <nav className="sidebar__navigation" aria-label="Main menu">
        <span className={`sidebar__section-label${isCollapsed ? ' sr-only' : ''}`}>
          Menu
        </span>
        <ul className="sidebar__list">
          {navigationItems.map((item) => {
            const Icon = item.icon

            return (
              <li key={item.path}>
                <Tooltip
                  content={item.label}
                  relationship="label"
                  positioning="after"
                >
                  <NavLink
                    to={item.path}
                    end={item.end}
                    className={({ isActive }) =>
                      `sidebar__link${isActive ? ' sidebar__link--active' : ''}`
                    }
                    aria-label={isCollapsed ? item.label : undefined}
                    onClick={onNavigate}
                  >
                    <Icon className="sidebar__link-icon" aria-hidden="true" />
                    {!isCollapsed && <span>{item.label}</span>}
                  </NavLink>
                </Tooltip>
              </li>
            )
          })}
        </ul>
      </nav>

      <div className="sidebar__footer">
        {!isCollapsed && (
          <div className="environment-note">
            <ShieldLock20Regular aria-hidden="true" />
            <div>
              <span className="environment-note__title">On-premises</span>
              <span className="environment-note__text">Secure workspace</span>
            </div>
          </div>
        )}
        {!mobile && (
          <Tooltip
            content={isCollapsed ? 'Expand navigation' : 'Collapse navigation'}
            relationship="label"
            positioning="after"
          >
            <Button
              className="sidebar__collapse-button"
              appearance="subtle"
              icon={
                isCollapsed ? (
                  <PanelLeftExpand24Regular />
                ) : (
                  <PanelLeftContract24Regular />
                )
              }
              aria-label={
                isCollapsed ? 'Expand navigation' : 'Collapse navigation'
              }
              onClick={onCollapse}
            >
              {!isCollapsed ? 'Collapse' : undefined}
            </Button>
          </Tooltip>
        )}
      </div>
    </aside>
  )
}
