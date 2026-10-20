import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { LanguageProvider, LanguageSelect, useLanguage } from '../../src/shared/i18n/language'

function Sample() {
  const { t } = useLanguage()
  return <><LanguageSelect /><p>{t('Your assigned journeys')}</p></>
}

describe('UI language preference', () => {
  it('persists Sinhala and displays a translated driver heading', () => {
    localStorage.clear()
    render(<LanguageProvider><Sample /></LanguageProvider>)
    fireEvent.change(screen.getByRole('combobox', { name: 'Language' }), { target: { value: 'si' } })
    expect(screen.getByText('ඔබට පැවරූ ගමන්')).toBeTruthy()
    expect(localStorage.getItem('smartrail.uiLanguage')).toBe('si')
    expect(document.documentElement.lang).toBe('si')
  })

  it('displays a Tamil translation from the persisted preference', () => {
    localStorage.setItem('smartrail.uiLanguage', 'ta')
    render(<LanguageProvider><Sample /></LanguageProvider>)
    expect(screen.getByText('உங்களுக்கு ஒதுக்கப்பட்ட பயணங்கள்')).toBeTruthy()
  })
})
