import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { VfSearchInputComponent } from './vf-search-input.component';

describe('VfSearchInputComponent', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  async function create() {
    await TestBed.configureTestingModule({ imports: [VfSearchInputComponent] }).compileComponents();
    const fixture = TestBed.createComponent(VfSearchInputComponent);
    fixture.detectChanges();
    const emitted: string[] = [];
    fixture.componentInstance.debouncedValue.subscribe((value) => emitted.push(value));
    const field = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('.search-field');
    if (!field) {
      throw new Error('search field not rendered');
    }

    return { fixture, field, emitted };
  }

  it('debounces typing before emitting', async () => {
    const { field, emitted } = await create();

    field.value = 'أموك';
    field.dispatchEvent(new Event('input'));
    field.value = 'أموكسي';
    field.dispatchEvent(new Event('input'));
    expect(emitted).toEqual([]);

    vi.advanceTimersByTime(300);
    expect(emitted).toEqual(['أموكسي']);
  });

  it('escape clears the field and emits an empty search', async () => {
    const { field, emitted } = await create();

    field.value = 'باركود';
    field.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(300);

    field.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    vi.advanceTimersByTime(300);

    expect(field.value).toBe('');
    expect(emitted).toEqual(['باركود', '']);
  });
});
