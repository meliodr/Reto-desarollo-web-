import { FormControl, FormGroup } from '@angular/forms';
import { peiPeriodValidator, trimmedRequired, validCalendarDate } from './pei-validation';
describe('PEI form validation', () => {
  const form = (tipo: string, fechaInicio: string, fechaFin: string) =>
    new FormGroup(
      {
        tipo: new FormControl(tipo),
        fechaInicio: new FormControl(fechaInicio),
        fechaFin: new FormControl(fechaFin),
      },
      { validators: peiPeriodValidator },
    );
  it('accepts four inclusive calendar years for an ordinary PEI', () =>
    expect(form('ordinario', '2026-01-01', '2029-12-31').valid).toBe(true));
  it('rejects a five-year ordinary PEI and accepts an extraordinary one', () => {
    expect(form('ordinario', '2026-01-01', '2030-12-31').hasError('periodYears')).toBe(true);
    expect(form('extraordinario', '2026-01-01', '2030-12-31').valid).toBe(true);
  });
  it('rejects reversed dates without changing the supplied values', () => {
    const value = form('ordinario', '2026-12-31', '2026-01-01');
    expect(value.hasError('reversedDates')).toBe(true);
    expect(value.value.fechaFin).toBe('2026-01-01');
  });
  it('validates calendar dates and whitespace-only content', () => {
    expect(validCalendarDate('2028-02-29')).toBe(true);
    expect(validCalendarDate('2026-02-29')).toBe(false);
    expect(new FormControl('   ', trimmedRequired).valid).toBe(false);
  });
});
