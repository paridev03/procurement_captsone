/// Full request lifecycle tracker — Submitted → Manager Approval → Finance Approval →
/// Vendor Selected → Payment → Completed — matching the Request Details screen's status
/// progression. Rendered unconditionally on the request detail page: whichever role is
/// looking (the requester, their manager, Finance, or a Procurement Admin — anyone the
/// backend already lets view this request) sees the exact same full picture, not just the
/// slice relevant to their own stage.
export default function ApprovalTracker({ request }) {
  const rejectedApproval = request.approvals?.find((a) => a.decision === 'Rejected');
  const isCancelled = request.status === 'Cancelled';
  const isPaymentFailed = request.status === 'PaymentFailed';

  const steps = [
    { key: 'submitted', label: 'Submitted', done: !!request.submittedAt },
    { key: 'manager', label: 'Manager Approval', done: !!request.managerApprovedAt, rejected: rejectedApproval?.stage === 'ManagerApproval' },
    { key: 'finance', label: 'Finance Approval', done: !!request.financeApprovedAt, rejected: rejectedApproval?.stage === 'FinanceApproval' },
    { key: 'vendor', label: 'Vendor Selected', done: !!request.vendor },
    { key: 'payment', label: 'Payment', done: request.payment?.status === 'Success', failed: isPaymentFailed },
    { key: 'completed', label: 'Completed', done: request.status === 'Completed' },
  ];

  const stoppedIndex = steps.findIndex((s) => s.rejected || s.failed);
  const firstNotDone = steps.findIndex((s) => !s.done);
  const activeIndex = stoppedIndex !== -1 ? stoppedIndex : firstNotDone === -1 ? steps.length - 1 : firstNotDone;

  return (
    <div className="approval-tracker">
      {(isCancelled || (!request.submittedAt && request.status === 'Draft')) && (
        <div className={`approval-tracker__note${isCancelled ? ' is-cancelled' : ''}`}>
          {isCancelled ? 'This request was cancelled.' : 'Not yet submitted — still a draft.'}
        </div>
      )}
      <div className="approval-tracker__steps">
        {steps.map((s, i) => {
          const isStopped = stoppedIndex === i;
          const isPastStop = stoppedIndex !== -1 && i > stoppedIndex;
          const state = s.done ? 'done' : isStopped ? 'stopped' : i === activeIndex && !isPastStop ? 'active' : 'pending';
          return (
            <div className="approval-tracker__item" key={s.key}>
              <div className={`approval-tracker__step is-${state}`}>
                <span className="approval-tracker__dot">
                  {state === 'done' ? '✓' : state === 'stopped' ? '✕' : i + 1}
                </span>
                <span className="approval-tracker__label">{s.label}</span>
              </div>
              {i < steps.length - 1 && <span className="approval-tracker__line" />}
            </div>
          );
        })}
      </div>
    </div>
  );
}
