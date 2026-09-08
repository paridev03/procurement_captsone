import { Link } from 'react-router-dom';
import StatusBadge from './StatusBadge';
import { EmptyState } from './Feedback';

const money = (n) => `$${Number(n).toLocaleString(undefined, { minimumFractionDigits: 2 })}`;

// A request counts as "new" for 24 hours after whichever happened most recently —
// submission (the moment it became actionable for whoever's queue this is) or, for a
// request never submitted yet, its creation.
const NEW_WINDOW_MS = 24 * 60 * 60 * 1000;
const isNew = (r) => {
  const recency = r.submittedAt || r.createdAt;
  return recency && Date.now() - new Date(recency).getTime() < NEW_WINDOW_MS;
};

export default function RequestList({ requests, emptyMessage }) {
  if (!requests || requests.length === 0) {
    return <EmptyState message={emptyMessage || 'No requests here yet.'} />;
  }

  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Request #</th>
            <th>Title</th>
            <th>Requester</th>
            <th>Department</th>
            <th>Amount</th>
            <th>Status</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          {requests.map((r) => (
            <tr key={r.id} className={isNew(r) ? 'is-new-row' : undefined}>
              <td>
                <Link to={`/requests/${r.id}`}>{r.requestNumber}</Link>
                {isNew(r) && <span className="new-badge">New</span>}
              </td>
              <td>{r.title}</td>
              <td>{r.requesterName}</td>
              <td>{r.department}</td>
              <td>{money(r.totalAmount)}</td>
              <td>
                <StatusBadge status={r.status} />
              </td>
              <td>{new Date(r.createdAt).toLocaleDateString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
