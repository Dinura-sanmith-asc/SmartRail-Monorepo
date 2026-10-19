import { AppRouter } from './router'
import { RuntimeConfigProvider } from './providers'

export default function App() {
  return (
    <RuntimeConfigProvider>
      <AppRouter />
    </RuntimeConfigProvider>
  )
}
