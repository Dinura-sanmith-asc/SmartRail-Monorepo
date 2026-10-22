import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { AccessState } from '../../../src/shared/ui/AccessState'

describe('AccessState', () => {
  it('announces loading as a status and explains it in text', () => {
    render(<AccessState state="loading" />)

    expect(screen.getByRole('status').textContent).toMatch(/loading/i)
  })

  it('announces missing access without exposing protected content', () => {
    render(<AccessState state="no-access" />)

    expect(screen.getByRole('alert').textContent).toMatch(/no access|not authorized|no permission/i)
    expect(screen.queryByText(/station [A-Z0-9]|controller operations/i)).toBeNull()
  })

  it('announces a forbidden result as an alert without success language', () => {
    render(<AccessState state="forbidden" />)

    const alert = screen.getByRole('alert')
    expect(alert.textContent).toMatch(/access denied|not authorized|no permission/i)
    expect(alert.textContent).not.toMatch(/completed successfully|saved successfully/i)
  })

  it('announces unavailable authorization as a status and offers retry', () => {
    render(<AccessState state="unavailable" onRetry={vi.fn()} />)

    expect(screen.getByRole('status').textContent).toMatch(/temporarily unavailable|access information.*unavailable/i)
    expect(screen.getByRole('button', { name: /retry|try again/i })).toBeTruthy()
  })

  it('announces offline failure as an alert and offers retry', () => {
    render(<AccessState state="offline" onRetry={vi.fn()} />)

    expect(screen.getByRole('alert').textContent).toMatch(/offline|network/i)
    expect(screen.getByRole('button', { name: /retry|try again/i })).toBeTruthy()
  })

  it('calls onRetry when the retry button is activated', async () => {
    const user = userEvent.setup()
    const onRetry = vi.fn()
    render(<AccessState state="unavailable" onRetry={onRetry} />)

    await user.click(screen.getByRole('button', { name: /retry|try again/i }))

    expect(onRetry).toHaveBeenCalledOnce()
  })

  it('supports keyboard focus and Enter activation for retry', async () => {
    const user = userEvent.setup()
    const onRetry = vi.fn()
    render(<AccessState state="offline" onRetry={onRetry} />)
    const retryButton = screen.getByRole('button', { name: /retry|try again/i })

    await user.tab()

    expect(document.activeElement).toBe(retryButton)
    await user.keyboard('{Enter}')
    expect(onRetry).toHaveBeenCalledOnce()
  })

  it('provides textual status meaning that remains available without color', () => {
    render(<AccessState state="forbidden" />)

    const alert = screen.getByRole('alert')
    expect(alert.textContent).toMatch(/access denied|not authorized|no permission/i)
    expect(alert.textContent?.trim()).not.toBe('')
  })
})
