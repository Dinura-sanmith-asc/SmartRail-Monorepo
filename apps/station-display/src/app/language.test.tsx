import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DisplayLanguageProvider, DisplayLanguageSelect, useDisplayLanguage } from './language'

function LanguageSample() {
  const { t } = useDisplayLanguage()
  return <><DisplayLanguageSelect /><p>{t('Departures')}</p></>
}

describe('station display language preference', () => {
  it('persists Sinhala and changes the passive board language', () => {
    localStorage.clear()
    render(<DisplayLanguageProvider><LanguageSample /></DisplayLanguageProvider>)
    fireEvent.change(screen.getByRole('combobox', { name: 'Language' }), { target: { value: 'si' } })
    expect(screen.getByText('පිටත්වීම්')).toBeTruthy()
    expect(localStorage.getItem('smartrail.uiLanguage')).toBe('si')
  })

  it('restores Tamil from the saved language preference', () => {
    localStorage.setItem('smartrail.uiLanguage', 'ta')
    render(<DisplayLanguageProvider><LanguageSample /></DisplayLanguageProvider>)
    expect(screen.getByText('புறப்பாடுகள்')).toBeTruthy()
  })
})
