import { AppRouter } from './router'
import { ConfiguredAuthProvider, RuntimeConfigProvider } from './providers'
import { LanguageProvider } from '../shared/i18n/language'

export default function App() {
  return (
    <LanguageProvider><RuntimeConfigProvider>
      <ConfiguredAuthProvider>
        <AppRouter />
      </ConfiguredAuthProvider>
    </RuntimeConfigProvider></LanguageProvider>
  )
}
