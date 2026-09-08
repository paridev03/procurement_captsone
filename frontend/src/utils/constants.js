// Must match backend/ProcurementApi/Domain/Enums/Enums.cs Category exactly — the wire
// value round-trips directly (no relabeling needed) since the C# enum members are named
// with underscores to match this vocabulary.
export const CATEGORIES = [
  { value: 'IT_EQUIPMENT', label: 'IT Equipment' },
  { value: 'SOFTWARE', label: 'Software' },
  { value: 'OFFICE_SUPPLIES', label: 'Office Supplies' },
  { value: 'TRAVEL', label: 'Travel' },
  { value: 'TRAINING', label: 'Training' },
  { value: 'OTHER', label: 'Other' },
];

export const CATEGORY_LABELS = Object.fromEntries(CATEGORIES.map((c) => [c.value, c.label]));

export const ROLES = [
  { value: 'Employee', label: 'Employee' },
  { value: 'Manager', label: 'Manager' },
  { value: 'Finance', label: 'Finance' },
  { value: 'ProcurementAdmin', label: 'Procurement Admin' },
];
