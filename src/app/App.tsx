import { AppRouter } from './router'
import { ConfiguredAuthProvider, RuntimeConfigProvider } from './providers'

export default function App() {
  return (
    <RuntimeConfigProvider>
      <ConfiguredAuthProvider>
        <AppRouter />
      </ConfiguredAuthProvider>
    </RuntimeConfigProvider>
  )
}
