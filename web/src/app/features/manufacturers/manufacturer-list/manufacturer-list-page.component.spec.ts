import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { ManufacturerListPageComponent } from './manufacturer-list-page.component';

describe('ManufacturerListPageComponent', () => {
  let http: HttpTestingController;

  function flushList(totalCount = 0): void {
    const request = http.expectOne(
      (candidate) => candidate.url === '/api/v1/manufacturers' && candidate.method === 'GET',
    );
    request.flush({ items: [], page: 1, pageSize: 25, totalCount });
  }

  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(ManufacturerListPageComponent);
    http = TestBed.inject(HttpTestingController);
    TestBed.tick(); // the store's list request fires
    flushList(0);
    return fixture.componentInstance;
  }

  afterEach(() => {
    http.verify();
  });

  it('creating a manufacturer POSTs the name, closes the dialog, and refreshes the list', () => {
    const component = setup();

    component.openCreate();
    expect(component.dialogVisible()).toBe(true);
    component.onDialogSave('شركة الأمل');

    const create = http.expectOne(
      (request) => request.url === '/api/v1/manufacturers' && request.method === 'POST',
    );
    expect(create.request.body).toEqual({ name: 'شركة الأمل' });
    create.flush({ id: 'm-1' });

    expect(component.dialogVisible()).toBe(false);
    // Success re-reads the list from the server (never optimistic).
    TestBed.tick();
    flushList(1);
  });

  it('a duplicate name keeps the dialog open and surfaces the server field message', () => {
    const component = setup();

    component.openCreate();
    component.onDialogSave('شركة الأمل');

    const create = http.expectOne(
      (request) => request.url === '/api/v1/manufacturers' && request.method === 'POST',
    );
    create.flush(
      {
        type: 'about:blank',
        title: 'Validation',
        status: 400,
        errorCode: 'VTF-VAL-001',
        errors: { name: ['توجد شركة مصنعة بهذا الاسم بالفعل.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(component.dialogVisible()).toBe(true);
    // The failure is classified by the shared mapper (STD-UX-123); the dialog
    // projects the `name` field error inline with the local Arabic duplicate
    // message — never the server's text (STD-UX-019, STD-FE-037).
    const failure = component.dialogServerFailure();
    expect(failure?.kind).toBe('field');
    expect(failure?.fieldErrors).toEqual({ name: ['توجد شركة مصنعة بهذا الاسم بالفعل.'] });
    // No refresh is issued on a failed write.
  });

  it('a generic failure surfaces the local fallback message', () => {
    const component = setup();

    component.openCreate();
    component.onDialogSave('شركة الأمل');

    http
      .expectOne((request) => request.url === '/api/v1/manufacturers' && request.method === 'POST')
      .flush(
        { type: 'about:blank', title: 'Server Error', status: 500 },
        { status: 500, statusText: 'Internal Server Error' },
      );

    expect(component.dialogVisible()).toBe(true);
    const failure = component.dialogServerFailure();
    expect(failure?.kind).toBe('system');
    expect(failure?.messageKey).toBe('manufacturers.error.saveFailed');
  });

  it('renaming a manufacturer PUTs to its id and refreshes', () => {
    const component = setup();

    component.openRename({ id: 'm-9', name: 'قديم', isActive: true });
    component.onDialogSave('جديد');

    const rename = http.expectOne(
      (request) => request.url === '/api/v1/manufacturers/m-9' && request.method === 'PUT',
    );
    expect(rename.request.body).toEqual({ name: 'جديد' });
    rename.flush(null, { status: 204, statusText: 'No Content' });

    expect(component.dialogVisible()).toBe(false);
    TestBed.tick();
    flushList(1);
  });

  it('deactivating an active manufacturer POSTs deactivate and refreshes', () => {
    const component = setup();

    component.toggleActive({ id: 'm-3', name: 'شركة الأمل', isActive: true });

    http
      .expectOne((request) => request.url === '/api/v1/manufacturers/m-3/deactivate' && request.method === 'POST')
      .flush(null, { status: 204, statusText: 'No Content' });

    TestBed.tick();
    flushList(1);
  });

  it('activating an inactive manufacturer POSTs activate', () => {
    const component = setup();

    component.toggleActive({ id: 'm-4', name: 'شركة النور', isActive: false });

    http
      .expectOne((request) => request.url === '/api/v1/manufacturers/m-4/activate' && request.method === 'POST')
      .flush(null, { status: 204, statusText: 'No Content' });

    TestBed.tick();
    flushList(0);
  });

  it('a failed toggle is surfaced as a classified failure, never silent (STD-UX-004)', () => {
    const component = setup();

    component.toggleActive({ id: 'm-5', name: 'شركة الأمل', isActive: true });

    http
      .expectOne((request) => request.url === '/api/v1/manufacturers/m-5/deactivate' && request.method === 'POST')
      .flush(
        { type: 'about:blank', title: 'Server Error', status: 500 },
        { status: 500, statusText: 'Internal Server Error' },
      );

    expect(component.toggleFailure()?.kind).toBe('system');
    expect(component.toggleFailure()?.messageKey).toBe('errors.system');
    // The list still re-reads the authoritative state.
    TestBed.tick();
    flushList(1);
  });
});
