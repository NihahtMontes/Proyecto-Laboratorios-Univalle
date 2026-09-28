import { PersonRepository } from '../src/people/person.repository.js';
import { FakeUsersPgPool } from './users.fakes.js';

interface PersonRowFixture {
  readonly id: number;
  readonly actor_code: string | null;
  readonly person_type: 'internal' | 'external';
  readonly name: string;
  readonly email: string | null;
  readonly phone_number: string | null;
  readonly is_entity: boolean;
  readonly address: string | null;
  readonly category: number;
  readonly status: number;
  readonly created_at: Date;
  readonly updated_at: Date | null;
}

function makeRow(overrides: Partial<PersonRowFixture>): PersonRowFixture {
  return {
    id: 1,
    actor_code: null,
    person_type: 'internal',
    name: 'Person One',
    email: null,
    phone_number: null,
    is_entity: false,
    address: null,
    category: 1,
    status: 0,
    created_at: new Date('2026-09-19T12:00:00.000Z'),
    updated_at: null,
    ...overrides,
  };
}

describe('PersonRepository (deleted-person filter)', () => {
  it('default list excludes deleted (status=2) persons', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new PersonRepository();
    // COUNT
    pool.queueResult([{ total_count: 1 }]);
    // SELECT
    pool.queueResult([makeRow({ id: 1, name: 'Active', status: 0 })]);

    const page = await repo.list(pool, '11111111-1111-4111-8111-111111111111', {});
    expect(page.items).toHaveLength(1);

    const countSql = pool.queries[0]?.sql ?? '';
    expect(countSql).toContain('status<>2');
    expect(countSql).not.toMatch(/status\s*=\s*\$2/);
  });

  it('explicit status=2 filter returns deleted persons', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new PersonRepository();
    pool.queueResult([{ total_count: 1 }]);
    pool.queueResult([makeRow({ id: 7, name: 'Deleted', status: 2 })]);

    const page = await repo.list(pool, '11111111-1111-4111-8111-111111111111', {
      statusFilter: 2,
    });
    expect(page.items).toHaveLength(1);

    const countSql = pool.queries[0]?.sql ?? '';
    expect(countSql).toContain('status=$2');
    expect(countSql).not.toContain('status<>2');
  });

  it('explicit status=0 filter still returns only active persons', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new PersonRepository();
    pool.queueResult([{ total_count: 1 }]);
    pool.queueResult([makeRow({ id: 1, status: 0 })]);

    const page = await repo.list(pool, '11111111-1111-4111-8111-111111111111', {
      statusFilter: 0,
    });
    expect(page.items).toHaveLength(1);

    const countSql = pool.queries[0]?.sql ?? '';
    expect(countSql).toContain('status=$2');
    expect(countSql).not.toContain('status<>2');
    expect(pool.queries[0]?.params).toEqual([expect.any(String), 0]);
  });

  it('explicit status=1 filter returns only suspended persons', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new PersonRepository();
    pool.queueResult([{ total_count: 1 }]);
    pool.queueResult([makeRow({ id: 1, status: 1 })]);

    const page = await repo.list(pool, '11111111-1111-4111-8111-111111111111', {
      statusFilter: 1,
    });
    expect(page.items).toHaveLength(1);

    const countSql = pool.queries[0]?.sql ?? '';
    expect(countSql).toContain('status=$2');
    expect(countSql).not.toContain('status<>2');
    expect(pool.queries[0]?.params).toEqual([expect.any(String), 1]);
  });
});
