type EmptyStateProps = {
  title: string;
  body: string;
};

export function EmptyState({ title, body }: EmptyStateProps) {
  return (
    <section className="empty-state">
      <p className="eyebrow">Nothing here yet</p>
      <h2>{title}</h2>
      <p>{body}</p>
    </section>
  );
}
