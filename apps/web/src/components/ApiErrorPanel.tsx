import type { ApiError } from "@/lib/api";

type ApiErrorPanelProps = {
  error: ApiError;
  context: string;
};

export function ApiErrorPanel({ error, context }: ApiErrorPanelProps) {
  const message = error.status === 0
    ? "Start the backend API and set BETTERBOOKING_API_BASE_URL if it is not running on http://localhost:5245."
    : error.detail ?? "The API returned an error response.";

  return (
    <section className="notice error" role="status">
      <p className="eyebrow">{context}</p>
      <h2>{error.status === 0 ? "API is unreachable" : `${error.status} ${error.title}`}</h2>
      <p>{message}</p>
    </section>
  );
}
