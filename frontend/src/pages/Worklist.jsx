import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { requestService } from '../api/requestService';
import { useAuth } from '../context/AuthContext';
import RequestList from '../components/RequestList';
import { Spinner, ErrorBanner } from '../components/Feedback';

const HEADINGS = {
  Employee: ['My Requests', 'Requests you have raised.'],
  Manager: ['Pending My Approval', 'Requests from your team awaiting a decision.'],
  Finance: ['Pending Finance Approval', 'Manager-approved requests awaiting a budget decision.'],
  ProcurementAdmin: ['Procurement Queue', 'Finance-approved requests to fulfil: select a vendor and trigger payment.'],
};

export default function Worklist() {
  const { user } = useAuth();
  const [requests, setRequests] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    requestService
      .getWorklist()
      .then((data) => {
        if (!cancelled) setRequests(data);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const [title, subtitle] = HEADINGS[user.role] || ['Requests', ''];

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>{title}</h1>
          <p>{subtitle}</p>
        </div>
        {user.role === 'Employee' && (
          <Link to="/requests/new" className="btn">
            + New Request
          </Link>
        )}
      </div>

      <ErrorBanner message={error} />

      {requests === null && !error ? (
        <Spinner />
      ) : (
        <RequestList requests={requests} emptyMessage="Nothing here right now." />
      )}
    </div>
  );
}
