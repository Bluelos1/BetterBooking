import type { ApiError } from "@/lib/api";

type ApiErrorPanelProps = {
  error: ApiError;
  context: string;
};

export function ApiErrorPanel({ error, context }: ApiErrorPanelProps) {
  const message = error.status === 0 ? "Please try again shortly." : error.detail ?? "Please try again.";

  return (
    <section className="notice error" role="status">
      <p className="eyebrow">{context}</p>
      <h2>{error.status === 0 ? "We could not load this content" : error.title}</h2>
      <p>{message}</p>
    </section>
  );
}
