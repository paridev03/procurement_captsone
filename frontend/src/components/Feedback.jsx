// Small shared building blocks for loading / error / empty states (Section 9, docs/ANALYSIS.md).

export function Spinner({ label = 'Loading…' }) {
  return <div className="state-block state-block--loading">{label}</div>;
}

export function ErrorBanner({ message }) {
  if (!message) return null;
  return <div className="error-banner">{message}</div>;
}

export function EmptyState({ message }) {
  return <div className="state-block state-block--empty">{message}</div>;
}
