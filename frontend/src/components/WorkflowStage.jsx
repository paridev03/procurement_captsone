/// Shared "① Procurement → ② Payment → ③ Invoice" stage indicator used across the
/// Procurement, Payment, and Invoice pages so the workflow reads as one connected flow
/// rather than three disconnected screens.
export default function WorkflowStage({ procurementStatus, paymentStatus, hasInvoice }) {
  const procurementDone = procurementStatus && procurementStatus !== 'Draft';
  const paymentDone = paymentStatus === 'Success';
  const invoiceDone = !!hasInvoice;

  const stages = [
    {
      key: 'procurement',
      label: 'Procurement',
      sublabel: !procurementStatus ? 'Draft' : procurementStatus === 'Draft' ? 'Draft' : 'Submitted',
      done: procurementDone,
    },
    {
      key: 'payment',
      label: 'Payment',
      sublabel: paymentStatus === 'Success' ? 'Paid' : paymentStatus === 'Draft' ? 'Draft' : 'Pending',
      done: paymentDone,
    },
    {
      key: 'invoice',
      label: 'Invoice',
      sublabel: invoiceDone ? 'Generated' : 'Pending',
      done: invoiceDone,
    },
  ];

  const firstNotDone = stages.findIndex((s) => !s.done);
  const activeIndex = firstNotDone === -1 ? stages.length - 1 : firstNotDone;

  return (
    <div className="workflow-stage">
      {stages.map((s, i) => (
        <div className="workflow-stage__item" key={s.key}>
          <div
            className={
              'workflow-stage__step' +
              (s.done ? ' is-done' : i === activeIndex ? ' is-active' : '')
            }
          >
            <span className="workflow-stage__num">{s.done ? '✓' : i + 1}</span>
            <span className="workflow-stage__text">
              <span className="workflow-stage__label">{s.label}</span>
              <span className="workflow-stage__sub">{s.sublabel}</span>
            </span>
          </div>
          {i < stages.length - 1 && <span className="workflow-stage__arrow">→</span>}
        </div>
      ))}
    </div>
  );
}
