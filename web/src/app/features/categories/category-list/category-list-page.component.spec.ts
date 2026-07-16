import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { CategoryListPageComponent } from './category-list-page.component';

describe('CategoryListPageComponent', () => {
  let http: HttpTestingController;

  function flushList(totalCount = 0): void {
    const request = http.expectOne(
      (candidate) => candidate.url === '/api/v1/categories' && candidate.method === 'GET',
    );
    request.flush({ items: [], page: 1, pageSize: 25, totalCount });
  }

  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(CategoryListPageComponent);
    http = TestBed.inject(HttpTestingController);
    TestBed.tick(); // the store's list request fires
    flushList(0);
    return fixture.componentInstance;
  }

  afterEach(() => {
    http.verify();
  });

  it('creating a category POSTs the name, closes the dialog, and refreshes the list', () => {
    const component = setup();

    component.openCreate();
    expect(component.dialogVisible()).toBe(true);
    component.onDialogSave('أدوية');

    const create = http.expectOne(
      (request) => request.url === '/api/v1/categories' && request.method === 'POST',
    );
    expect(create.request.body).toEqual({ name: 'أدوية' });
    create.flush({ id: 'c-1' });

    expect(component.dialogVisible()).toBe(false);
    // Success re-reads the list from the server (never optimistic).
    TestBed.tick();
    flushList(1);
  });

  it('a duplicate name keeps the dialog open and surfaces the server field message', () => {
    const component = setup();

    component.openCreate();
    component.onDialogSave('أدوية');

    const create = http.expectOne(
      (request) => request.url === '/api/v1/categories' && request.method === 'POST',
    );
    create.flush(
      {
        type: 'about:blank',
        title: 'Validation',
        status: 400,
        errorCode: 'VTF-VAL-001',
        errors: { name: ['يوجد تصنيف بهذا الاسم بالفعل.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(component.dialogVisible()).toBe(true);
    // A per-field name failure surfaces the local Arabic duplicate message,
    // independent of the server's Accept-Language negotiation.
    expect(component.dialogServerError()).toBe('يوجد تصنيف بهذا الاسم بالفعل.');
    // No refresh is issued on a failed write.
  });

  it('a generic failure surfaces the local fallback message', () => {
    const component = setup();

    component.openCreate();
    component.onDialogSave('أدوية');

    http
      .expectOne((request) => request.url === '/api/v1/categories' && request.method === 'POST')
      .flush(
        { type: 'about:blank', title: 'Server Error', status: 500 },
        { status: 500, statusText: 'Internal Server Error' },
      );

    expect(component.dialogVisible()).toBe(true);
    expect(component.dialogServerError()).toBe('تعذّر حفظ التصنيف. أعد المحاولة.');
  });

  it('renaming a category PUTs to its id and refreshes', () => {
    const component = setup();

    component.openRename({ id: 'c-9', name: 'قديم', isActive: true });
    component.onDialogSave('جديد');

    const rename = http.expectOne(
      (request) => request.url === '/api/v1/categories/c-9' && request.method === 'PUT',
    );
    expect(rename.request.body).toEqual({ name: 'جديد' });
    rename.flush(null, { status: 204, statusText: 'No Content' });

    expect(component.dialogVisible()).toBe(false);
    TestBed.tick();
    flushList(1);
  });

  it('deactivating an active category POSTs deactivate and refreshes', () => {
    const component = setup();

    component.toggleActive({ id: 'c-3', name: 'أدوية', isActive: true });

    http
      .expectOne((request) => request.url === '/api/v1/categories/c-3/deactivate' && request.method === 'POST')
      .flush(null, { status: 204, statusText: 'No Content' });

    TestBed.tick();
    flushList(1);
  });

  it('activating an inactive category POSTs activate', () => {
    const component = setup();

    component.toggleActive({ id: 'c-4', name: 'أعلاف', isActive: false });

    http
      .expectOne((request) => request.url === '/api/v1/categories/c-4/activate' && request.method === 'POST')
      .flush(null, { status: 204, statusText: 'No Content' });

    TestBed.tick();
    flushList(0);
  });
});
