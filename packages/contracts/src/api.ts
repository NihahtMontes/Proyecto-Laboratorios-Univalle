export interface ApiError {
  readonly code: string;
  readonly message: string;
  readonly correlationId?: string;
  readonly fieldErrors?: Readonly<Record<string, readonly string[]>>;
}

export interface ApiSuccess<T> {
  readonly success: true;
  readonly data: T;
}

export interface ApiFailure {
  readonly success: false;
  readonly error: ApiError;
}

export type ApiResult<T> = ApiSuccess<T> | ApiFailure;
