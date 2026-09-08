const STATUS_STYLES = {
  Draft: { bg: '#e5e7eb', fg: '#374151' },
  Submitted: { bg: '#dbeafe', fg: '#1e40af' },
  ManagerApproved: { bg: '#e0e7ff', fg: '#3730a3' },
  FinanceApproved: { bg: '#cffafe', fg: '#155e75' },
  VendorSelected: { bg: '#fef9c3', fg: '#854d0e' },
  PaymentInProgress: { bg: '#fef3c7', fg: '#92400e' },
  Completed: { bg: '#dcfce7', fg: '#166534' },
  Rejected: { bg: '#fee2e2', fg: '#991b1b' },
  PaymentFailed: { bg: '#fee2e2', fg: '#991b1b' },
  Cancelled: { bg: '#f3f4f6', fg: '#6b7280' },
};

const LABELS = {
  Draft: 'Draft',
  Submitted: 'Submitted',
  ManagerApproved: 'Manager Approved',
  FinanceApproved: 'Finance Approved',
  VendorSelected: 'Vendor Selected',
  PaymentInProgress: 'Payment In Progress',
  Completed: 'Completed',
  Rejected: 'Rejected',
  PaymentFailed: 'Payment Failed',
  Cancelled: 'Cancelled',
};

export default function StatusBadge({ status }) {
  const style = STATUS_STYLES[status] || { bg: '#e5e7eb', fg: '#374151' };
  return (
    <span
      style={{
        display: 'inline-block',
        padding: '2px 10px',
        borderRadius: 999,
        fontSize: 12,
        fontWeight: 600,
        background: style.bg,
        color: style.fg,
        whiteSpace: 'nowrap',
      }}
    >
      {LABELS[status] || status}
    </span>
  );
}
