import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';

import { ApiError, ProblemDetails } from './problem-details';

/**
 * The single HTTP access point (/core, ADR-0013): typed responses in,
 * RFC 9457 failures out as {@link ApiError}.
 */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private static readonly BaseUrl = '/api/v1';

  private readonly http = inject(HttpClient);

  get<T>(path: string, params?: Record<string, string | number | boolean | undefined>): Observable<T> {
    let httpParams = new HttpParams();
    if (params) {
      for (const [name, value] of Object.entries(params)) {
        if (value !== undefined && value !== '') {
          httpParams = httpParams.set(name, String(value));
        }
      }
    }

    return this.http
      .get<T>(`${ApiClient.BaseUrl}${path}`, { params: httpParams })
      .pipe(catchError((error: unknown) => throwError(() => toApiError(error))));
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .post<T>(`${ApiClient.BaseUrl}${path}`, body)
      .pipe(catchError((error: unknown) => throwError(() => toApiError(error))));
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return this.http
      .put<T>(`${ApiClient.BaseUrl}${path}`, body)
      .pipe(catchError((error: unknown) => throwError(() => toApiError(error))));
  }

  delete<T>(path: string): Observable<T> {
    return this.http
      .delete<T>(`${ApiClient.BaseUrl}${path}`)
      .pipe(catchError((error: unknown) => throwError(() => toApiError(error))));
  }
}

function toApiError(error: unknown): ApiError {
  if (error instanceof HttpErrorResponse) {
    const problem = isProblemDetails(error.error) ? error.error : null;
    return new ApiError(error.status, problem);
  }

  return new ApiError(0, null);
}

function isProblemDetails(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null && 'status' in body && 'title' in body;
}
