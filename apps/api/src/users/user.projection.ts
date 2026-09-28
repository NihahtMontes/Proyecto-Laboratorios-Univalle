/**
 * Read-model projection for the users slice (F1 §3, §8, §11): effective ASP
 * status per membership, derived initials and the active-site role badge.
 * Pure functions shared by the service and its tests.
 */
import type { RequestIdentity } from '../identity/identity.contracts.js';
import type { AccountStatus } from '../identity/identity.contracts.js';
import { createHash } from 'node:crypto';
import type { ManagedUserRecord, ProfilePictureRef } from './user.types.js';

export function iso(d: Date | string): string {
  return d instanceof Date ? d.toISOString() : new Date(d).toISOString();
}

export interface MembershipSnapshotRow {
  site_id: string;
  site_name: string;
  role: 'Administrador' | 'Supervisor';
  membership_status: 'active' | 'suspended' | 'revoked';
  valid_from: Date | string | null;
  valid_until: Date | string | null;
  position: string | null;
  department: string | null;
  hire_date: Date | string | null;
}

export function mapMemberships(
  rows: ReadonlyArray<MembershipSnapshotRow>,
  now: Date,
  accountStatus: AccountStatus,
): ManagedUserRecord['memberships'] {
  return rows.map((row) => {
    const validFrom =
      row.valid_from === null || row.valid_from === undefined
        ? null
        : row.valid_from instanceof Date
          ? row.valid_from
          : new Date(row.valid_from);
    const validUntil =
      row.valid_until === null || row.valid_until === undefined
        ? null
        : row.valid_until instanceof Date
          ? row.valid_until
          : new Date(row.valid_until);
    let effective: 'Activo' | 'Inactivo' | 'Eliminado';
    if (accountStatus === 'deleted') effective = 'Eliminado';
    else if (row.membership_status === 'revoked') effective = 'Eliminado';
    else if (accountStatus === 'inactive') effective = 'Inactivo';
    else if (row.membership_status === 'suspended') effective = 'Inactivo';
    else if (validFrom !== null && validFrom > now) effective = 'Inactivo';
    else if (validUntil !== null && validUntil <= now) effective = 'Inactivo';
    else effective = 'Activo';
    return {
      siteId: row.site_id,
      siteName: row.site_name,
      role: row.role,
      status: row.membership_status,
      effectiveStatus: effective,
      validFrom: validFrom === null ? null : iso(validFrom),
      validUntil: validUntil === null ? null : iso(validUntil),
      position: row.position,
      department: row.department,
      hireDate:
        row.hire_date === null || row.hire_date === undefined
          ? null
          : row.hire_date instanceof Date
            ? row.hire_date.toISOString()
            : new Date(row.hire_date).toISOString(),
    };
  });
}

export function activeRoleOf(
  memberships: ReadonlyArray<MembershipSnapshotRow>,
  actor: RequestIdentity,
): 'Administrador' | 'Supervisor' | null {
  return (
    memberships.find((m) => m.site_id === actor.activeSiteId && m.membership_status !== 'revoked')
      ?.role ?? null
  );
}

export function initialsOf(first: string, last: string): string {
  const f = [...first.trim()][0] ?? '';
  const l = [...last.trim()][0] ?? '';
  return (f + l).toUpperCase();
}

/** Opaque cache validator derived from the object key (F1 §14). */
export function profilePictureRef(key: string | null | undefined): ProfilePictureRef | null {
  if (key === null || key === undefined || key === '') return null;
  return { etag: '"' + createHash('sha256').update(key).digest('hex').slice(0, 32) + '"' };
}
