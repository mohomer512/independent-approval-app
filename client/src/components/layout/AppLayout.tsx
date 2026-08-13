import { Spinner } from '@fluentui/react-components'
import { Suspense, useEffect, useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { Outlet, useLocation } from 'react-router'
import { getPageTitle } from '../../app/navigation'
import { NavigationSidebar } from './NavigationSidebar'
import { TopHeader } from './TopHeader'

export function AppLayout() {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false)
  const [isMobileNavigationOpen, setIsMobileNavigationOpen] = useState(false)
  const mobileNavigationRef = useRef<HTMLDivElement>(null)
  const navigationTriggerRef = useRef<HTMLElement | null>(null)
  const location = useLocation()
  const pageTitle = getPageTitle(location.pathname)

  useEffect(() => {
    if (isMobileNavigationOpen) {
      mobileNavigationRef.current?.focus()
    }
  }, [isMobileNavigationOpen])

  const openMobileNavigation = () => {
    if (document.activeElement instanceof HTMLElement) {
      navigationTriggerRef.current = document.activeElement
    }
    setIsMobileNavigationOpen(true)
  }

  const closeMobileNavigation = () => {
    setIsMobileNavigationOpen(false)
    window.requestAnimationFrame(() => navigationTriggerRef.current?.focus())
  }

  const handleMobileNavigationKeyDown = (
    event: KeyboardEvent<HTMLDivElement>,
  ) => {
    if (event.key === 'Escape') {
      event.preventDefault()
      closeMobileNavigation()
      return
    }

    if (event.key !== 'Tab' || !mobileNavigationRef.current) {
      return
    }

    const focusableElements = Array.from(
      mobileNavigationRef.current.querySelectorAll<HTMLElement>(
        'a[href], button:not([disabled]):not([tabindex="-1"])',
      ),
    )
    const firstElement = focusableElements.at(0)
    const lastElement = focusableElements.at(-1)

    if (!firstElement || !lastElement) {
      return
    }

    if (event.shiftKey && document.activeElement === firstElement) {
      event.preventDefault()
      lastElement.focus()
    } else if (!event.shiftKey && document.activeElement === lastElement) {
      event.preventDefault()
      firstElement.focus()
    }
  }

  return (
    <div
      className={`app-shell${isSidebarCollapsed ? ' app-shell--collapsed' : ''}`}
      dir="ltr"
    >
      <a className="skip-link" href="#main-content">
        Skip to main content
      </a>
      <NavigationSidebar
        collapsed={isSidebarCollapsed}
        onCollapse={() => setIsSidebarCollapsed((isCollapsed) => !isCollapsed)}
      />

      {isMobileNavigationOpen && (
        <div
          ref={mobileNavigationRef}
          className="mobile-navigation-layer"
          role="dialog"
          aria-modal="true"
          aria-label="Navigation menu"
          tabIndex={-1}
          onKeyDown={handleMobileNavigationKeyDown}
        >
          <button
            type="button"
            className="mobile-navigation-backdrop"
            aria-label="Close navigation menu"
            tabIndex={-1}
            onClick={closeMobileNavigation}
          />
          <NavigationSidebar
            mobile
            collapsed={false}
            onCollapse={() => undefined}
            onNavigate={closeMobileNavigation}
            onClose={closeMobileNavigation}
          />
        </div>
      )}

      <div className="app-shell__content">
        <TopHeader
          pageTitle={pageTitle}
          onOpenNavigation={openMobileNavigation}
        />
        <main className="page-content" id="main-content">
          <Suspense
            fallback={
              <div className="route-loading">
                <Spinner label="Loading workspace" />
              </div>
            }
          >
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  )
}
