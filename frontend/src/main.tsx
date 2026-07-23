import './index.css'
import { type ReactNode, StrictMode } from 'react'
import App from './App.tsx'
import { createRoot } from 'react-dom/client'

const rootElement: HTMLElement | null = document.querySelector('#root')
if (rootElement === null) {
  throw new Error('Root element #root not found')
}

const children: ReactNode = (
  <StrictMode>
    <App />
  </StrictMode>
)

createRoot(rootElement).render(children)
