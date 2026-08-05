import { FluentProvider, webLightTheme } from '@fluentui/react-components'
import { BrowserRouter } from 'react-router'
import { AppRoutes } from './AppRoutes'

export function App() {
  return (
    <FluentProvider theme={webLightTheme} className="fluent-app-root">
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </FluentProvider>
  )
}
