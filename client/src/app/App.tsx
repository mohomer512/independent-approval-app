import {
  FluentProvider,
  webDarkTheme,
  webLightTheme,
} from '@fluentui/react-components'
import { useEffect } from 'react'
import { BrowserRouter } from 'react-router'
import { useCurrentUser } from '../hooks/useCurrentUser'
import { ApplicationSessionProvider } from './ApplicationSessionProvider'
import { AppRoutes } from './AppRoutes'

function ApplicationSurface() {
  const { data: currentUser } = useCurrentUser()
  const language = currentUser?.language ?? 'en'
  const themeName = currentUser?.theme ?? 'light'
  const direction = language === 'ar' ? 'rtl' : 'ltr'

  useEffect(() => {
    document.documentElement.lang = language
    document.documentElement.dir = direction
    document.documentElement.dataset.theme = themeName
  }, [direction, language, themeName])

  return (
    <FluentProvider
      theme={themeName === 'dark' ? webDarkTheme : webLightTheme}
      dir={direction}
      className="fluent-app-root"
    >
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </FluentProvider>
  )
}

export function App() {
  return (
    <ApplicationSessionProvider>
      <ApplicationSurface />
    </ApplicationSessionProvider>
  )
}
