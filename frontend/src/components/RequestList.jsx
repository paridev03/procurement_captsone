import { Link } from 'react-router-dom';
import StatusBadge from './StatusBadge';
import { EmptyState } from './Feedback';

const money = (n) => `$${Number(n).toLocaleString(undefined, { minimumFractionDigits: 2 })}`;

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
            <tr key={r.id}>
              <td>
                <Link to={`/requests/${r.id}`}>{r.requestNumber}</Link>
              </td>
              <td>{r.title}</td>
              <td>{r.requesterName}</td>
              <td>{r.department}</td>
              <td>{money(r.estimatedTotalCost)}</td>
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
