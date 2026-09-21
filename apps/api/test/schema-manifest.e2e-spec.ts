/**
 * MIG-F3-PG-MANIFEST-004 — semantic normalizer and schema-manifest verification specs.
 *
 * Pure tests (no DB, no env, no sockets). Covers the PostgreSQL 18.6 catalog
 * equivalences that produced false diffs in `db:migrate:verify`:
 *  - `x IN (...)` vs `x = ANY (ARRAY['...'::text, ...])`,
 *  - redundant parentheses around OR terms,
 *  - `USING btree` and functional expressions in index definitions,
 *  - `lu_session.security_version` has no default in SQL 0001.
 *
 * Negatives prove that the normalisers are fail-closed: distinct values, columns,
 * operators, uniqueness or partial predicates still produce diffs.
 */
import {
  AUTH_SECURITY_SCHEMA_MANIFEST,
  IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST,
  TENANT_ROUTE_SCHEMA_MANIFEST,
  normalizeCheckDefinition,
  normalizeDefault,
  normalizeIndexDefinition,
  parseIndexDef,
  verifySchema,
  type ColumnSpec,
  type TableManifest,
} from '../src/database/schema-manifest.js';
import {
  AUTH_SECURITY_TARGET_TABLES,
  CONTROL_PLANE_TABLES,
  TARGET_TABLES,
} from '../src/database/migration-plan.js';

describe('0002 auth security manifest contract', () => {
  it('is exhaustive over baseline plus both additive security tables', () => {
    expect(Object.keys(AUTH_SECURITY_SCHEMA_MANIFEST.tables)).toEqual([
      ...TARGET_TABLES,
      ...AUTH_SECURITY_TARGET_TABLES,
    ]);
    expect(
      AUTH_SECURITY_SCHEMA_MANIFEST.tables.lu_auth_rate_limit.columns.map((column) => column.name),
    ).toEqual([
      'scope',
      'key_hash',
      'attempt_count',
      'window_started_at',
      'reset_at',
      'updated_at',
    ]);
    expect(
      AUTH_SECURITY_SCHEMA_MANIFEST.tables.lu_security_event.columns.map((column) => column.name),
    ).toEqual([
      'id',
      'event_type',
      'user_id',
      'site_id',
      'subject_hash',
      'ip_hash',
      'occurred_at',
      'metadata',
    ]);
  });

  it('pins append-only audit event types including the initial admin bootstrap', () => {
    const definitions = AUTH_SECURITY_SCHEMA_MANIFEST.tables.lu_security_event.checks.map(
      (check) => check.definition,
    );
    expect(definitions.some((definition) => definition.includes("'admin_bootstrap'"))).toBe(true);
    expect(
      AUTH_SECURITY_SCHEMA_MANIFEST.tables.lu_security_event.indexes.map((index) => index.name),
    ).toEqual([
      'pk_lu_security_event',
      'ix_lu_security_event_type_time',
      'ix_lu_security_event_user_time',
    ]);
  });
});

describe('0003 tenant route manifest contract', () => {
  it('is exhaustive and never stores a tenant connection string', () => {
    expect(Object.keys(TENANT_ROUTE_SCHEMA_MANIFEST.tables)).toEqual([...CONTROL_PLANE_TABLES]);
    const route = TENANT_ROUTE_SCHEMA_MANIFEST.tables.lu_tenant_route;
    expect(route.columns.map((column) => column.name)).toEqual([
      'site_id',
      'runtime_secret_reference',
      'writer_label',
      'state',
      'schema_version',
      'last_health_at',
      'created_at',
      'updated_at',
    ]);
    expect(
      route.columns.some((column) => /dsn|connection|string|password/i.test(column.name)),
    ).toBe(false);
    expect(route.foreignKeys).toEqual([
      {
        columns: ['site_id'],
        referencesTable: 'lu_site',
        referencesColumns: ['id'],
        deleteAction: 'RESTRICT',
      },
    ]);
  });
});

describe('0001 manifest column contract', () => {
  const expectedColumns: Readonly<Record<(typeof TARGET_TABLES)[number], readonly string[]>> = {
    lu_site: ['id', 'code', 'name', 'status', 'created_at', 'updated_at'],
    lu_user: [
      'id',
      'email',
      'full_name',
      'password_hash',
      'is_super_admin',
      'status',
      'security_version',
      'created_at',
      'updated_at',
    ],
    lu_site_membership: [
      'user_id',
      'site_id',
      'role',
      'status',
      'valid_from',
      'valid_until',
      'created_at',
      'updated_at',
    ],
    lu_session: [
      'id',
      'token_hash',
      'user_id',
      'active_site_id',
      'security_version',
      'created_at',
      'last_seen_at',
      'idle_expires_at',
      'absolute_expires_at',
      'revoked_at',
      'revocation_reason',
    ],
  };

  it.each(TARGET_TABLES)('matches the literal 0001 columns for %s', (table) => {
    expect(
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST.tables[table].columns.map((column) => column.name),
    ).toEqual(expectedColumns[table]);
  });
});

describe('normalizeCheckDefinition: PG18 IN/ANY equivalence', () => {
  it('canonicalises IN and = ANY (ARRAY[...]) to the same value', () => {
    const inForm = normalizeCheckDefinition(
      "CHECK (status IN ('provisioning', 'active', 'migrating', 'degraded', 'disabled'))",
    );
    const anyForm = normalizeCheckDefinition(
      "CHECK (status = ANY (ARRAY['provisioning'::text, 'active'::text, 'migrating'::text, 'degraded'::text, 'disabled'::text]))",
    );
    expect(inForm).toBe(anyForm);
    expect(inForm).toBe(
      "status in ('provisioning', 'active', 'migrating', 'degraded', 'disabled')",
    );
  });

  it('canonicalises mixed-case and extra-whitespace ANY forms deterministically', () => {
    const a = normalizeCheckDefinition(
      "CHECK (role = ANY( ARRAY[ 'Administrador'::text , 'Supervisor'::text ] ))",
    );
    const b = normalizeCheckDefinition("CHECK (role IN ('Administrador', 'Supervisor'))");
    expect(a).toBe(b);
    expect(a).toBe("role in ('administrador', 'supervisor')");
  });
});

describe('normalizeCheckDefinition: redundant OR parentheses', () => {
  it('strips outer redundant parentheses around an OR chain', () => {
    const wrapped = normalizeCheckDefinition(
      'CHECK ((revoked_at IS NULL OR revoked_at >= created_at))',
    );
    const plain = normalizeCheckDefinition(
      'CHECK (revoked_at IS NULL OR revoked_at >= created_at)',
    );
    expect(wrapped).toBe(plain);
    expect(wrapped).toBe('revoked_at is null or revoked_at >= created_at');
  });

  it('strips per-term redundant parentheses in an OR chain', () => {
    const wrapped = normalizeCheckDefinition(
      'CHECK ((revoked_at IS NULL) OR (revoked_at >= created_at))',
    );
    const plain = normalizeCheckDefinition(
      'CHECK (revoked_at IS NULL OR revoked_at >= created_at)',
    );
    expect(wrapped).toBe(plain);
  });

  it('flattens nested OR chains', () => {
    const nested = normalizeCheckDefinition(
      'CHECK ((revoked_at IS NULL OR revoked_at >= created_at) OR (revoked_at IS NOT NULL))',
    );
    const flat = normalizeCheckDefinition(
      'CHECK (revoked_at IS NULL OR revoked_at >= created_at OR revoked_at IS NOT NULL)',
    );
    expect(nested).toBe(flat);
  });

  it('makes AND-inside-OR precedence explicit in the canonical form', () => {
    const explicit = normalizeCheckDefinition('CHECK ((a = 1 AND b = 2) OR c = 3)');
    const implicit = normalizeCheckDefinition('CHECK (a = 1 AND b = 2 OR c = 3)');
    // Both are semantically equivalent in SQL and therefore normalise to the
    // same fail-closed canonical representation.
    expect(explicit).toBe(implicit);
    expect(explicit).toBe('(a = 1 and b = 2) or c = 3');
  });

  it('preserves parentheses that change precedence (OR inside AND)', () => {
    const orInsideAnd = normalizeCheckDefinition('CHECK (a = 1 AND (b = 2 OR c = 3))');
    const differentPrecedence = normalizeCheckDefinition('CHECK (a = 1 AND b = 2 OR c = 3)');
    expect(orInsideAnd).not.toBe(differentPrecedence);
    expect(orInsideAnd).toBe('a = 1 and (b = 2 or c = 3)');
  });
});

describe('normalizeCheckDefinition: fail-closed negatives', () => {
  it('distinguishes an extra value in the IN list', () => {
    const a = normalizeCheckDefinition("CHECK (status IN ('active'))");
    const b = normalizeCheckDefinition("CHECK (status IN ('active', 'disabled'))");
    expect(a).not.toBe(b);
  });

  it('distinguishes a missing value in the IN list', () => {
    const a = normalizeCheckDefinition("CHECK (status IN ('active', 'disabled', 'suspended'))");
    const b = normalizeCheckDefinition("CHECK (status IN ('active', 'disabled'))");
    expect(a).not.toBe(b);
  });

  it('distinguishes a different column', () => {
    const a = normalizeCheckDefinition("CHECK (status IN ('active'))");
    const b = normalizeCheckDefinition("CHECK (role IN ('active'))");
    expect(a).not.toBe(b);
  });

  it('distinguishes a different operator', () => {
    const a = normalizeCheckDefinition('CHECK (security_version >= 0)');
    const b = normalizeCheckDefinition('CHECK (security_version > 0)');
    expect(a).not.toBe(b);
  });

  it('distinguishes ANY arrays with different values', () => {
    const a = normalizeCheckDefinition(
      "CHECK (status = ANY (ARRAY['active'::text, 'disabled'::text]))",
    );
    const b = normalizeCheckDefinition(
      "CHECK (status = ANY (ARRAY['active'::text, 'suspended'::text]))",
    );
    expect(a).not.toBe(b);
  });

  it('distinguishes IN from NOT IN', () => {
    const a = normalizeCheckDefinition("CHECK (status IN ('active'))");
    const b = normalizeCheckDefinition("CHECK (status NOT IN ('active'))");
    expect(a).not.toBe(b);
  });

  it('distinguishes different OR terms', () => {
    const a = normalizeCheckDefinition('CHECK (a = 1 OR b = 2)');
    const b = normalizeCheckDefinition('CHECK (a = 1 OR b = 3)');
    expect(a).not.toBe(b);
  });
});

describe('normalizeDefault', () => {
  it('normalises CURRENT_TIMESTAMP and text casts to canonical forms', () => {
    expect(normalizeDefault('CURRENT_TIMESTAMP')).toBe('current_timestamp');
    expect(normalizeDefault("'active'::text")).toBe("'active'");
  });

  it('returns null for missing defaults', () => {
    expect(normalizeDefault(null)).toBeNull();
  });

  it('normalises boolean and numeric defaults', () => {
    expect(normalizeDefault('false')).toBe('false');
    expect(normalizeDefault('0')).toBe('0');
    expect(normalizeDefault("'active'::text")).toBe("'active'");
  });
});

describe('parseIndexDef: PG18 functional-index and USING btree equivalence', () => {
  it('parses a plain btree index with columns', () => {
    const parsed = parseIndexDef(
      'CREATE INDEX ix_lu_site_membership_site_status ON public.lu_site_membership USING btree (site_id, status)',
      'lu_site_membership',
      'ix_lu_site_membership_site_status',
    );
    expect(parsed).toEqual({
      name: 'ix_lu_site_membership_site_status',
      table: 'lu_site_membership',
      columns: ['site_id', 'status'],
      unique: false,
      where: null,
    });
  });

  it('parses a unique functional index with USING btree', () => {
    const parsed = parseIndexDef(
      'CREATE UNIQUE INDEX ux_lu_site_code_lower ON public.lu_site USING btree (lower(code))',
      'lu_site',
      'ux_lu_site_code_lower',
    );
    expect(parsed).toEqual({
      name: 'ux_lu_site_code_lower',
      table: 'lu_site',
      columns: ['lower(code)'],
      unique: true,
      where: null,
    });
  });

  it('parses a partial index with a WHERE predicate', () => {
    const parsed = parseIndexDef(
      'CREATE INDEX ix_lu_session_active_expiry ON public.lu_session USING btree (absolute_expires_at, idle_expires_at) WHERE revoked_at IS NULL',
      'lu_session',
      'ix_lu_session_active_expiry',
    );
    expect(parsed).toEqual({
      name: 'ix_lu_session_active_expiry',
      table: 'lu_session',
      columns: ['absolute_expires_at', 'idle_expires_at'],
      unique: false,
      where: 'revoked_at is null',
    });
  });

  it('parses pg_get_indexdef output with extra whitespace and schema qualification', () => {
    const parsed = parseIndexDef(
      'CREATE UNIQUE INDEX   ux_lu_user_email_lower   ON   public.lu_user   USING   btree   (lower(email))',
      'lu_user',
      'ux_lu_user_email_lower',
    );
    expect(parsed).toEqual({
      name: 'ux_lu_user_email_lower',
      table: 'lu_user',
      columns: ['lower(email)'],
      unique: true,
      where: null,
    });
  });

  it('returns undefined for malformed index definitions', () => {
    expect(parseIndexDef('not an index', 't', 'ix')).toBeUndefined();
    expect(parseIndexDef('CREATE INDEX ix ON t (a', 't', 'ix')).toBeUndefined();
    expect(
      parseIndexDef('CREATE INDEX ix ON t USING btree (a) GARBAGE', 't', 'ix'),
    ).toBeUndefined();
  });
});

describe('parseIndexDef: fail-closed negatives', () => {
  it('distinguishes unique from non-unique functional indexes', () => {
    const unique = parseIndexDef(
      'CREATE UNIQUE INDEX ux ON public.t USING btree (lower(code))',
      't',
      'ux',
    );
    const plain = parseIndexDef('CREATE INDEX ux ON public.t USING btree (lower(code))', 't', 'ux');
    expect(unique?.unique).toBe(true);
    expect(plain?.unique).toBe(false);
  });

  it('distinguishes different partial predicates', () => {
    const a = parseIndexDef(
      'CREATE INDEX ix ON public.t USING btree (a) WHERE revoked_at IS NULL',
      't',
      'ix',
    );
    const b = parseIndexDef(
      'CREATE INDEX ix ON public.t USING btree (a) WHERE revoked_at IS NOT NULL',
      't',
      'ix',
    );
    expect(a?.where).not.toBe(b?.where);
  });

  it('distinguishes different functional expressions', () => {
    const a = parseIndexDef('CREATE INDEX ix ON public.t USING btree (lower(code))', 't', 'ix');
    const b = parseIndexDef('CREATE INDEX ix ON public.t USING btree (upper(code))', 't', 'ix');
    expect(a?.columns).not.toEqual(b?.columns);
  });

  it('distinguishes different column sets', () => {
    const a = parseIndexDef('CREATE INDEX ix ON public.t USING btree (a, b)', 't', 'ix');
    const b = parseIndexDef('CREATE INDEX ix ON public.t USING btree (a, c)', 't', 'ix');
    expect(a?.columns).not.toEqual(b?.columns);
  });
});

describe('normalizeIndexDefinition', () => {
  it('strips public schema and lowercases', () => {
    const normalized = normalizeIndexDefinition('CREATE INDEX ix ON public.t USING btree (a)');
    expect(normalized).toBe('create index ix on t using btree (a)');
  });
});

describe('IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST: lu_session.security_version default', () => {
  it('expects no default because SQL 0001 omits DEFAULT 0 on lu_session.security_version', () => {
    const session = IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_session;
    const column = session.columns.find((c) => c.name === 'security_version');
    expect(column).toBeDefined();
    expect(column?.columnDefault).toBeNull();
  });

  it('still expects default 0 on lu_user.security_version', () => {
    const user = IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_user;
    const column = user.columns.find((c) => c.name === 'security_version');
    expect(column?.columnDefault).toBe('0');
  });
});

describe('verifySchema: exhaustive missing/extra detection', () => {
  type QueryFilter = (text: string, values?: unknown[]) => unknown[] | undefined;

  function buildClient(filters: QueryFilter[]): {
    query: <T extends Record<string, unknown>>(
      text: string,
      values?: unknown[],
    ) => Promise<{ rows: T[] }>;
  } {
    return {
      async query<T extends Record<string, unknown>>(text: string, values?: unknown[]) {
        for (const filter of filters) {
          const rows = filter(text, values);
          if (rows !== undefined) {
            return { rows: rows as T[] };
          }
        }
        return { rows: [] as T[] };
      },
    };
  }

  function columnRow(table: string, column: ColumnSpec) {
    return {
      table_name: table,
      column_name: column.name,
      data_type: column.dataType,
      udt_name: column.udtName,
      character_maximum_length: column.charMaxLength,
      is_nullable: column.isNullable ? 'YES' : 'NO',
      column_default: column.columnDefault,
    };
  }

  function baseFiltersFor(manifest: typeof IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST): QueryFilter[] {
    const colRows: Record<string, unknown>[] = [];
    const conRows: Record<string, unknown>[] = [];
    const idxRows: Record<string, unknown>[] = [];
    for (const table of TARGET_TABLES) {
      const spec = manifest.tables[table];
      for (const col of spec.columns) {
        colRows.push(columnRow(table, col));
      }
      conRows.push({
        table_name: table,
        constraint_name: `${table}_pkey`,
        constraint_type: 'p',
        definition: `PRIMARY KEY (${spec.primaryKey.columns.join(', ')})`,
      });
      for (const u of spec.uniques) {
        conRows.push({
          table_name: table,
          constraint_name: `${table}_${u.columns.join('_')}_key`,
          constraint_type: 'u',
          definition: `UNIQUE (${u.columns.join(', ')})`,
        });
      }
      for (const fk of spec.foreignKeys) {
        conRows.push({
          table_name: table,
          constraint_name: `fk_${table}_${fk.columns.join('_')}`,
          constraint_type: 'f',
          definition: `FOREIGN KEY (${fk.columns.join(', ')}) REFERENCES ${fk.referencesTable}(${fk.referencesColumns.join(', ')}) ON DELETE ${fk.deleteAction}`,
        });
      }
      for (const c of spec.checks) {
        conRows.push({
          table_name: table,
          constraint_name: `chk_${table}_${c.definition.slice(0, 20).replace(/\W/g, '_')}`,
          constraint_type: 'c',
          definition: `CHECK (${c.definition})`,
        });
      }
      for (const ix of spec.indexes) {
        const unique = ix.unique ? 'UNIQUE ' : '';
        const where = ix.where === null ? '' : ` WHERE ${ix.where}`;
        idxRows.push({
          index_name: ix.name,
          table_name: table,
          indexdef: `CREATE ${unique}INDEX ${ix.name} ON public.${table} USING btree (${ix.columns.join(', ')})${where}`,
        });
      }
    }

    return [
      (text) => (text.includes('information_schema.columns') ? colRows : undefined),
      (text) => (text.includes('pg_constraint') ? conRows : undefined),
      (text) => (text.includes('pg_index') ? idxRows : undefined),
    ];
  }

  it('passes when the catalog exactly matches the manifest', async () => {
    const client = buildClient(baseFiltersFor(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST)) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(true);
    expect(result.diffs).toEqual([]);
    expect(result.actualTableNames).toEqual([...TARGET_TABLES]);
  });

  it('reports a missing column', async () => {
    const manifest = structuredClone(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    const luUser = manifest.tables.lu_user;
    const trimmedColumns = luUser.columns.filter((c) => c.name !== 'full_name');
    (manifest.tables.lu_user as TableManifest) = {
      ...luUser,
      columns: trimmedColumns,
    };
    const client = buildClient(
      baseFiltersFor(manifest as typeof IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST),
    ) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(result.diffs).toContain('lu_user.full_name: column is missing');
  });

  it('reports an unexpected extra column', async () => {
    const manifest = structuredClone(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    const luUser = manifest.tables.lu_user;
    const extraColumn: ColumnSpec = {
      name: 'phone',
      dataType: 'text',
      udtName: 'text',
      charMaxLength: null,
      isNullable: true,
      columnDefault: null,
    };
    (manifest.tables.lu_user as TableManifest) = {
      ...luUser,
      columns: [...luUser.columns, extraColumn],
    };
    const client = buildClient(
      baseFiltersFor(manifest as typeof IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST),
    ) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(result.diffs).toContain('lu_user.phone: unexpected extra column');
  });

  it('reports a missing index', async () => {
    const manifest = structuredClone(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    const luSession = manifest.tables.lu_session;
    (manifest.tables.lu_session as TableManifest) = {
      ...luSession,
      indexes: luSession.indexes.filter((i) => i.name !== 'ix_lu_session_user'),
    };
    const client = buildClient(
      baseFiltersFor(manifest as typeof IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST),
    ) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(
      result.diffs.some((d) => d.includes('ix_lu_session_user') && d.includes('missing index')),
    ).toBe(true);
  });

  it('reports an unexpected extra index', async () => {
    const manifest = structuredClone(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    const luSession = manifest.tables.lu_session;
    (manifest.tables.lu_session as TableManifest) = {
      ...luSession,
      indexes: [
        ...luSession.indexes,
        {
          name: 'ix_backdoor',
          table: 'lu_session',
          columns: ['user_id'],
          unique: false,
          where: null,
        },
      ],
    };
    const client = buildClient(
      baseFiltersFor(manifest as typeof IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST),
    ) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(
      result.diffs.some((d) => d.includes('ix_backdoor') && d.includes('unexpected index')),
    ).toBe(true);
  });

  it('reports an unexpected unparseable index (e.g. INCLUDE/options) as a fail-closed diff', async () => {
    const filters = baseFiltersFor(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    const indexesFilter = filters.find((f) => f('SELECT * FROM pg_index', []) !== undefined);
    expect(indexesFilter).toBeDefined();
    const customFilters: QueryFilter[] = [
      (text, values) => {
        if (!text.includes('pg_index')) return undefined;
        const rows = indexesFilter!(text, values) ?? [];
        return [
          ...rows,
          {
            index_name: 'ix_evil_include',
            table_name: 'lu_session',
            indexdef:
              'CREATE INDEX ix_evil_include ON public.lu_session USING btree (user_id) INCLUDE (created_at)',
          },
        ];
      },
      ...filters,
    ];
    const client = buildClient(customFilters) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(
      result.diffs.some(
        (d) => d.includes('ix_evil_include') && d.includes('unexpected unparseable index'),
      ),
    ).toBe(true);
  });

  it('reports an expected index with an unparseable catalog definition as fail-closed', async () => {
    const filters = baseFiltersFor(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    const indexesFilter = filters.find((f) => f('SELECT * FROM pg_index', []) !== undefined);
    expect(indexesFilter).toBeDefined();
    const customFilters: QueryFilter[] = [
      (text, values) => {
        if (!text.includes('pg_index')) return undefined;
        const rows = indexesFilter!(text, values) ?? [];
        return rows.map((row) =>
          (row as { index_name: string }).index_name === 'ix_lu_session_user'
            ? {
                index_name: 'ix_lu_session_user',
                table_name: 'lu_session',
                indexdef:
                  'CREATE INDEX ix_lu_session_user ON public.lu_session USING btree (user_id) WITH (fillfactor=70)',
              }
            : row,
        );
      },
      ...filters,
    ];
    const client = buildClient(customFilters) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(
      result.diffs.some(
        (d) =>
          d.includes('ix_lu_session_user') &&
          d.includes('unparseable definition') &&
          !d.includes('missing index'),
      ),
    ).toBe(true);
  });

  it('reports a missing CHECK constraint', async () => {
    const manifest = structuredClone(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    const luUser = manifest.tables.lu_user;
    (manifest.tables.lu_user as TableManifest) = {
      ...luUser,
      checks: luUser.checks.filter((c) => !c.definition.includes('security_version')),
    };
    const client = buildClient(
      baseFiltersFor(manifest as typeof IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST),
    ) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(
      result.diffs.some((d) => d.includes('missing CHECK') && d.includes('security_version')),
    ).toBe(true);
  });

  it('reports an unexpected extra CHECK constraint', async () => {
    const manifest = structuredClone(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    const luUser = manifest.tables.lu_user;
    (manifest.tables.lu_user as TableManifest) = {
      ...luUser,
      checks: [...luUser.checks, { definition: normalizeCheckDefinition("email LIKE '%@%.%'") }],
    };
    const client = buildClient(
      baseFiltersFor(manifest as typeof IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST),
    ) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(result.diffs.some((d) => d.includes('unexpected CHECK') && d.includes('like'))).toBe(
      true,
    );
  });

  it('reports a column default mismatch', async () => {
    const manifest = structuredClone(IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    const luUser = manifest.tables.lu_user;
    const badColumns = luUser.columns.map((c) =>
      c.name === 'security_version' ? { ...c, columnDefault: normalizeDefault('1') } : c,
    );
    (manifest.tables.lu_user as TableManifest) = { ...luUser, columns: badColumns };
    const client = buildClient(
      baseFiltersFor(manifest as typeof IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST),
    ) as never;
    const result = await verifySchema(client, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(result.diffs).toContain('lu_user.security_version: expected default 0, got 1');
  });
});

describe('verifySchema: PostgreSQL 18.6 catalog forms', () => {
  function pg18Client(): {
    query: <T extends Record<string, unknown>>(
      text: string,
      values?: unknown[],
    ) => Promise<{ rows: T[] }>;
  } {
    return {
      async query<T extends Record<string, unknown>>(text: string, values?: unknown[]) {
        void values;
        const rows: unknown[] = [];
        if (text.includes('information_schema.columns')) {
          for (const table of TARGET_TABLES) {
            const spec = IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST.tables[table];
            let ordinal = 1;
            for (const col of spec.columns) {
              let defaultValue: string | null = col.columnDefault;
              if (
                col.columnDefault === null &&
                col.name === 'security_version' &&
                table === 'lu_session'
              ) {
                defaultValue = null;
              }
              rows.push({
                table_name: table,
                column_name: col.name,
                data_type: col.dataType,
                udt_name: col.udtName,
                character_maximum_length: col.charMaxLength,
                is_nullable: col.isNullable ? 'YES' : 'NO',
                column_default: defaultValue,
                ordinal_position: ordinal,
              });
              ordinal += 1;
            }
          }
        } else if (text.includes('pg_constraint')) {
          for (const table of TARGET_TABLES) {
            const spec = IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST.tables[table];
            rows.push({
              table_name: table,
              constraint_name: `${table}_pkey`,
              constraint_type: 'p',
              definition: `PRIMARY KEY (${spec.primaryKey.columns.join(', ')})`,
            });
            for (const u of spec.uniques) {
              rows.push({
                table_name: table,
                constraint_name: `${table}_${u.columns.join('_')}_key`,
                constraint_type: 'u',
                definition: `UNIQUE (${u.columns.join(', ')})`,
              });
            }
            for (const fk of spec.foreignKeys) {
              rows.push({
                table_name: table,
                constraint_name: `fk_${table}_${fk.columns.join('_')}`,
                constraint_type: 'f',
                definition: `FOREIGN KEY (${fk.columns.join(', ')}) REFERENCES ${fk.referencesTable}(${fk.referencesColumns.join(', ')}) ON DELETE ${fk.deleteAction}`,
              });
            }
            for (const c of spec.checks) {
              // Emit PG-style forms: IN rewritten as = ANY (ARRAY[...]) with casts,
              // and OR chains wrapped in redundant parentheses.
              let pgDef = c.definition;
              if (pgDef.includes(' in ')) {
                const match = /^(\S+) in \((.+)\)$/.exec(pgDef);
                if (match !== null) {
                  const expr = match[1]!;
                  const items = match[2]!
                    .split(',')
                    .map((s) => `${s.trim()}::text`)
                    .join(', ');
                  pgDef = `${expr} = ANY (ARRAY[${items}])`;
                }
              }
              if (pgDef.includes(' or ')) {
                pgDef = `(${pgDef
                  .split(' or ')
                  .map((t) => `(${t.trim()})`)
                  .join(' OR ')})`;
              }
              rows.push({
                table_name: table,
                constraint_name: `chk_${table}_${c.definition.slice(0, 20).replace(/\W/g, '_')}`,
                constraint_type: 'c',
                definition: `CHECK (${pgDef})`,
              });
            }
          }
        } else if (text.includes('pg_index')) {
          for (const table of TARGET_TABLES) {
            const spec = IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST.tables[table];
            for (const ix of spec.indexes) {
              const unique = ix.unique ? 'UNIQUE ' : '';
              const where = ix.where === null ? '' : ` WHERE ${ix.where}`;
              rows.push({
                index_name: ix.name,
                table_name: table,
                indexdef: `CREATE ${unique}INDEX ${ix.name} ON public.${table} USING btree (${ix.columns.join(', ')})${where}`,
              });
            }
          }
        }
        return { rows: rows as T[] };
      },
    };
  }

  it('passes against PG18-style catalog output without false diffs', async () => {
    const result = await verifySchema(
      pg18Client() as never,
      IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST,
    );
    expect(result.passed).toBe(true);
    expect(result.diffs).toEqual([]);
  });
});

describe('verifySchema return shape', () => {
  it('returns passed false when diffs are non-empty', async () => {
    const client = {
      async query<T extends Record<string, unknown>>() {
        return { rows: [] as T[] };
      },
    };
    const result = await verifySchema(client as never, IDENTITY_CONTROL_PLANE_SCHEMA_MANIFEST);
    expect(result.passed).toBe(false);
    expect(result.diffs.length).toBeGreaterThan(0);
    expect(result.actualTableNames).toEqual([]);
  });
});
