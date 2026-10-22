import { describe, expect, it } from 'vitest'
import { parseCurrentIdentity } from '../../../../src/features/staff-access/identity/currentIdentity'

const approvedRoles = ['Passenger', 'Driver', 'Station Staff', 'Controller'] as const

describe('parseCurrentIdentity', () => {
  it('accepts exactly the approved subject and roles fields', () => {
    expect(
      parseCurrentIdentity({
        subject: 'opaque-cognito-subject-42',
        roles: ['Controller'],
      }),
    ).toEqual({ subject: 'opaque-cognito-subject-42', roles: ['Controller'] })
  })

  it('accepts an opaque non-UUID subject and an empty role list', () => {
    expect(parseCurrentIdentity({ subject: 'sub:opaque/value', roles: [] })).toEqual({
      subject: 'sub:opaque/value',
      roles: [],
    })
  })

  it.each([null, undefined, [], 'identity', 42])('rejects non-object payload %j', (value) => {
    expect(() => parseCurrentIdentity(value)).toThrow()
  })

  it.each([
    {},
    { subject: '', roles: [] },
    { subject: 'subject', roles: 'Controller' },
    { subject: 'subject', roles: ['Unknown Role'] },
    { subject: 'subject', roles: ['Controller', 'Controller'] },
    { subject: 'subject', roles: [], stationIds: ['S-01'] },
    { subject: 'subject', roles: [], profile: { name: 'Unexpected' } },
  ])('rejects a payload outside the strict contract: %j', (value) => {
    expect(() => parseCurrentIdentity(value)).toThrow()
  })

  it.each(approvedRoles)('accepts the approved role label %s', (role) => {
    expect(parseCurrentIdentity({ subject: 'opaque-subject', roles: [role] }).roles).toEqual([role])
  })
})
