import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
export const trimmedRequired: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.trim().length > 0 ? null : { required: true };
export const trimmedMinLength =
  (min: number): ValidatorFn =>
  (control: AbstractControl) =>
    typeof control.value === 'string' && control.value.trim().length >= min
      ? null
      : { minlength: { requiredLength: min } };
export function validCalendarDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const [year, month, day] = value.split('-').map(Number);
  const date = new Date(0);
  date.setUTCFullYear(year, month - 1, day);
  date.setUTCHours(0, 0, 0, 0);
  return (
    year >= 1 &&
    date.getUTCFullYear() === year &&
    date.getUTCMonth() === month - 1 &&
    date.getUTCDate() === day
  );
}
export const peiPeriodValidator: ValidatorFn = (form: AbstractControl): ValidationErrors | null => {
  const start = form.get('fechaInicio')?.value as string;
  const end = form.get('fechaFin')?.value as string;
  if (!start || !end) return null;
  if (!validCalendarDate(start) || !validCalendarDate(end)) return { invalidDates: true };
  if (end < start) return { reversedDates: true };
  const expected = form.get('tipo')?.value === 'extraordinario' ? 5 : 4;
  const years = Number(end.slice(0, 4)) - Number(start.slice(0, 4)) + 1;
  return years === expected ? null : { periodYears: { expected } };
};
