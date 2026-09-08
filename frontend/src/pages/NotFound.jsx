import { Link } from 'react-router-dom';

export default function NotFound() {
  return (
    <div className="state-block">
      <h2>Page not found</h2>
      <p>
        <Link to="/">Go back to the worklist</Link>
      </p>
    </div>
  );
}
