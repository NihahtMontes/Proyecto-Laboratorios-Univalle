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
  normalizeIndexExpression,
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
import { POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST } from '../src/database/schema-manifest-f2.js';

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

describe('normalizeCheckDefinition: PG varchar renderings (tenant 0012 lu_person)', () => {
  // Exact pg_get_constraintdef shapes for varchar(20) person_type / varchar(500) address.
  const PG_TYPE =
    "CHECK (((person_type)::text = ANY ((ARRAY['internal'::character varying, 'external'::character varying])::text[])))";
  const PG_ADDRESS =
    "CHECK ((((person_type)::text <> 'external'::text) OR (length(btrim((COALESCE(address, ''::character varying))::text)) > 0)))";
  const EXPECTED_TYPE = normalizeCheckDefinition("person_type IN ('internal', 'external')");
  const EXPECTED_ADDRESS = normalizeCheckDefinition(
    "person_type <> 'external' OR length(btrim(coalesce(address, ''))) > 0",
  );
  const pgType = (items: string, op = '= ANY', col = 'person_type', cast = '::text[]') =>
    `CHECK (((${col})::text ${op} ((ARRAY[${items}])${cast})))`;
  const vc = (...values: string[]) => values.map((v) => `'${v}'::character varying`).join(', ');
  const pgAddress = (typeValue: string, fallback: string, cmp: string) =>
    `CHECK ((((person_type)::text <> '${typeValue}'::text) OR (length(btrim((COALESCE(address, ${fallback}))::text)) ${cmp})))`;

  it('A: varchar = ANY ((ARRAY[...::character varying])::text[]) equals IN (...)', () => {
    expect(EXPECTED_TYPE).toBe("person_type in ('internal', 'external')");
    expect(normalizeCheckDefinition(PG_TYPE)).toBe(EXPECTED_TYPE);
    expect(normalizeCheckDefinition(pgType(vc('internal', 'external')))).toBe(EXPECTED_TYPE);
  });

  it("B: (COALESCE(col, ''::character varying))::text equals coalesce(col, '')", () => {
    expect(EXPECTED_ADDRESS).toBe(
      "person_type <> 'external' or length(btrim(coalesce(address, ''))) > 0",
    );
    expect(normalizeCheckDefinition(PG_ADDRESS)).toBe(EXPECTED_ADDRESS);
  });

  it('keeps genuinely different IN/ANY constraints distinct', () => {
    const different = [
      pgType(vc('internal', 'supplier')), // 1. wrong allowed value
      pgType(vc('internal')), // 2. missing allowed value
      pgType(vc('internal', 'external', 'supplier')), // 3. extra allowed value
      pgType(vc('internal', 'external'), '= ANY', 'person_kind'), // 4. wrong column
      pgType(vc('internal', 'external'), '<> ALL'), // 5. NOT IN
      pgType(vc('internal', 'external'), '= ALL'), // 6. = ALL instead of = ANY
      // 10. non-varchar / meaningful casts stay fail-closed
      pgType("'internal'::bpchar, 'external'::bpchar"),
      pgType(vc('internal', 'external'), '= ANY', 'person_type', '::integer[]'),
      pgType("'internal'::character varying, (other_col)::character varying"),
    ];
    for (const def of different) {
      expect(normalizeCheckDefinition(def)).not.toBe(EXPECTED_TYPE);
    }
  });

  it('keeps genuinely different COALESCE/address constraints distinct', () => {
    const different = [
      pgAddress('external', "'n/a'::character varying", '> 0'), // 7. fallback differs
      pgAddress('external', "''::character varying", '>= 0'), // 8. different predicate
      pgAddress('internal', "''::character varying", '> 0'), // 9. wrong comparison value
      pgAddress('external', 'other_col', '> 0'), // non-literal fallback
      // 10. a non-varchar cast on the coalesce result stays fail-closed
      "CHECK ((((person_type)::text <> 'external'::text) OR (length(btrim((COALESCE(address, ''::character varying))::integer)) > 0)))",
    ];
    for (const def of different) {
      expect(normalizeCheckDefinition(def)).not.toBe(EXPECTED_ADDRESS);
    }
  });

  it('CHARACTERIZATION: a bpchar fallback is folded by the pre-existing global ::bpchar strip, not by the varchar helper', () => {
    // stripTypeCasts (unchanged, present before F2 fix #3) removes every `::bpchar`,
    // so this shape normalizes equal. PG only renders ''::bpchar for a char(n)
    // column; the column type itself is verified independently by the column spec
    // (lu_person.address is varchar(500)), so a char(n) drift still fails verification.
    expect(normalizeCheckDefinition(pgAddress('external', "''::bpchar", '> 0'))).toBe(
      EXPECTED_ADDRESS,
    );
    // The new varchar helper itself never rewrites bpchar (index path, fix #2 contract).
    expect(normalizeIndexExpression("lower((coalesce(model, ''::bpchar))::text)")).toBe(
      "lower((coalesce(model, ''::bpchar))::text)",
    );
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

describe('normalizeCheckDefinition: PG-18 redundant keyword-quoting on identifiers', () => {
  // PostgreSQL-18 emits double-quoted identifiers in `pg_get_constraintdef`
  // when the column name collides with a reserved keyword (`"position"`,
  // `"length"`, `"user"`, etc.). The manifest declares identifiers bare
  // (e.g. `position IS NULL OR length(position) <= 100`). The
  // normalizer must collapse ONLY the redundant keyword-quoting form,
  // preserving the case-sensitive distinction so that `"Position"`,
  // `"my column"`, and escaped-quote identifiers do NOT get weakened.
  it('collapses `"position"` (lowercase keyword) into bare `position`', () => {
    const actual = normalizeCheckDefinition('"position" IS NULL OR length("position") <= 100');
    const expected = normalizeCheckDefinition('position IS NULL OR length(position) <= 100');
    expect(actual).toBe(expected);
  });

  it('also collapses `"length"` (function name quoted by some forms)', () => {
    const actual = normalizeCheckDefinition('length("position") <= 100');
    const expected = normalizeCheckDefinition('length(position) <= 100');
    expect(actual).toBe(expected);
  });

  it('preserves `"Position"` (case-sensitive) so it does NOT equal bare `Position`', () => {
    const quoted = normalizeCheckDefinition('"Position" IS NULL');
    const bare = normalizeCheckDefinition('Position IS NULL');
    // After lowercase: quoted form becomes `"position" is null` (quotes
    // preserved because the content was mixed-case in the source);
    // bare form becomes `position is null`. They MUST differ.
    expect(quoted).not.toBe(bare);
    expect(quoted).toBe('"position" is null');
    expect(bare).toBe('position is null');
  });

  it('preserves identifiers with spaces (`"my column"`) without weakening', () => {
    const quoted = normalizeCheckDefinition('"my column" IS NULL');
    const bare = normalizeCheckDefinition('my column IS NULL');
    // The bare form is not a legal PG identifier (and the tokenizer
    // will pass it through); but the quotes MUST survive on the quoted
    // form regardless. Crucially the two outputs are not equal — fail-closed.
    expect(quoted).toContain('"my column"');
    expect(quoted).not.toBe(bare);
  });

  it('preserves identifiers with dots or non-ASCII symbols', () => {
    const dotted = normalizeCheckDefinition('"col.with.dots" IS NULL');
    const dashed = normalizeCheckDefinition('"col-with-dash" IS NULL');
    expect(dotted).toBe('"col.with.dots" is null');
    expect(dashed).toBe('"col-with-dash" is null');
  });

  it('preserves ALL_CAPS quoted identifiers (case-sensitive preserved)', () => {
    const result = normalizeCheckDefinition('"ALL_CAPS" IS NULL');
    expect(result).toBe('"all_caps" is null');
  });

  it('preserves identifiers with escaped quotes (`"col""name"`)', () => {
    const result = normalizeCheckDefinition('"col""name" IS NULL');
    // The escaped-quote form has an interior `""` which makes the regex
    // not match the outer pair; the whole token (including its inner
    // escape) survives unchanged.
    expect(result).toContain('"col""name"');
  });

  it('strips redundant quoting after `CHECK ` prefix is removed', () => {
    const actual = normalizeCheckDefinition(
      'CHECK ("position" IS NULL OR length("position") <= 100)',
    );
    const expected = normalizeCheckDefinition(
      'CHECK (position IS NULL OR length(position) <= 100)',
    );
    expect(actual).toBe(expected);
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
      // Mirrors PG-18: identity columns report attidentity='d' from
      // pg_attribute; non-identity columns report ''. The new column
      // query joins information_schema with pg_attribute to surface this.
      attidentity: column.identity !== undefined ? 'd' : '',
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
                attidentity: col.identity !== undefined ? 'd' : '',
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

describe('F2-W14B: F2 manifest mirrors the strengthened password/phone constraints', () => {
  const luUserChecks = POST_F2_CONTROL_PLANE_SCHEMA_MANIFEST.tables.lu_user.checks.map(
    (check) => check.definition,
  );
  const BCRYPT_SHAPE = "'^[$]2[aby][$](0[4-9]|[12][0-9]|3[01])[$][./A-Za-z0-9]{53}$'";

  it('mirrors the exact conditional password CHECK definition', () => {
    const expected = normalizeCheckDefinition(
      `(password_scheme = 'bcrypt' AND password_hash ~ ${BCRYPT_SHAPE}) OR (password_scheme = 'reset_required' AND password_hash IS NULL) OR (password_scheme IN ('legacy_identity_v2', 'legacy_identity_v3') AND password_hash IS NOT NULL AND length(btrim(password_hash)) > 0)`,
    );
    expect(luUserChecks).toContain(expected);
  });

  it('mirrors the exact phone format CHECK definition', () => {
    const expected = normalizeCheckDefinition(
      "phone_number IS NULL OR (length(phone_number) <= 30 AND phone_number ~ '^[+]?[0-9 ().-]+$' AND length(regexp_replace(phone_number, '[^0-9]', '', 'g')) >= 7 AND length(regexp_replace(phone_number, '[^0-9]', '', 'g')) <= 15)",
    );
    expect(luUserChecks).toContain(expected);
  });

  it('no longer carries the W2-era weak conditional hash / phone checks', () => {
    expect(luUserChecks).not.toContain(
      normalizeCheckDefinition('phone_number IS NULL OR length(phone_number) <= 30'),
    );
    expect(luUserChecks).not.toContain(
      normalizeCheckDefinition(
        "(password_scheme IN ('legacy_identity_v2', 'legacy_identity_v3', 'bcrypt') AND password_hash IS NOT NULL AND length(btrim(password_hash)) > 0) OR (password_scheme = 'reset_required' AND password_hash IS NULL)",
      ),
    );
  });
});

describe('normalizeIndexExpression: PG-18 redundant (col)::text cast equivalence', () => {
  it('collapses (col)::text into (col) for bare column references', () => {
    expect(normalizeIndexExpression('lower((name)::text)')).toBe('lower(name)');
    expect(normalizeIndexExpression('lower((code)::text)')).toBe('lower(code)');
  });

  it('leaves non-text casts intact so the verifier stays fail-closed', () => {
    expect(normalizeIndexExpression('lower((name)::int)')).toBe('lower((name)::int)');
    expect(normalizeIndexExpression('lower((name)::varchar(200))')).toBe(
      'lower((name)::varchar(200))',
    );
    expect(normalizeIndexExpression('lower((name)::bpchar)')).toBe('lower((name)::bpchar)');
  });

  it('leaves non-text casts and arithmetic intact (only ::text collapses)', () => {
    // `(a)::text || (b)::text` collapses BOTH casts; for text-typed columns
    // the explicit casts are no-ops so this is a pure deparse-variant. The
    // shape `lower(a || b)` is semantically equivalent to the input.
    expect(normalizeIndexExpression('lower((a)::text || (b)::text)')).toBe('lower(a || b)');
    // `::int` is preserved: changing the cast WOULD change the expression.
    expect(normalizeIndexExpression('(a + b)')).toBe('(a + b)');
    expect(normalizeIndexExpression('ARRAY[(a)::int, (b)::int]')).toBe('ARRAY[(a)::int, (b)::int]');
  });
});

describe('normalizeIndexExpression: varchar COALESCE literal coercion (tenant 0004 ux_lu_equipment_site_name_model_active)', () => {
  const EXPECTED = "lower(coalesce(model, ''::text))";
  const PG_RENDERED = "lower((coalesce(model, ''::character varying))::text)";
  const DEF = (cols: string, where = 'status <> 2', unique = true) =>
    `CREATE ${unique ? 'UNIQUE ' : ''}INDEX ux_lu_equipment_site_name_model_active ON public.lu_equipment USING btree (${cols}) WHERE ${where}`;
  const parse = (def: string) =>
    parseIndexDef(def, 'lu_equipment', 'ux_lu_equipment_site_name_model_active');
  const expectedSpec = {
    name: 'ux_lu_equipment_site_name_model_active',
    table: 'lu_equipment',
    columns: ['site_id', 'lower(name)', EXPECTED],
    unique: true,
    where: normalizeCheckDefinition('status <> 2'),
  };

  it('A: the PG-rendered varchar coercion normalizes to the canonical expected form', () => {
    expect(normalizeIndexExpression(PG_RENDERED)).toBe(EXPECTED);
    expect(normalizeIndexExpression(EXPECTED)).toBe(EXPECTED);
    expect(parse(DEF(`site_id, lower((name)::text), ${PG_RENDERED}`))).toEqual(expectedSpec);
  });

  it('B: genuinely different expressions and index shapes still differ', () => {
    const variants = [
      // wrong column
      `site_id, lower((name)::text), lower((coalesce(brand, ''::character varying))::text)`,
      // missing lower()
      `site_id, lower((name)::text), (coalesce(model, ''::character varying))::text`,
      // different coalesce fallback literal
      `site_id, lower((name)::text), lower((coalesce(model, 'n/a'::character varying))::text)`,
      // bpchar trims on cast: not equivalent, must not collapse
      `site_id, lower((name)::text), lower((coalesce(model, ''::bpchar))::text)`,
      // non-literal fallback: different coalesce semantics
      `site_id, lower((name)::text), lower((coalesce(model, name))::text)`,
    ];
    for (const cols of variants) {
      expect(parse(DEF(cols))).not.toEqual(expectedSpec);
    }
    const good = `site_id, lower((name)::text), ${PG_RENDERED}`;
    // different partial predicate
    expect(parse(DEF(good, 'status <> 1'))).not.toEqual(expectedSpec);
    // different uniqueness
    expect(parse(DEF(good, 'status <> 2', false))).not.toEqual(expectedSpec);
    // bpchar / non-literal shapes are returned unchanged by the normalizer
    expect(normalizeIndexExpression("lower((coalesce(model, ''::bpchar))::text)")).toBe(
      "lower((coalesce(model, ''::bpchar))::text)",
    );
    expect(normalizeIndexExpression('lower((coalesce(model, name))::text)')).toBe(
      'lower((coalesce(model, name))::text)',
    );
  });
});

describe('parseIndexDef: PG-18 lower((col)::text) is parsed equivalent to lower(col)', () => {
  it('treats lower((name)::text) and lower(name) as the same functional expression', () => {
    const a = parseIndexDef(
      'CREATE UNIQUE INDEX ux_lu_faculty_name_active ON public.lu_faculty USING btree (lower(name)) WHERE status <> 2',
      'lu_faculty',
      'ux_lu_faculty_name_active',
    );
    const b = parseIndexDef(
      'CREATE UNIQUE INDEX ux_lu_faculty_name_active ON public.lu_faculty USING btree (lower((name)::text)) WHERE status <> 2',
      'lu_faculty',
      'ux_lu_faculty_name_active',
    );
    expect(a).toBeDefined();
    expect(b).toBeDefined();
    expect(a?.columns).toEqual(b?.columns);
    expect(a?.where).toBe(b?.where);
    expect(a?.unique).toBe(b?.unique);
  });

  it('still distinguishes a different functional expression (lower vs upper)', () => {
    const a = parseIndexDef(
      'CREATE INDEX ix ON public.t USING btree (lower((col)::text))',
      't',
      'ix',
    );
    const b = parseIndexDef(
      'CREATE INDEX ix ON public.t USING btree (upper((col)::text))',
      't',
      'ix',
    );
    expect(a?.columns).not.toEqual(b?.columns);
  });

  it('still distinguishes a different non-text cast', () => {
    const a = parseIndexDef('CREATE INDEX ix ON public.t USING btree (lower(col))', 't', 'ix');
    const b = parseIndexDef(
      'CREATE INDEX ix ON public.t USING btree (lower((col)::int))',
      't',
      'ix',
    );
    expect(a?.columns).not.toEqual(b?.columns);
  });
});

describe('verifySchema: GENERATED BY DEFAULT AS IDENTITY column model', () => {
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
          if (rows !== undefined) return { rows: rows as T[] };
        }
        return { rows: [] as T[] };
      },
    };
  }

  function mkRow(table: string, col: ColumnSpec, attidentity = ''): Record<string, unknown> {
    return {
      table_name: table,
      column_name: col.name,
      data_type: col.dataType,
      udt_name: col.udtName,
      character_maximum_length: col.charMaxLength,
      is_nullable: col.isNullable ? 'YES' : 'NO',
      column_default: col.columnDefault,
      attidentity,
    };
  }

  const identityManifest = {
    schema: 'public',
    tables: {
      lu_faculty: {
        columns: [
          {
            name: 'id',
            dataType: 'bigint',
            udtName: 'int8',
            charMaxLength: null,
            isNullable: false,
            columnDefault: null,
            identity: { generated: 'BY DEFAULT', sequence: 'lu_faculty_id_seq' },
          },
        ],
        primaryKey: { columns: ['id'] },
        uniques: [],
        foreignKeys: [],
        checks: [],
        indexes: [
          {
            name: 'lu_faculty_pkey',
            table: 'lu_faculty',
            columns: ['id'],
            unique: true,
            where: null,
          },
        ],
      } satisfies TableManifest,
    },
  };

  it('accepts identity columns when attidentity=d and columnDefault=null', async () => {
    const identityCol = identityManifest.tables.lu_faculty.columns[0]!;
    const filters: QueryFilter[] = [
      (text) =>
        text.includes('information_schema.columns')
          ? [mkRow('lu_faculty', identityCol, 'd')]
          : undefined,
      (text) =>
        text.includes('pg_constraint')
          ? [
              {
                table_name: 'lu_faculty',
                constraint_name: 'lu_faculty_pkey',
                constraint_type: 'p',
                definition: 'PRIMARY KEY (id)',
              },
            ]
          : undefined,
      (text) =>
        text.includes('pg_index')
          ? [
              {
                index_name: 'lu_faculty_pkey',
                table_name: 'lu_faculty',
                indexdef:
                  'CREATE UNIQUE INDEX lu_faculty_pkey ON public.lu_faculty USING btree (id)',
              },
            ]
          : undefined,
    ];
    const result = await verifySchema(buildClient(filters) as never, identityManifest);
    expect(result.passed).toBe(true);
    expect(result.diffs).toEqual([]);
  });

  it('rejects identity columns when attidentity is empty (column not BY DEFAULT identity)', async () => {
    const identityCol = identityManifest.tables.lu_faculty.columns[0]!;
    const filters: QueryFilter[] = [
      (text) =>
        text.includes('information_schema.columns')
          ? [mkRow('lu_faculty', identityCol, '')]
          : undefined,
      (text) =>
        text.includes('pg_constraint')
          ? [
              {
                table_name: 'lu_faculty',
                constraint_name: 'lu_faculty_pkey',
                constraint_type: 'p',
                definition: 'PRIMARY KEY (id)',
              },
            ]
          : undefined,
      (text) =>
        text.includes('pg_index')
          ? [
              {
                index_name: 'lu_faculty_pkey',
                table_name: 'lu_faculty',
                indexdef:
                  'CREATE UNIQUE INDEX lu_faculty_pkey ON public.lu_faculty USING btree (id)',
              },
            ]
          : undefined,
    ];
    const result = await verifySchema(buildClient(filters) as never, identityManifest);
    expect(result.passed).toBe(false);
    expect(result.diffs.some((d) => d.includes('attidentity') && d.includes("'d'"))).toBe(true);
  });

  it('rejects identity columns when the catalog reports a non-null column_default', async () => {
    const identityCol = identityManifest.tables.lu_faculty.columns[0]!;
    // Non-null column_default simulates a serial-style legacy default that
    // would NOT be consistent with a GENERATED BY DEFAULT AS IDENTITY column.
    const filters: QueryFilter[] = [
      (text) =>
        text.includes('information_schema.columns')
          ? [
              mkRow(
                'lu_faculty',
                { ...identityCol, columnDefault: "nextval('lu_faculty_id_seq'::regclass)" },
                'd',
              ),
            ]
          : undefined,
      (text) =>
        text.includes('pg_constraint')
          ? [
              {
                table_name: 'lu_faculty',
                constraint_name: 'lu_faculty_pkey',
                constraint_type: 'p',
                definition: 'PRIMARY KEY (id)',
              },
            ]
          : undefined,
      (text) =>
        text.includes('pg_index')
          ? [
              {
                index_name: 'lu_faculty_pkey',
                table_name: 'lu_faculty',
                indexdef:
                  'CREATE UNIQUE INDEX lu_faculty_pkey ON public.lu_faculty USING btree (id)',
              },
            ]
          : undefined,
    ];
    const result = await verifySchema(buildClient(filters) as never, identityManifest);
    expect(result.passed).toBe(false);
    expect(result.diffs.some((d) => d.includes('expected identity column'))).toBe(true);
  });
});
