export const API_PREFIX = '/api/v1' as const;

export interface HealthzResponse {
  readonly status: 'ok';
  readonly timestamp: string;
}
