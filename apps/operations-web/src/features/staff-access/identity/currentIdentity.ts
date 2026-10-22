export const STAFF_ROLES = ['Passenger', 'Driver', 'Station Staff', 'Controller'] as const

export type StaffRole = (typeof STAFF_ROLES)[number]

export type CurrentIdentity = {
  subject: string
  roles: StaffRole[]
}

export class InvalidIdentityError extends Error {
  constructor() {
    super('The identity response does not match the approved contract.')
    this.name = 'InvalidIdentityError'
  }
}

export function parseCurrentIdentity(value: unknown): CurrentIdentity {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new InvalidIdentityError()

  const payload = value as Record<string, unknown>
  if (Object.keys(payload).length !== 2 || !Object.hasOwn(payload, 'subject') || !Object.hasOwn(payload, 'roles')) {
    throw new InvalidIdentityError()
  }

  if (typeof payload.subject !== 'string' || !payload.subject.trim() || !Array.isArray(payload.roles)) {
    throw new InvalidIdentityError()
  }

  const roles: StaffRole[] = []
  for (const role of payload.roles) {
    if (typeof role !== 'string' || !STAFF_ROLES.includes(role as StaffRole) || roles.includes(role as StaffRole)) {
      throw new InvalidIdentityError()
    }
    roles.push(role as StaffRole)
  }

  return { subject: payload.subject, roles }
}
